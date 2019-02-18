﻿module PrayerTracker.Handlers.PrayerRequest

open FSharp.Control.Tasks.V2.ContextInsensitive
open Giraffe
open Microsoft.AspNetCore.Http
open NodaTime
open PrayerTracker
open PrayerTracker.Entities
open PrayerTracker.ViewModels
open System
open System.Threading.Tasks

/// Retrieve a prayer request, and ensure that it belongs to the current class
let private findRequest (ctx : HttpContext) reqId =
  task {
    let! req = ctx.dbContext().TryRequestById reqId
    match req with
    | Some pr when pr.smallGroupId = (currentGroup ctx).smallGroupId -> return Ok pr
    | Some _ ->
        let s = Views.I18N.localizer.Force ()
        addError ctx s.["The prayer request you tried to access is not assigned to your group"]
        return Error (redirectTo false "/unauthorized")
    | None -> return Error fourOhFour
    }

/// Generate a list of requests for the given date
let private generateRequestList ctx date =
  let grp   = currentGroup ctx
  let clock = ctx.GetService<IClock> ()
  let listDate =
    match date with
    | Some d -> d
    | None -> grp.localDateNow clock
  let reqs = ctx.dbContext().AllRequestsForSmallGroup grp clock (Some listDate) true
  { requests   = reqs |> List.ofSeq
    date       = listDate
    listGroup  = grp
    showHeader = true
    canEmail   = tryCurrentUser ctx |> Option.isSome
    recipients = []
    }

/// Parse a string into a date (optionally, of course)
let private parseListDate (date : string option) =
  match date with
  | Some dt -> match DateTime.TryParse dt with true, d -> Some d | false, _ -> None
  | None -> None


/// GET /prayer-request/[request-id]/edit
let edit (reqId : PrayerRequestId) : HttpHandler =
  requireAccess [ User ]
  >=> fun next ctx ->
    let startTicks = DateTime.Now.Ticks
    let grp        = currentGroup ctx
    let now        = grp.localDateNow (ctx.GetService<IClock> ())
    task {
      match reqId = Guid.Empty with
      | true ->
          return!
            { viewInfo ctx startTicks with script = [ "ckeditor/ckeditor" ]; helpLink = Help.editRequest }
            |> Views.PrayerRequest.edit EditRequest.empty (now.ToString "yyyy-MM-dd") ctx
            |> renderHtml next ctx
      | false ->
          let! result = findRequest ctx reqId
          match result with
          | Ok req ->
              let s = Views.I18N.localizer.Force ()
              match req.isExpired now grp.preferences.daysToExpire with
              | true ->
                  { UserMessage.Warning with
                      text        = htmlLocString s.["This request is expired."]
                      description =
                        s.["To make it active again, update it as necessary, leave “{0}” and “{1}” unchecked, and it will return as an active request.",
                            s.["Expire Immediately"], s.["Check to not update the date"]]
                        |> (htmlLocString >> Some)
                    }
                  |> addUserMessage ctx
              | false -> ()
              return!
                { viewInfo ctx startTicks with script = [ "ckeditor/ckeditor" ]; helpLink = Help.editRequest }
                |> Views.PrayerRequest.edit (EditRequest.fromRequest req) "" ctx
                |> renderHtml next ctx
          | Error e -> return! e next ctx
      }


/// GET /prayer-requests/email/[date]
let email date : HttpHandler =
  requireAccess [ User ]
  >=> fun next ctx ->
    let startTicks = DateTime.Now.Ticks
    let s          = Views.I18N.localizer.Force ()
    let listDate   = parseListDate (Some date)
    let grp        = currentGroup ctx
    task {
      let  list       = generateRequestList ctx listDate
      let! recipients = ctx.dbContext().AllMembersForSmallGroup grp.smallGroupId
      use! client     = Email.getConnection ()
      do! Email.sendEmails client recipients
            grp s.["Prayer Requests for {0} - {1:MMMM d, yyyy}", grp.name, list.date].Value
            (list.asHtml s) (list.asText s) s
      return!
        viewInfo ctx startTicks
        |> Views.PrayerRequest.email { list with recipients = recipients }
        |> renderHtml next ctx
      }


/// POST /prayer-request/[request-id]/delete
let delete reqId : HttpHandler =
  requireAccess [ User ]
  >=> validateCSRF
  >=> fun next ctx ->
    task {
      let! result = findRequest ctx reqId
      match result with
      | Ok r ->
          let db = ctx.dbContext ()
          let s  = Views.I18N.localizer.Force ()
          db.PrayerRequests.Remove r |> ignore
          let! _ = db.SaveChangesAsync ()
          addInfo ctx s.["The prayer request was deleted successfully"]
          return! redirectTo false "/prayer-requests" next ctx
      | Error e -> return! e next ctx
      }


/// GET /prayer-request/[request-id]/expire
let expire reqId : HttpHandler =
  requireAccess [ User ]
  >=> fun next ctx ->
    task {
      let! result = findRequest ctx reqId
      match result with
      | Ok r ->
          let db = ctx.dbContext ()
          let s  = Views.I18N.localizer.Force ()
          db.UpdateEntry { r with isManuallyExpired = true }
          let! _ = db.SaveChangesAsync ()
          addInfo ctx s.["Successfully {0} prayer request", s.["Expired"].Value.ToLower ()]
          return! redirectTo false "/prayer-requests" next ctx
      | Error e -> return! e next ctx
      }


