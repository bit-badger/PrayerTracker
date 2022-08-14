module PrayerTracker.Handlers.PrayerRequest

open Giraffe
open Microsoft.AspNetCore.Http
open PrayerTracker
open PrayerTracker.Data
open PrayerTracker.Entities
open PrayerTracker.ViewModels

/// Retrieve a prayer request, and ensure that it belongs to the current class
let private findRequest (ctx : HttpContext) reqId = task {
    let! conn = ctx.Conn
    match! PrayerRequests.tryById reqId conn with
    | Some req when req.SmallGroupId = ctx.Session.CurrentGroup.Value.Id -> return Ok req
    | Some _ ->
        let s = Views.I18N.localizer.Force ()
        addError ctx s["The prayer request you tried to access is not assigned to your group"]
        return Result.Error (redirectTo false "/unauthorized" earlyReturn ctx)
    | None -> return Result.Error (fourOhFour ctx)
}

/// Generate a list of requests for the given date
let private generateRequestList (ctx : HttpContext) date = task {
    let  group    = ctx.Session.CurrentGroup.Value
    let  listDate = match date with Some d -> d | None -> SmallGroup.localDateNow ctx.Clock group
    let! conn     = ctx.Conn
    let! reqs     =
        PrayerRequests.forGroup
            {   SmallGroup = group
                Clock      = ctx.Clock
                ListDate   = Some listDate
                ActiveOnly = true
                PageNumber = 0
            } conn
    return
        {   Requests   = reqs
            Date       = listDate
            SmallGroup = group
            ShowHeader = true
            CanEmail   = Option.isSome ctx.User.UserId
            Recipients = []
        }
}

open NodaTime.Text

/// Parse a string into a date (optionally, of course)
let private parseListDate (date : string option) =
    match date with
    | Some dt -> match LocalDatePattern.Iso.Parse dt with it when it.Success -> Some it.Value | _ -> None
    | None -> None

open System

/// GET /prayer-request/[request-id]/edit
let edit reqId : HttpHandler = requireAccess [ User ] >=> fun next ctx -> task {
    let group     = ctx.Session.CurrentGroup.Value
    let now       = SmallGroup.localDateNow ctx.Clock group
    let requestId = PrayerRequestId reqId
    if requestId.Value = Guid.Empty then
        return!
            { viewInfo ctx with HelpLink = Some Help.editRequest }
            |> Views.PrayerRequest.edit EditRequest.empty (now.ToString ("R", null)) ctx
            |> renderHtml next ctx
    else
        match! findRequest ctx requestId with
        | Ok req ->
            let s = Views.I18N.localizer.Force ()
            if PrayerRequest.isExpired now group req then
                { UserMessage.warning with
                    Text        = htmlLocString s["This request is expired."]
                    Description =
                        s["To make it active again, update it as necessary, leave “{0}” and “{1}” unchecked, and it will return as an active request.",
                          s["Expire Immediately"], s["Check to not update the date"]]
                        |> (htmlLocString >> Some)
                  }
                |> addUserMessage ctx
            return!
                { viewInfo ctx with HelpLink = Some Help.editRequest }
                |> Views.PrayerRequest.edit (EditRequest.fromRequest req) "" ctx
                |> renderHtml next ctx
        | Result.Error e -> return! e
}

/// GET /prayer-requests/email/[date]
let email date : HttpHandler = requireAccess [ User ] >=> fun next ctx -> task {
    let  s          = Views.I18N.localizer.Force ()
    let  listDate   = parseListDate (Some date)
    let  group      = ctx.Session.CurrentGroup.Value
    let! list       = generateRequestList ctx listDate
    let! conn       = ctx.Conn
    let! recipients = Members.forGroup group.Id conn
    use! client     = Email.getConnection ()
    do! Email.sendEmails
            {   Client        = client
                Recipients    = recipients
                Group         = group
                Subject       = s["Prayer Requests for {0} - {1:MMMM d, yyyy}", group.Name, list.Date].Value
                HtmlBody      = list.AsHtml s
                PlainTextBody = list.AsText s
                Strings       = s
            }
    return!
        viewInfo ctx
        |> Views.PrayerRequest.email { list with Recipients = recipients }
        |> renderHtml next ctx
}

