module PrayerTracker.Handlers.PrayerRequest

open System
open System.Threading.Tasks
open Giraffe
open Microsoft.AspNetCore.Http
open NodaTime
open PrayerTracker
open PrayerTracker.Entities
open PrayerTracker.ViewModels

/// Retrieve a prayer request, and ensure that it belongs to the current class
let private findRequest (ctx : HttpContext) reqId = task {
    match! ctx.db.TryRequestById reqId with
    | Some req when req.SmallGroupId = (currentGroup ctx).Id -> return Ok req
    | Some _ ->
        let s = Views.I18N.localizer.Force ()
        addError ctx s["The prayer request you tried to access is not assigned to your group"]
        return Result.Error (redirectTo false "/unauthorized")
    | None -> return Result.Error fourOhFour
}

/// Generate a list of requests for the given date
let private generateRequestList ctx date = task {
    let  grp      = currentGroup ctx
    let  clock    = ctx.GetService<IClock> ()
    let  listDate = match date with Some d -> d | None -> grp.localDateNow clock
    let! reqs     = ctx.db.AllRequestsForSmallGroup grp clock (Some listDate) true 0
    return
        {   Requests   = reqs
            Date       = listDate
            SmallGroup = grp
            ShowHeader = true
            CanEmail   = Option.isSome ctx.Session.user
            Recipients = []
        }
}

/// Parse a string into a date (optionally, of course)
let private parseListDate (date : string option) =
    match date with
    | Some dt -> match DateTime.TryParse dt with true, d -> Some d | false, _ -> None
    | None -> None


/// GET /prayer-request/[request-id]/edit
let edit reqId : HttpHandler = requireAccess [ User ] >=> fun next ctx -> task {
    let startTicks = DateTime.Now.Ticks
    let grp        = currentGroup ctx
    let now        = grp.localDateNow (ctx.GetService<IClock> ())
    let requestId  = PrayerRequestId reqId
    if requestId.Value = Guid.Empty then
        return!
            { viewInfo ctx startTicks with Script = [ "ckeditor/ckeditor" ]; HelpLink = Some Help.editRequest }
            |> Views.PrayerRequest.edit EditRequest.empty (now.ToString "yyyy-MM-dd") ctx
            |> renderHtml next ctx
    else
        match! findRequest ctx requestId with
        | Ok req ->
            let s = Views.I18N.localizer.Force ()
            if req.isExpired now grp.Preferences.DaysToExpire then
                { UserMessage.warning with
                    Text        = htmlLocString s["This request is expired."]
                    Description =
                        s["To make it active again, update it as necessary, leave “{0}” and “{1}” unchecked, and it will return as an active request.",
                          s["Expire Immediately"], s["Check to not update the date"]]
                        |> (htmlLocString >> Some)
                  }
                |> addUserMessage ctx
            return!
                { viewInfo ctx startTicks with Script = [ "ckeditor/ckeditor" ]; HelpLink = Some Help.editRequest }
                |> Views.PrayerRequest.edit (EditRequest.fromRequest req) "" ctx
                |> renderHtml next ctx
        | Result.Error e -> return! e next ctx
}


/// GET /prayer-requests/email/[date]
let email date : HttpHandler = requireAccess [ User ] >=> fun next ctx -> task {
    let  startTicks  = DateTime.Now.Ticks
    let  s           = Views.I18N.localizer.Force ()
    let  listDate    = parseListDate (Some date)
    let  grp         = currentGroup ctx
    let! list        = generateRequestList ctx listDate
    let! recipients  = ctx.db.AllMembersForSmallGroup grp.Id
    use! client      = Email.getConnection ()
    do! Email.sendEmails client recipients
          grp s["Prayer Requests for {0} - {1:MMMM d, yyyy}", grp.Name, list.Date].Value
          (list.AsHtml s) (list.AsText s) s
    return!
        viewInfo ctx startTicks
        |> Views.PrayerRequest.email { list with Recipients = recipients }
        |> renderHtml next ctx
}


/// POST /prayer-request/[request-id]/delete
let delete reqId : HttpHandler = requireAccess [ User ] >=> validateCsrf >=> fun next ctx -> task {
    let requestId = PrayerRequestId reqId
    match! findRequest ctx requestId with
    | Ok req ->
        let s  = Views.I18N.localizer.Force ()
        ctx.db.PrayerRequests.Remove req |> ignore
        let! _ = ctx.db.SaveChangesAsync ()
        addInfo ctx s["The prayer request was deleted successfully"]
        return! redirectTo false "/prayer-requests" next ctx
    | Result.Error e -> return! e next ctx
}


/// GET /prayer-request/[request-id]/expire
let expire reqId : HttpHandler = requireAccess [ User ] >=> fun next ctx -> task {
    let requestId = PrayerRequestId reqId
    match! findRequest ctx requestId with
    | Ok req ->
        let s  = Views.I18N.localizer.Force ()
        ctx.db.UpdateEntry { req with Expiration = Forced }
        let! _ = ctx.db.SaveChangesAsync ()
        addInfo ctx s["Successfully {0} prayer request", s["Expired"].Value.ToLower ()]
        return! redirectTo false "/prayer-requests" next ctx
    | Result.Error e -> return! e next ctx
}


