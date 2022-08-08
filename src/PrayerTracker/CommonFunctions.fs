/// Functions common to many handlers
[<AutoOpen>]
module PrayerTracker.Handlers.CommonFunctions

open Microsoft.AspNetCore.Mvc.Rendering

/// Create a select list from an enumeration
let toSelectList<'T> valFunc textFunc withDefault emptyText (items : 'T seq) =
    match items with null -> nullArg "items" | _ -> ()
    [ match withDefault with
      | true ->
          let s = PrayerTracker.Views.I18N.localizer.Force ()
          yield SelectListItem ($"""&mdash; %A{s[emptyText]} &mdash;""", "")
      | _ -> ()
      yield! items |> Seq.map (fun x -> SelectListItem (textFunc x, valFunc x))
    ]
  
/// Create a select list from an enumeration
let toSelectListWithEmpty<'T> valFunc textFunc emptyText (items : 'T seq) =
    toSelectList valFunc textFunc true emptyText items
    
/// Create a select list from an enumeration
let toSelectListWithDefault<'T> valFunc textFunc (items : 'T seq) =
    toSelectList valFunc textFunc true "Select" items

/// The version of PrayerTracker
let appVersion =
    let v = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version
#if (DEBUG)
    $"v{v}"
#else
    seq {
        $"v%d{v.Major}"
        match v.Minor with
        | 0 -> match v.Build with 0 -> () | _ -> $".0.%d{v.Build}"
        | _ ->
            $".%d{v.Minor}"
            match v.Build with 0 -> () | _ -> $".%d{v.Build}"
    }
    |> String.concat ""
#endif


open Giraffe
open Giraffe.Htmx
open Microsoft.AspNetCore.Http
open NodaTime
open PrayerTracker
open PrayerTracker.ViewModels

/// Create the common view information heading
let viewInfo (ctx : HttpContext) =
    let msg =
        match ctx.Session.Messages with
        | [] -> []
        | x ->
            ctx.Session.Messages <- []
            x
    let layout =
        match ctx.Request.Headers.HxTarget with
        | Some hdr when hdr = "pt-body" -> ContentOnly
        | Some _ -> PartialPage
        | None -> FullPage
    { AppViewInfo.fresh with
        Version      = appVersion
        Messages     = msg
        RequestStart = ctx.Items[Key.startTime] :?> Instant
        User         = ctx.Session.CurrentUser
        Group        = ctx.Session.CurrentGroup
        Layout       = layout
    }

/// The view is the last parameter, so it can be composed
let renderHtml next ctx view =
    htmlView view next ctx

open Microsoft.Extensions.Logging

/// Display an error regarding form submission
let bindError (msg : string) =
    handleContext (fun ctx ->
        ctx.GetService<ILoggerFactory>().CreateLogger("PrayerTracker.Handlers").LogError msg
        (setStatusCode 400 >=> text msg) earlyReturn ctx)

/// Handler that will return a status code 404 and the text "Not Found"
let fourOhFour (ctx : HttpContext) =
    (setStatusCode 404 >=> text "Not Found") earlyReturn ctx

/// Handler to validate CSRF prevention token
let validateCsrf : HttpHandler = fun next ctx -> task {
    match! (ctx.GetService<Microsoft.AspNetCore.Antiforgery.IAntiforgery> ()).IsRequestValidAsync ctx with
    | true -> return! next ctx
    | false -> return! (clearResponse >=> setStatusCode 400 >=> text "Quit hacking...") earlyReturn ctx
}

/// Add a message to the session
let addUserMessage (ctx : HttpContext) msg =
    ctx.Session.Messages <- msg :: ctx.Session.Messages


open Microsoft.AspNetCore.Html
open Microsoft.Extensions.Localization

/// Convert a localized string to an HTML string
let htmlLocString (x : LocalizedString) =
    (System.Net.WebUtility.HtmlEncode >> HtmlString) x.Value

let htmlString (x : LocalizedString) =
    HtmlString x.Value

/// Add an error message to the session
let addError ctx msg =
    addUserMessage ctx { UserMessage.error with Text = htmlLocString msg }

/// Add an informational message to the session
let addInfo ctx msg =
    addUserMessage ctx { UserMessage.info with Text = htmlLocString msg }

/// Add an informational HTML message to the session
let addHtmlInfo ctx msg =
    addUserMessage ctx { UserMessage.info with Text = htmlString msg }
  
/// Add a warning message to the session
let addWarning ctx msg =
    addUserMessage ctx { UserMessage.warning with Text = htmlLocString msg }


/// A level of required access
type AccessLevel =
    /// Administrative access
    | Admin
    /// Small group administrative access
    | User
    /// Small group member access
    | Group
    /// Errbody
    | Public


open Microsoft.AspNetCore.Http.Extensions
open PrayerTracker.Entities

/// Require one of the given access roles
let requireAccess levels : HttpHandler = fun next ctx -> task {
    // These calls fill the user and group in the session, making .Value safe to use for the rest of the request
    let! user  = ctx.CurrentUser  ()
    let! group = ctx.CurrentGroup ()
    match user, group with
    | _, _      when List.contains Public levels              -> return! next ctx
    | Some _, _ when List.contains User   levels              -> return! next ctx
    | _, Some _ when List.contains Group  levels              -> return! next ctx
    | Some u, _ when List.contains Admin  levels && u.IsAdmin -> return! next ctx
    | _, _ when List.contains Admin levels ->
        let s = Views.I18N.localizer.Force ()
        addError ctx s["You are not authorized to view the requested page."]
        return! redirectTo false "/unauthorized" next ctx
    | _, _ when List.contains User levels ->
        // Redirect to the user log on page
        ctx.Session.SetString (Key.Session.redirectUrl, ctx.Request.GetEncodedPathAndQuery ())
        return! redirectTo false "/user/log-on" next ctx
    | _, _ when List.contains Group levels ->
        // Redirect to the small group log on page
        return! redirectTo false "/small-group/log-on" next ctx
    | _, _ ->
        let s = Views.I18N.localizer.Force ()
        addError ctx s["You are not authorized to view the requested page."]
        return! redirectTo false "/unauthorized" next ctx
}