/// POST /prayer-request/[request-id]/delete
let delete reqId : HttpHandler = requireAccess [ User ] >=> validateCsrf >=> fun next ctx -> task {
    let requestId = PrayerRequestId reqId
    match! findRequest ctx requestId with
    | Ok req ->
        let  s    = Views.I18N.localizer.Force ()
        let! conn = ctx.Conn
        do! PrayerRequests.deleteById req.Id conn
        addInfo ctx s["The prayer request was deleted successfully"]
        return! redirectTo false "/prayer-requests" next ctx
    | Result.Error e -> return! e
}

/// GET /prayer-request/[request-id]/expire
let expire reqId : HttpHandler = requireAccess [ User ] >=> fun next ctx -> task {
    let requestId = PrayerRequestId reqId
    match! findRequest ctx requestId with
    | Ok req ->
        let  s    = Views.I18N.localizer.Force ()
        let! conn = ctx.Conn
        do! PrayerRequests.updateExpiration { req with Expiration = Forced } conn
        addInfo ctx s["Successfully {0} prayer request", s["Expired"].Value.ToLower ()]
        return! redirectTo false "/prayer-requests" next ctx
    | Result.Error e -> return! e
}

/// GET /prayer-requests/[group-id]/list
let list groupId : HttpHandler = requireAccess [ AccessLevel.Public ] >=> fun next ctx -> task {
    let! conn = ctx.Conn
    match! SmallGroups.tryByIdWithPreferences groupId conn with
    | Some group when group.Preferences.IsPublic ->
        let! reqs =
            PrayerRequests.forGroup
                {   SmallGroup = group
                    Clock      = ctx.Clock
                    ListDate   = None
                    ActiveOnly = true
                    PageNumber = 0
                } conn
        return!
            viewInfo ctx
            |> Views.PrayerRequest.list
                {   Requests   = reqs
                    Date       = SmallGroup.localDateNow ctx.Clock group
                    SmallGroup = group
                    ShowHeader = true
                    CanEmail   = Option.isSome ctx.User.UserId
                    Recipients = []
                }
            |> renderHtml next ctx
    | Some _ ->
        let s = Views.I18N.localizer.Force ()
        addError ctx s["The request list for the group you tried to view is not public."]
        return! redirectTo false "/unauthorized" next ctx
    | None -> return! fourOhFour ctx
}

/// GET /prayer-requests/lists
let lists : HttpHandler = requireAccess [ AccessLevel.Public ] >=> fun next ctx -> task {
    let! conn   = ctx.Conn
    let! groups = SmallGroups.listPublicAndProtected conn
    return!
        viewInfo ctx
        |> Views.PrayerRequest.lists groups
        |> renderHtml next ctx
}

/// GET /prayer-requests[/inactive?]
///  - OR -
/// GET /prayer-requests?search=[search-query]
let maintain onlyActive : HttpHandler = requireAccess [ User ] >=> fun next ctx -> task {
    // TODO: stopped here
    let group   = ctx.Session.CurrentGroup.Value
    let pageNbr =
        match ctx.GetQueryStringValue "page" with
        | Ok pg -> match Int32.TryParse pg with true, p -> p | false, _ -> 1
        | Result.Error _ -> 1
    let! model = backgroundTask {
        match ctx.GetQueryStringValue "search" with
        | Ok search ->
            let! reqs = ctx.Db.SearchRequestsForSmallGroup group search pageNbr
            return
                { MaintainRequests.empty with
                    Requests   = reqs
                    SearchTerm = Some search
                    PageNbr    = Some pageNbr
                }
        | Result.Error _ ->
            let! reqs = ctx.Db.AllRequestsForSmallGroup group ctx.Clock None onlyActive pageNbr
            return
                { MaintainRequests.empty with
                    Requests   = reqs
                    OnlyActive = Some onlyActive
                    PageNbr    = if onlyActive then None else Some pageNbr
                }
    }
    return!
        { viewInfo ctx with HelpLink = Some Help.maintainRequests }
        |> Views.PrayerRequest.maintain { model with SmallGroup = group } ctx
        |> renderHtml next ctx
}