/// GET /prayer-requests/[group-id]/list
let list groupId : HttpHandler =
  requireAccess [ AccessLevel.Public ]
  >=> fun next ctx ->
    let startTicks = DateTime.Now.Ticks
    let db         = ctx.dbContext ()
    task {
      let! grp = db.TryGroupById groupId
      match grp with
      | Some g when g.preferences.isPublic ->
          let clock = ctx.GetService<IClock> ()
          let reqs  = db.AllRequestsForSmallGroup g clock None true
          return!
            viewInfo ctx startTicks
            |> Views.PrayerRequest.list
                { requests   = List.ofSeq reqs
                  date       = g.localDateNow clock
                  listGroup  = g
                  showHeader = true
                  canEmail   = (tryCurrentUser >> Option.isSome) ctx
                  recipients = []
                  }
            |> renderHtml next ctx
      | Some _ ->
          let s = Views.I18N.localizer.Force ()
          addError ctx s.["The request list for the group you tried to view is not public."]
          return! redirectTo false "/unauthorized" next ctx
      | None -> return! fourOhFour next ctx
      }


/// GET /prayer-requests/lists
let lists : HttpHandler =
  requireAccess [ AccessLevel.Public ]
  >=> fun next ctx ->
    let startTicks = DateTime.Now.Ticks
    task {
      let! grps = ctx.dbContext().PublicAndProtectedGroups ()
      return!
        viewInfo ctx startTicks
        |> Views.PrayerRequest.lists grps
        |> renderHtml next ctx
      }


/// GET /prayer-requests[/inactive?]
let maintain onlyActive : HttpHandler =
  requireAccess [ User ]
  >=> fun next ctx ->
    let startTicks = DateTime.Now.Ticks
    let db         = ctx.dbContext ()
    let grp        = currentGroup ctx
    task {
      let reqs = db.AllRequestsForSmallGroup grp (ctx.GetService<IClock> ()) None onlyActive
      return!
        { viewInfo ctx startTicks with helpLink = Help.maintainRequests }
        |> Views.PrayerRequest.maintain reqs grp onlyActive ctx
        |> renderHtml next ctx
      }


/// GET /prayer-request/print/[date]
let print date : HttpHandler =
  requireAccess [ User; Group ]
  >=> fun next ctx ->
    let listDate = parseListDate (Some date)
    task {
      let list = generateRequestList ctx listDate
      return!
        Views.PrayerRequest.print list appVersion
        |> renderHtml next ctx
      }


/// GET /prayer-request/[request-id]/restore
let restore reqId : HttpHandler =
  requireAccess [ User ]
  >=> fun next ctx ->
    task {
      let! result = findRequest ctx reqId
      match result with
      | Ok r ->
          let db = ctx.dbContext ()
          let s  = Views.I18N.localizer.Force ()
          db.UpdateEntry { r with isManuallyExpired = false; updatedDate = DateTime.Now }
          let! _ = db.SaveChangesAsync ()
          addInfo ctx s.["Successfully {0} prayer request", s.["Restored"].Value.ToLower ()]
          return! redirectTo false "/prayer-requests" next ctx
      | Error e -> return! e next ctx
      }


/// POST /prayer-request/save
let save : HttpHandler =
  requireAccess [ User ]
  >=> validateCSRF
  >=> fun next ctx ->
    task {
      let! result = ctx.TryBindFormAsync<EditRequest> ()
      match result with
      | Ok m ->
          let  db  = ctx.dbContext ()
          let! req =
            match m.isNew () with
            | true -> Task.FromResult (Some { PrayerRequest.empty with prayerRequestId = Guid.NewGuid () })
            | false -> db.TryRequestById m.requestId
          match req with
          | Some pr ->
              let upd8 =
                { pr with
                    requestType       = m.requestType
                    requestor         = m.requestor
                    text              = ckEditorToText m.text
                    doNotExpire       = m.expiration = "Y"
                    isManuallyExpired = m.expiration = "X"
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
              |> (match m.isNew () with true -> db.AddEntry | false -> db.UpdateEntry)
              let! _   = db.SaveChangesAsync ()
              let  s   = Views.I18N.localizer.Force ()
              let  act = match m.isNew () with true -> "Added" | false -> "Updated"
              addInfo ctx s.["Successfully {0} prayer request", s.[act].Value.ToLower ()]
              return! redirectTo false "/prayer-requests" next ctx
          | None -> return! fourOhFour next ctx
      | Error e -> return! bindError e next ctx
      }


/// GET /prayer-request/view/[date?]
let view date : HttpHandler =
  requireAccess [ User; Group ]
  >=> fun next ctx ->
    let startTicks = DateTime.Now.Ticks
    let listDate   = parseListDate date
    task {
      let list = generateRequestList ctx listDate
      return!
        viewInfo ctx startTicks
        |> Views.PrayerRequest.view { list with showHeader = false }
        |> renderHtml next ctx
      }
