﻿module PrayerTracker.Handlers.Home

open System
open System.Globalization
open Giraffe
open Microsoft.AspNetCore.Http
open Microsoft.AspNetCore.Localization
open PrayerTracker

// GET /error/[error-code]
let error code : HttpHandler = requireAccess [ AccessLevel.Public ] >=> fun next ctx ->
    viewInfo ctx
    |> Views.Home.error code
    |> renderHtml next ctx

// GET /
let homePage : HttpHandler = requireAccess [ AccessLevel.Public ] >=> fun next ctx ->
    viewInfo ctx
    |> Views.Home.index
    |> renderHtml next ctx

// GET /language/[culture]
let language culture : HttpHandler = requireAccess [ AccessLevel.Public ] >=> fun next ctx ->
    try
        match culture with
        | null
        | ""
        | "en" -> "en-US"
        | "es" -> "es-MX"
        | _ -> $"{culture}-{culture.ToUpper ()}"
        |> (CultureInfo >> Option.ofObj)
    with
    | :? CultureNotFoundException
    | :? ArgumentException -> None
    |> function
    | Some c ->
        ctx.Response.Cookies.Append (
            CookieRequestCultureProvider.DefaultCookieName,
            CookieRequestCultureProvider.MakeCookieValue (RequestCulture c),
            CookieOptions (Expires = Nullable<DateTimeOffset> (DateTimeOffset (DateTime.Now.AddYears 1))))
    | _ -> ()
    let url = match string ctx.Request.Headers["Referer"] with null | "" -> "/" | r -> r
    redirectTo false url next ctx

// GET /legal/privacy-policy
let privacyPolicy : HttpHandler = requireAccess [ AccessLevel.Public ] >=> fun next ctx ->
    viewInfo ctx
    |> Views.Home.privacyPolicy
    |> renderHtml next ctx

// GET /legal/terms-of-service
let tos : HttpHandler = requireAccess [ AccessLevel.Public ] >=> fun next ctx ->
    viewInfo ctx
    |> Views.Home.termsOfService
    |> renderHtml next ctx

open Microsoft.AspNetCore.Authentication
open Microsoft.AspNetCore.Authentication.Cookies

// GET /log-off
let logOff : HttpHandler = requireAccess [ AccessLevel.Public ] >=> fun next ctx -> task {
    ctx.Session.Clear ()
    do! ctx.SignOutAsync CookieAuthenticationDefaults.AuthenticationScheme
    addHtmlInfo ctx ctx.Strings["Log Off Successful • Have a nice day!"]
    return! redirectTo false "/" next ctx
}

// GET /unauthorized
let unauthorized : HttpHandler = requireAccess [ AccessLevel.Public ] >=> fun next ctx ->
    viewInfo ctx
    |> Views.Home.unauthorized
    |> renderHtml next ctx
