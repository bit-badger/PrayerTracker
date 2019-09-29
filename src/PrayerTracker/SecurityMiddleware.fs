namespace PrayerTracker

open FSharp.Control.Tasks.V2.ContextInsensitive
open Microsoft.AspNetCore.Http
open Cookies
open System.IdentityModel.Tokens.Jwt
open System
open System.Security.Claims
open Microsoft.IdentityModel.Tokens

/// Middleware to obtain the current user or group from a cookie or the authorization header
type SecurityMiddleware (next : RequestDelegate) =

  /// Try to get a JWT from the user's logged in cookie
  let tryGetJwtFromUserCookie (ctx : HttpContext) =
    match UserCookie.fromPayload ctx.Request.Cookies.[Key.Cookie.user] with
    | Some cookie -> Some cookie.token
    | None -> None

  /// Try to get a JWT from a group's logged in cookie
  let tryGetJwtFromGroupCookie (ctx : HttpContext) =
    match GroupCookie.fromPayload ctx.Request.Cookies.[Key.Cookie.group] with
    | Some cookie -> Some cookie.token
    | None -> None

  /// Try to get a JWT from the Authorization header
  let tryGetJwtFromAuthHeader (ctx : HttpContext) =
    match ctx.Request.Headers.["Authorization"] |> Seq.tryFind (fun x -> x.StartsWith "Bearer ") with
    | Some hdr -> Some (hdr.Replace ("Bearer ", ""))
    | None -> None

  /// Attempt to get the user either from a cookie or an Authorization header
  member __.Invoke ctx =
    task {
      let jwt =
        seq {
          tryGetJwtFromUserCookie  ctx
          tryGetJwtFromGroupCookie ctx
          tryGetJwtFromAuthHeader  ctx
          }
        |> Seq.tryFind Option.isSome
        |> Option.flatten
      match jwt with
      | Some token ->
          let handler = JwtSecurityTokenHandler ()
          let mutable x : JwtSecurityToken ref = JwtSecurityToken () // does not build
          let usr = handler.ValidateToken(token, TokenValidationParameters (), x) // JwtSecurityTokenHandler. ClaimsPrincipal ()
          ctx.User <- usr
          ()
      | None -> ()
      do! next.Invoke ctx
    }


