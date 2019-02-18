/// Functions common to many handlers
[<AutoOpen>]
module PrayerTracker.Handlers.CommonFunctions

open FSharp.Control.Tasks.V2.ContextInsensitive
open Giraffe
open Microsoft.AspNetCore.Antiforgery
open Microsoft.AspNetCore.Html
open Microsoft.AspNetCore.Http
open Microsoft.AspNetCore.Http.Extensions
open Microsoft.AspNetCore.Mvc.Rendering
open Microsoft.Extensions.Localization
open PrayerTracker
open PrayerTracker.Cookies
open PrayerTracker.ViewModels
open System
open System.Net
open System.Reflection
open System.Threading.Tasks

/// Create a select list from an enumeration
let toSelectList<'T> valFunc textFunc withDefault emptyText (items : 'T seq) =
  match items with null -> nullArg "items" | _ -> ()
  [ match withDefault with
    | true ->
        let s = PrayerTracker.Views.I18N.localizer.Force ()
        yield SelectListItem (sprintf "&mdash; %A &mdash;" s.[emptyText], "")
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
  let v = Assembly.GetExecutingAssembly().GetName().Version
#if (DEBUG)
  sprintf "v%A" v
#else
  seq {
    yield sprintf "v%d" v.Major
    match v.Minor with
    | 0 -> match v.Build with 0 -> () | _ -> yield sprintf ".0.%d" v.Build
    | _ ->
        yield sprintf ".%d" v.Minor
        match v.Build with 0 -> () | _ -> yield sprintf ".%d" v.Build
    }
  |> String.concat ""
#endif

/// An option of the currently signed-in user
let tryCurrentUser (ctx : HttpContext) =
  ctx.Session.GetUser ()

/// The currently signed-in user (will raise if none exists)
let currentUser ctx =
  match tryCurrentUser ctx with Some u -> u | None -> nullArg "User"

/// An option of the currently signed-in small group
let tryCurrentGroup (ctx : HttpContext) =
  ctx.Session.GetSmallGroup ()

/// The currently signed-in small group (will raise if none exists)
let currentGroup ctx =
  match tryCurrentGroup ctx with Some g -> g | None -> nullArg "SmallGroup"

/// Create the common view information heading
let viewInfo (ctx : HttpContext) startTicks =
  let msg =
    match ctx.Session.GetMessages () with
    | [] -> []
    | x ->
        ctx.Session.SetMessages []
        x
  match tryCurrentUser ctx with
  | Some u ->
      // The idle timeout is 2 hours; if the app pool is recycled or the actual session goes away, we will log the
      // user back in transparently using this cookie.  Every request resets the timer.
      let timeout =
        { Id       = u.userId
          GroupId  = (currentGroup ctx).smallGroupId
          Until    = DateTime.UtcNow.AddHours(2.).Ticks
          Password = ""
          }
      ctx.Response.Cookies.Append 
        (Key.Cookie.timeout, { timeout with Password = saltedTimeoutHash timeout }.toPayload (),
          CookieOptions (Expires = Nullable<DateTimeOffset> (DateTimeOffset (DateTime timeout.Until)), HttpOnly = true))
  | None -> ()
  { AppViewInfo.fresh with
      version      = appVersion
      messages     = msg
      requestStart = startTicks
      user         = ctx.Session.GetUser ()
      group        = ctx.Session.GetSmallGroup ()
      }

/// The view is the last parameter, so it can be composed
let renderHtml next ctx view =
  htmlView view next ctx

/// Display an error regarding form submission
let bindError (msg : string) next (ctx : HttpContext) =
  System.Console.WriteLine msg
  ctx.SetStatusCode 400
  text msg next ctx

/// Handler that will return a status code 404 and the text "Not Found"
let fourOhFour next (ctx : HttpContext) =
  ctx.SetStatusCode 404
  text "Not Found" next ctx


/// Handler to validate CSRF prevention token
let validateCSRF : HttpHandler =
  fun next ctx ->
    let antiForgery = ctx.GetService<IAntiforgery> ()
    task {
      let! isValid = antiForgery.IsRequestValidAsync ctx
      match isValid with
      | true -> return! next ctx
      | false ->
          return! (clearResponse >=> setStatusCode 400 >=> text "Quit hacking...") (fun _ -> Task.FromResult None) ctx
      }


/// Add a message to the session
let addUserMessage (ctx : HttpContext) msg =
  msg :: ctx.Session.GetMessages () |> ctx.Session.SetMessages

/// Convert a localized string to an HTML string
let htmlLocString (x : LocalizedString) =
  (WebUtility.HtmlEncode >> HtmlString) x.Value

let htmlString (x : LocalizedString) =
  HtmlString x.Value

/// Add an error message to the session
let addError ctx msg =
  addUserMessage ctx { UserMessage.Error with text = htmlLocString msg }

/// Add an informational message to the session
let addInfo ctx msg =
  addUserMessage ctx { UserMessage.Info with text = htmlLocString msg }

/// Add an informational HTML message to the session
let addHtmlInfo ctx msg =
  addUserMessage ctx { UserMessage.Info with text = htmlString msg }
  
/// Add a warning message to the session
let addWarning ctx msg =
  addUserMessage ctx { UserMessage.Warning with text = htmlLocString msg }


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


/// Require the given access role (also refreshes "Remember Me" user and group logons)
let requireAccess level : HttpHandler =
  
  /// Is there currently a user logged on?
  let isUserLoggedOn (ctx : HttpContext) =
    ctx.Session.GetUser () |> Option.isSome

  /// Log a user on from the timeout cookie
  let logOnUserFromTimeoutCookie (ctx : HttpContext) =
    task {
      // Make sure the cookie hasn't been tampered with
      try
        match TimeoutCookie.fromPayload ctx.Request.Cookies.[Key.Cookie.timeout] with
        | Some c when c.Password = saltedTimeoutHash c ->
            let  db   = ctx.dbContext ()
            let! user = db.TryUserById c.Id
            match user with
            | Some _ ->
                ctx.Session.SetUser user
                let! grp = db.TryGroupById c.GroupId
                ctx.Session.SetSmallGroup grp
            | _ -> ()
        | _ -> ()
      // If something above doesn't work, the user doesn't get logged in
      with _ -> ()
    }
  
  /// Attempt to log the user on from their stored cookie
  let logOnUserFromCookie (ctx : HttpContext) =
    task {
      match UserCookie.fromPayload ctx.Request.Cookies.[Key.Cookie.user] with
      | Some c ->
          let  db   = ctx.dbContext ()
          let! user = db.TryUserLogOnByCookie c.Id c.GroupId c.PasswordHash
          match user with
          | Some _ ->
              ctx.Session.SetUser user
              let! grp = db.TryGroupById c.GroupId
              ctx.Session.SetSmallGroup grp
              // Rewrite the cookie to extend the expiration
              ctx.Response.Cookies.Append (Key.Cookie.user, c.toPayload (), autoRefresh)
          | _ -> ()
      | _ -> ()
      }

  /// Is there currently a small group (or member thereof) logged on?
  let isGroupLoggedOn (ctx : HttpContext) =
    ctx.Session.GetSmallGroup () |> Option.isSome
    
  /// Attempt to log the small group on from their stored cookie
  let logOnGroupFromCookie (ctx : HttpContext) =
    task {
      match GroupCookie.fromPayload ctx.Request.Cookies.[Key.Cookie.group] with
      | Some c ->
          let! grp = (ctx.dbContext ()).TryGroupLogOnByCookie c.GroupId c.PasswordHash sha1Hash
          match grp with
          | Some _ ->
              ctx.Session.SetSmallGroup grp
              // Rewrite the cookie to extend the expiration
              ctx.Response.Cookies.Append (Key.Cookie.group, c.toPayload (), autoRefresh)
          | None -> ()
      | None -> ()
    }
    
  fun next ctx ->
    task {
      // Auto-logon user or class, if required
      match isUserLoggedOn ctx with
      | true -> ()
      | false ->
          do! logOnUserFromTimeoutCookie ctx
          match isUserLoggedOn ctx with
          | true -> ()
          | false ->
              do! logOnUserFromCookie ctx
              match isGroupLoggedOn ctx with true -> () | false -> do! logOnGroupFromCookie ctx

      match true with
      | _ when level |> List.contains Public -> return! next ctx
      | _ when level |> List.contains User  && isUserLoggedOn  ctx -> return! next ctx
      | _ when level |> List.contains Group && isGroupLoggedOn ctx -> return! next ctx
      | _ when level |> List.contains Admin && isUserLoggedOn  ctx ->
          match (currentUser ctx).isAdmin with
          | true -> return! next ctx
          | false ->
              let s = Views.I18N.localizer.Force ()
              addError ctx s.["You are not authorized to view the requested page."]
              return! redirectTo false "/unauthorized" next ctx
      | _ when level |> List.contains User ->
          // Redirect to the user log on page
          ctx.Session.SetString (Key.Session.redirectUrl, ctx.Request.GetEncodedUrl ())
          return! redirectTo false "/user/log-on" next ctx
      | _ when level |> List.contains Group ->
          // Redirect to the small group log on page
          ctx.Session.SetString (Key.Session.redirectUrl, ctx.Request.GetEncodedUrl ())
          return! redirectTo false "/small-group/log-on" next ctx
      | _ ->
          let s = Views.I18N.localizer.Force ()
          addError ctx s.["You are not authorized to view the requested page."]
          return! redirectTo false "/unauthorized" next ctx
      }
