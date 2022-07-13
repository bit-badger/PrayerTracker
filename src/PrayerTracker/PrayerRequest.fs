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
    | Some req when req.smallGroupId = (currentGroup ctx).smallGroupId -> return Ok req
    | Some _ ->
        let s = Views.I18N.localizer.Force ()
        addError ctx s["The prayer request you tried to access is not assigned to your group"]
        return Error (redirectTo false "/web/unauthorized")
    | None -> return Error fourOhFour
}

/// Generate a list of requests for the given date
let private generateRequestList ctx date = task {
    let  grp      = currentGroup ctx
    let  clock    = ctx.GetService<IClock> ()
    let  listDate = match date with Some d -> d | None -> grp.localDateNow clock
    let! reqs     = ctx.db.AllRequestsForSmallGroup grp clock (Some listDate) true 0
    return
        { requests   = reqs |> List.ofSeq
          date       = listDate
          listGroup  = grp
          showHeader = true
          canEmail   = ctx.Session.user |> Option.isSome
          recipients = []
        }
}

/// Parse a string into a date (optionally, of course)
let private parseListDate (date : string option) =
    match date with
    | Some dt -> match DateTime.TryParse dt with true, d -> Some d | false, _ -> None
    | None -> None


/// GET /prayer-request/[request-id]/edit
let edit (reqId : PrayerRequestId) : HttpHandler = requireAccess [ User ] >=> fun next ctx -> task {
    let startTicks = DateTime.Now.Ticks
    let grp        = currentGroup ctx
    let now        = grp.localDateNow (ctx.GetService<IClock> ())
    if reqId = Guid.Empty then
        return!
            { viewInfo ctx startTicks with script = [ "ckeditor/ckeditor" ]; helpLink = Some Help.editRequest }
            |> Views.PrayerRequest.edit EditRequest.empty (now.ToString "yyyy-MM-dd") ctx
            |> renderHtml next ctx
    else
        match! findRequest ctx reqId with
        | Ok req ->
            let s = Views.I18N.localizer.Force ()
            if req.isExpired now grp.preferences.daysToExpire then
                { UserMessage.warning with
                    text        = htmlLocString s["This request is expired."]
                    description =
                        s["To make it active again, update it as necessary, leave “{0}” and “{1}” unchecked, and it will return as an active request.",
                          s["Expire Immediately"], s["Check to not update the date"]]
                        |> (htmlLocString >> Some)
                  }
                |> addUserMessage ctx
            return!
                { viewInfo ctx startTicks with script = [ "ckeditor/ckeditor" ]; helpLink = Some Help.editRequest }
                |> Views.PrayerRequest.edit (EditRequest.fromRequest req) "" ctx
                |> renderHtml next ctx
        | Error e -> return! e next ctx
}


/// GET /prayer-requests/email/[date]
let email date : HttpHandler = requireAccess [ User ] >=> fun next ctx -> task {
    let  startTicks  = DateTime.Now.Ticks
    let  s           = Views.I18N.localizer.Force ()
    let  listDate    = parseListDate (Some date)
    let  grp         = currentGroup ctx
    let! list        = generateRequestList ctx listDate
    let! recipients  = ctx.db.AllMembersForSmallGroup grp.smallGroupId
    use! client      = Email.getConnection ()
    do! Email.sendEmails client recipients
          grp s["Prayer Requests for {0} - {1:MMMM d, yyyy}", grp.name, list.date].Value
          (list.asHtml s) (list.asText s) s
    return!
        viewInfo ctx startTicks
        |> Views.PrayerRequest.email { list with recipients = recipients }
        |> renderHtml next ctx
}


/// POST /prayer-request/[request-id]/delete
let delete reqId : HttpHandler = requireAccess [ User ] >=> validateCSRF >=> fun next ctx -> task {
    match! findRequest ctx reqId with
    | Ok req ->
        let s  = Views.I18N.localizer.Force ()
        ctx.db.PrayerRequests.Remove req |> ignore
        let! _ = ctx.db.SaveChangesAsync ()
        addInfo ctx s["The prayer request was deleted successfully"]
        return! redirectTo false "/web/prayer-requests" next ctx
    | Error e -> return! e next ctx
}


/// GET /prayer-request/[request-id]/expire
let expire reqId : HttpHandler = requireAccess [ User ] >=> fun next ctx -> task {
    match! findRequest ctx reqId with
    | Ok req ->
        let s  = Views.I18N.localizer.Force ()
        ctx.db.UpdateEntry { req with expiration = Forced }
        let! _ = ctx.db.SaveChangesAsync ()
        addInfo ctx s["Successfully {0} prayer request", s["Expired"].Value.ToLower ()]
        return! redirectTo false "/web/prayer-requests" next ctx
    | Error e -> return! e next ctx
}


