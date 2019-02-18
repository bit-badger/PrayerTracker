module PrayerTracker.Handlers.Help

open Giraffe
open PrayerTracker
open System

/// Help template lookup
let private templates =
  [ Help.sendAnnouncement.Url,     Views.Help.sendAnnouncement
    Help.maintainGroupMembers.Url, Views.Help.groupMembers
    Help.groupPreferences.Url,     Views.Help.preferences
    Help.editRequest.Url,          Views.Help.editRequest
    Help.maintainRequests.Url,     Views.Help.requests
    Help.viewRequestList.Url,      Views.Help.viewRequests
    Help.logOn.Url,                Views.Help.logOn
    Help.changePassword.Url,       Views.Help.password
    ]
  |> Map.ofList


/// GET /help
let index : HttpHandler =
  requireAccess [ AccessLevel.Public ]
  >=> fun next ctx ->
    viewInfo ctx DateTime.Now.Ticks
    |> Views.Help.index
    |> renderHtml next ctx


/// GET /help/[module]/[topic]
let show (``module``, topic) : HttpHandler =
  requireAccess [ AccessLevel.Public ]
  >=> fun next ctx ->
    match templates.TryGetValue (sprintf "%s/%s" ``module`` topic) with
    | true, view -> renderHtml next ctx (view ())
    | false, _ -> fourOhFour next ctx