/// GET /prayer-request/print/[date]
let print date : HttpHandler = requireAccess [ User; Group ] >=> fun next ctx -> task {
    let! list = generateRequestList ctx (parseListDate (Some date))
    return!
        Views.PrayerRequest.print list appVersion
        |> renderHtml next ctx
}

/// GET /prayer-request/[request-id]/restore
let restore reqId : HttpHandler = requireAccess [ User ] >=> fun next ctx -> task {
    let requestId = PrayerRequestId reqId
    match! findRequest ctx requestId with
    | Ok req ->
        let s  = Views.I18N.localizer.Force ()
        ctx.Db.UpdateEntry { req with Expiration = Automatic; UpdatedDate = ctx.Now }
        let! _ = ctx.Db.SaveChangesAsync ()
        addInfo ctx s["Successfully {0} prayer request", s["Restored"].Value.ToLower ()]
        return! redirectTo false "/prayer-requests" next ctx
    | Result.Error e -> return! e
}

open System.Threading.Tasks

/// POST /prayer-request/save
let save : HttpHandler = requireAccess [ User ] >=> validateCsrf >=> fun next ctx -> task {
    match! ctx.TryBindFormAsync<EditRequest> () with
    | Ok model ->
        let! req =
          if model.IsNew then
              Task.FromResult (Some { PrayerRequest.empty with Id = (Guid.NewGuid >> PrayerRequestId) () })
          else ctx.Db.TryRequestById (idFromShort PrayerRequestId model.RequestId)
        match req with
        | Some pr ->
            let upd8 =
                { pr with
                    RequestType = PrayerRequestType.fromCode model.RequestType
                    Requestor   = match model.Requestor with Some x when x.Trim () = "" -> None | x -> x
                    Text        = ckEditorToText model.Text
                    Expiration  = Expiration.fromCode model.Expiration
                }
            let group = ctx.Session.CurrentGroup.Value
            let now   = SmallGroup.localDateNow ctx.Clock group
            match model.IsNew with
            | true ->
                let dt =
                    (defaultArg (parseListDate model.EnteredDate) now)
                        .AtStartOfDayInZone(SmallGroup.timeZone group)
                        .ToInstant()
                { upd8 with
                    SmallGroupId = group.Id
                    UserId       = ctx.User.UserId.Value
                    EnteredDate  = dt
                    UpdatedDate  = dt
                  }
            | false when defaultArg model.SkipDateUpdate false -> upd8
            | false -> { upd8 with UpdatedDate = ctx.Now }
            |> if model.IsNew then ctx.Db.AddEntry else ctx.Db.UpdateEntry
            let! _   = ctx.Db.SaveChangesAsync ()
            let  s   = Views.I18N.localizer.Force ()
            let  act = if model.IsNew then "Added" else "Updated"
            addInfo ctx s["Successfully {0} prayer request", s[act].Value.ToLower ()]
            return! redirectTo false "/prayer-requests" next ctx
        | None -> return! fourOhFour ctx
    | Result.Error e -> return! bindError e next ctx
}

/// GET /prayer-request/view/[date?]
let view date : HttpHandler = requireAccess [ User; Group ] >=> fun next ctx -> task {
    let! list = generateRequestList ctx (parseListDate date)
    return!
        viewInfo ctx
        |> Views.PrayerRequest.view { list with ShowHeader = false }
        |> renderHtml next ctx
}