/// GET /prayer-requests/[group-id]/list
let list groupId : HttpHandler = requireAccess [ AccessLevel.Public ] >=> fun next ctx -> task {
    let startTicks = DateTime.Now.Ticks
    match! ctx.db.TryGroupById groupId with
    | Some grp when grp.preferences.isPublic ->
        let clock = ctx.GetService<IClock> ()
        let! reqs  = ctx.db.AllRequestsForSmallGroup grp clock None true 0
        return!
            viewInfo ctx startTicks
            |> Views.PrayerRequest.list
                { requests   = reqs
                  date       = grp.localDateNow clock
                  listGroup  = grp
                  showHeader = true
                  canEmail   = ctx.Session.user |> Option.isSome
                  recipients = []
                }
            |> renderHtml next ctx
    | Some _ ->
        let s = Views.I18N.localizer.Force ()
        addError ctx s["The request list for the group you tried to view is not public."]
        return! redirectTo false "/web/unauthorized" next ctx
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
        | Error _ -> 1
    let! m = backgroundTask {
        match ctx.GetQueryStringValue "search" with
        | Ok search ->
            let! reqs = ctx.db.SearchRequestsForSmallGroup grp search pageNbr
            return
                { MaintainRequests.empty with
                    requests   = reqs
                    searchTerm = Some search
                    pageNbr    = Some pageNbr
                }
        | Error _ ->
            let! reqs = ctx.db.AllRequestsForSmallGroup grp (ctx.GetService<IClock> ()) None onlyActive pageNbr
            return
                { MaintainRequests.empty with
                    requests   = reqs
                    onlyActive = Some onlyActive
                    pageNbr    = match onlyActive with true -> None | false -> Some pageNbr
                }
    }
    return!
        { viewInfo ctx startTicks with helpLink = Some Help.maintainRequests }
        |> Views.PrayerRequest.maintain { m with smallGroup = grp } ctx
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
    match! findRequest ctx reqId with
    | Ok req ->
        let s  = Views.I18N.localizer.Force ()
        ctx.db.UpdateEntry { req with expiration = Automatic; updatedDate = DateTime.Now }
        let! _ = ctx.db.SaveChangesAsync ()
        addInfo ctx s["Successfully {0} prayer request", s["Restored"].Value.ToLower ()]
        return! redirectTo false "/web/prayer-requests" next ctx
    | Error e -> return! e next ctx
}


/// POST /prayer-request/save
let save : HttpHandler = requireAccess [ User ] >=> validateCSRF >=> fun next ctx -> task {
    match! ctx.TryBindFormAsync<EditRequest> () with
    | Ok m ->
        let! req =
          if m.isNew () then Task.FromResult (Some { PrayerRequest.empty with prayerRequestId = Guid.NewGuid () })
          else ctx.db.TryRequestById m.requestId
        match req with
        | Some pr ->
            let upd8 =
                { pr with
                    requestType = PrayerRequestType.fromCode m.requestType
                    requestor   = match m.requestor with Some x when x.Trim () = "" -> None | x -> x
                    text        = ckEditorToText m.text
                    expiration  = Expiration.fromCode m.expiration
                }
            let grp = currentGroup ctx
            let now = grp.localDateNow (ctx.GetService<IClock> ())
            match m.isNew () with
            | true ->
                let dt = match m.enteredDate with Some x -> x | None -> now
                { upd8 with
                    smallGroupId = grp.smallGroupId
                    userId       = (currentUser ctx).userId
                    enteredDate  = dt
                    updatedDate  = dt
                  }
            | false when Option.isSome m.skipDateUpdate && Option.get m.skipDateUpdate -> upd8
            | false -> { upd8 with updatedDate = now }
            |> (if m.isNew () then ctx.db.AddEntry else ctx.db.UpdateEntry)
            let! _   = ctx.db.SaveChangesAsync ()
            let  s   = Views.I18N.localizer.Force ()
            let  act = if m.isNew () then "Added" else "Updated"
            addInfo ctx s["Successfully {0} prayer request", s.[act].Value.ToLower ()]
            return! redirectTo false "/web/prayer-requests" next ctx
        | None -> return! fourOhFour next ctx
    | Error e -> return! bindError e next ctx
}


/// GET /prayer-request/view/[date?]
let view date : HttpHandler = requireAccess [ User; Group ] >=> fun next ctx -> task {
    let  startTicks = DateTime.Now.Ticks
    let! list       = generateRequestList ctx (parseListDate date)
    return!
        viewInfo ctx startTicks
        |> Views.PrayerRequest.view { list with showHeader = false }
        |> renderHtml next ctx
}