/// GET /prayer-requests/[group-id]/list
let list groupId : HttpHandler = requireAccess [ AccessLevel.Public ] >=> fun next ctx -> task {
    let startTicks = DateTime.Now.Ticks
    match! ctx.db.TryGroupById groupId with
    | Some grp when grp.Preferences.IsPublic ->
        let clock = ctx.GetService<IClock> ()
        let! reqs  = ctx.db.AllRequestsForSmallGroup grp clock None true 0
        return!
            viewInfo ctx startTicks
            |> Views.PrayerRequest.list
                { Requests   = reqs
                  Date       = grp.localDateNow clock
                  SmallGroup = grp
                  ShowHeader = true
                  CanEmail   = Option.isSome ctx.Session.user
                  Recipients = []
                }
            |> renderHtml next ctx
    | Some _ ->
        let s = Views.I18N.localizer.Force ()
        addError ctx s["The request list for the group you tried to view is not public."]
        return! redirectTo false "/unauthorized" next ctx
    | None -> return! fourOhFour next ctx
}


/// GET /prayer-requests/lists
let lists : HttpHandler = requireAccess [ AccessLevel.Public ] >=> fun next ctx -> task {
    let  startTicks = DateTime.Now.Ticks
    let! groups     = ctx.db.PublicAndProtectedGroups ()
    return!
        viewInfo ctx startTicks
        |> Views.PrayerRequest.lists groups
        |> renderHtml next ctx
}


/// GET /prayer-requests[/inactive?]
///  - OR -
/// GET /prayer-requests?search=[search-query]
let maintain onlyActive : HttpHandler = requireAccess [ User ] >=> fun next ctx -> task {
    let startTicks = DateTime.Now.Ticks
    let grp        = currentGroup ctx
    let pageNbr    =
        match ctx.GetQueryStringValue "page" with
        | Ok pg -> match Int32.TryParse pg with true, p -> p | false, _ -> 1
        | Result.Error _ -> 1
    let! m = backgroundTask {
        match ctx.GetQueryStringValue "search" with
        | Ok search ->
            let! reqs = ctx.db.SearchRequestsForSmallGroup grp search pageNbr
            return
                { MaintainRequests.empty with
                    Requests   = reqs
                    SearchTerm = Some search
                    PageNbr    = Some pageNbr
                }
        | Result.Error _ ->
            let! reqs = ctx.db.AllRequestsForSmallGroup grp (ctx.GetService<IClock> ()) None onlyActive pageNbr
            return
                { MaintainRequests.empty with
                    Requests   = reqs
                    OnlyActive = Some onlyActive
                    PageNbr    = if onlyActive then None else Some pageNbr
                }
    }
    return!
        { viewInfo ctx startTicks with HelpLink = Some Help.maintainRequests }
        |> Views.PrayerRequest.maintain { m with SmallGroup = grp } ctx
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
        ctx.db.UpdateEntry { req with Expiration = Automatic; UpdatedDate = DateTime.Now }
        let! _ = ctx.db.SaveChangesAsync ()
        addInfo ctx s["Successfully {0} prayer request", s["Restored"].Value.ToLower ()]
        return! redirectTo false "/prayer-requests" next ctx
    | Result.Error e -> return! e next ctx
}


/// POST /prayer-request/save
let save : HttpHandler = requireAccess [ User ] >=> validateCsrf >=> fun next ctx -> task {
    match! ctx.TryBindFormAsync<EditRequest> () with
    | Ok m ->
        let! req =
          if m.IsNew then Task.FromResult (Some { PrayerRequest.empty with Id = (Guid.NewGuid >> PrayerRequestId) () })
          else ctx.db.TryRequestById (idFromShort PrayerRequestId m.RequestId)
        match req with
        | Some pr ->
            let upd8 =
                { pr with
                    RequestType = PrayerRequestType.fromCode m.RequestType
                    Requestor   = match m.Requestor with Some x when x.Trim () = "" -> None | x -> x
                    Text        = ckEditorToText m.Text
                    Expiration  = Expiration.fromCode m.Expiration
                }
            let grp = currentGroup ctx
            let now = grp.localDateNow (ctx.GetService<IClock> ())
            match m.IsNew with
            | true ->
                let dt = defaultArg m.EnteredDate now
                { upd8 with
                    SmallGroupId = grp.Id
                    UserId       = (currentUser ctx).Id
                    EnteredDate  = dt
                    UpdatedDate  = dt
                  }
            | false when defaultArg m.SkipDateUpdate false -> upd8
            | false -> { upd8 with UpdatedDate = now }
            |> if m.IsNew then ctx.db.AddEntry else ctx.db.UpdateEntry
            let! _   = ctx.db.SaveChangesAsync ()
            let  s   = Views.I18N.localizer.Force ()
            let  act = if m.IsNew then "Added" else "Updated"
            addInfo ctx s["Successfully {0} prayer request", s[act].Value.ToLower ()]
            return! redirectTo false "/prayer-requests" next ctx
        | None -> return! fourOhFour next ctx
    | Result.Error e -> return! bindError e next ctx
}


/// GET /prayer-request/view/[date?]
let view date : HttpHandler = requireAccess [ User; Group ] >=> fun next ctx -> task {
    let  startTicks = DateTime.Now.Ticks
    let! list       = generateRequestList ctx (parseListDate date)
    return!
        viewInfo ctx startTicks
        |> Views.PrayerRequest.view { list with ShowHeader = false }
        |> renderHtml next ctx
}
