namespace PrayerTracker

open Microsoft.AspNetCore.Http

/// Middleware to add the starting ticks for the request
type RequestStartMiddleware (next : RequestDelegate) =
    
    member this.InvokeAsync (ctx : HttpContext) = task {
        ctx.Items[Key.startTime] <- ctx.Now
        return! next.Invoke ctx
    }


open System
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Hosting
open Microsoft.Extensions.Configuration

/// Module to hold configuration for the web app
[<RequireQualifiedAccess>]
module Configure =
  
    /// Set up the configuration for the app
    let configuration (ctx : WebHostBuilderContext) (cfg : IConfigurationBuilder) =
        cfg.SetBasePath(ctx.HostingEnvironment.ContentRootPath)
            .AddJsonFile("appsettings.json", optional = true, reloadOnChange = true)
            .AddJsonFile($"appsettings.{ctx.HostingEnvironment.EnvironmentName}.json", optional = true)
            .AddEnvironmentVariables()
        |> ignore

    open Microsoft.AspNetCore.Server.Kestrel.Core
    
    /// Configure Kestrel from appsettings.json
    let kestrel (ctx : WebHostBuilderContext) (opts : KestrelServerOptions) =
        (ctx.Configuration.GetSection >> opts.Configure >> ignore) "Kestrel"

    open System.Globalization
    open System.IO
    open Microsoft.AspNetCore.Authentication.Cookies
    open Microsoft.AspNetCore.Localization
    open Microsoft.Extensions.DependencyInjection
    open NeoSmart.Caching.Sqlite
    open NodaTime
    open Npgsql
    
    /// Configure ASP.NET Core's service collection (dependency injection container)
    let services (svc : IServiceCollection) =
        let _ = svc.AddOptions ()
        let _ = svc.AddLocalization (fun options -> options.ResourcesPath <- "Resources")
        let _ =
            svc.Configure<RequestLocalizationOptions> (fun (opts : RequestLocalizationOptions) ->
                let supportedCultures =[|
                    CultureInfo "en-US"; CultureInfo "en-GB"; CultureInfo "en-AU"; CultureInfo "en"
                    CultureInfo "es-MX"; CultureInfo "es-ES"; CultureInfo "es"
                |]
                opts.DefaultRequestCulture <- RequestCulture ("en-US", "en-US")
                opts.SupportedCultures     <- supportedCultures
                opts.SupportedUICultures   <- supportedCultures)
        let _ =
            svc.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                .AddCookie (fun opts ->
                    opts.ExpireTimeSpan    <- TimeSpan.FromMinutes 120.
                    opts.SlidingExpiration <- true
                    opts.AccessDeniedPath  <- "/error/403")
        let _ = svc.AddAuthorization ()
        let _ = svc.AddSqliteCache (fun opts -> opts.CachePath <- Path.Combine (".", "session.db"))
        let _ = svc.AddSession ()
        let _ = svc.AddAntiforgery ()
        let _ = svc.AddRouting ()
        let _ = svc.AddSingleton<IClock> SystemClock.Instance
        let _ =
            svc.AddScoped<NpgsqlConnection>(fun sp ->
                let cfg  = sp.GetService<IConfiguration> ()
                let conn = new NpgsqlConnection (cfg.GetConnectionString "PrayerTracker")
                conn.OpenAsync () |> Async.AwaitTask |> Async.RunSynchronously
                conn)
        let _ = NpgsqlConnection.GlobalTypeMapper.UseNodaTime ()
        ()
    
    open Giraffe
    
    let noWeb : HttpHandler = fun next ctx ->
        redirectTo true $"""/{string ctx.Request.RouteValues["path"]}""" next ctx
        
    open Giraffe.EndpointRouting
    
    /// Routes for PrayerTracker
    let routes = [
        route "/web/{**path}" noWeb
        GET_HEAD [
            subRoute "/church" [
                route  "es"       Handlers.Church.maintain
                routef "/%O/edit" Handlers.Church.edit
            ]
            route    "/class/logon" (redirectTo true "/small-group/log-on")
            routef   "/error/%s"    Handlers.Home.error
            routef   "/language/%s" Handlers.Home.language
            subRoute "/legal" [
                route "/privacy-policy"   Handlers.Home.privacyPolicy
                route "/terms-of-service" Handlers.Home.tos
            ]
            route    "/log-off" Handlers.Home.logOff
            subRoute "/prayer-request" [
                route  "s"           (Handlers.PrayerRequest.maintain true)
                routef "s/email/%s"  Handlers.PrayerRequest.email
                route  "s/inactive"  (Handlers.PrayerRequest.maintain false)
                route  "s/lists"     Handlers.PrayerRequest.lists
                routef "s/%O/list"   Handlers.PrayerRequest.list
                route  "s/maintain"  (redirectTo true "/prayer-requests")
                routef "s/print/%s"  Handlers.PrayerRequest.print
                route  "s/view"      (Handlers.PrayerRequest.view None)
                routef "s/view/%s"   (Some >> Handlers.PrayerRequest.view)
                routef "/%O/edit"    Handlers.PrayerRequest.edit
                routef "/%O/expire"  Handlers.PrayerRequest.expire
                routef "/%O/restore" Handlers.PrayerRequest.restore
            ]
            subRoute "/small-group" [
                route  ""                Handlers.SmallGroup.overview
                route  "s"               Handlers.SmallGroup.maintain
                route  "/announcement"   Handlers.SmallGroup.announcement
                routef "/%O/edit"        Handlers.SmallGroup.edit
                route  "/log-on"         (Handlers.SmallGroup.logOn None)
                routef "/log-on/%O"      (Some >> Handlers.SmallGroup.logOn)
                route  "/logon"          (redirectTo true "/small-group/log-on")
                routef "/member/%O/edit" Handlers.SmallGroup.editMember
                route  "/members"        Handlers.SmallGroup.members
                route  "/preferences"    Handlers.SmallGroup.preferences
            ]
            route    "/unauthorized" Handlers.Home.unauthorized
            subRoute "/user" [
                route  "s"                Handlers.User.maintain
                routef "/%O/edit"         Handlers.User.edit
                routef "/%O/small-groups" Handlers.User.smallGroups
                route  "/log-on"          Handlers.User.logOn
                route  "/logon"           (redirectTo true "/user/log-on")
                route  "/password"        Handlers.User.password
            ]
            route    "/" Handlers.Home.homePage
        ]
        POST [
            subRoute "/church" [
                routef "/%O/delete" Handlers.Church.delete
                route  "/save"      Handlers.Church.save
            ]
            subRoute "/prayer-request" [
                routef "/%O/delete" Handlers.PrayerRequest.delete
                route  "/save"      Handlers.PrayerRequest.save
            ]
            subRoute "/small-group" [
                route  "/announcement/send" Handlers.SmallGroup.sendAnnouncement
                routef "/%O/delete"         Handlers.SmallGroup.delete
                route  "/log-on/submit"     Handlers.SmallGroup.logOnSubmit
                routef "/member/%O/delete"  Handlers.SmallGroup.deleteMember
                route  "/member/save"       Handlers.SmallGroup.saveMember
                route  "/preferences/save"  Handlers.SmallGroup.savePreferences
                route  "/save"              Handlers.SmallGroup.save
            ]
            subRoute "/user" [
                routef "/%O/delete"         Handlers.User.delete
                route  "/edit/save"         Handlers.User.save
                route  "/log-on"            Handlers.User.doLogOn
                route  "/password/change"   Handlers.User.changePassword
                route  "/small-groups/save" Handlers.User.saveGroups
            ]
        ]
    ]

    open Microsoft.Extensions.Logging

    /// Giraffe error handler
    let errorHandler (ex : exn) (logger : ILogger) =
        logger.LogError (EventId(), ex, "An unhandled exception has occurred while executing the request.")
        clearResponse >=> setStatusCode 500 >=> text ex.Message
    
    open Microsoft.Extensions.Hosting
    
    /// Configure logging
    let logging (log : ILoggingBuilder) =
        let env = log.Services.BuildServiceProvider().GetService<IWebHostEnvironment> ()
        if env.IsDevelopment () then log else log.AddFilter (fun l -> l > LogLevel.Information)
        |> function l -> l.AddConsole().AddDebug()
        |> ignore
    
    open Microsoft.Extensions.Localization
    open Microsoft.Extensions.Options
    
    /// Configure the application
    let app (app : IApplicationBuilder) =
        let env = app.ApplicationServices.GetRequiredService<IWebHostEnvironment>()
        if env.IsDevelopment () then
            let _ = app.UseDeveloperExceptionPage ()
            ()
        else
            let _ = app.UseGiraffeErrorHandler errorHandler
            ()
        
        let _ = app.UseStatusCodePagesWithReExecute "/error/{0}"
        let _ = app.UseStaticFiles ()
        let _ = app.UseCookiePolicy (CookiePolicyOptions (MinimumSameSitePolicy = SameSiteMode.Strict))
        let _ = app.UseMiddleware<RequestStartMiddleware> ()
        let _ = app.UseRouting ()
        let _ = app.UseSession ()
        let _ = app.UseRequestLocalization
                    (app.ApplicationServices.GetService<IOptions<RequestLocalizationOptions>>().Value)
        let _ = app.UseAuthentication ()
        let _ = app.UseAuthorization ()
        let _ = app.UseEndpoints (fun e -> e.MapGiraffeEndpoints routes)
        Views.I18N.setUpFactories <| app.ApplicationServices.GetRequiredService<IStringLocalizerFactory> ()


/// The web application
module App =
    
    open System.Text
    open Microsoft.Extensions.DependencyInjection
    open Npgsql
    open Npgsql.FSharp
    open PrayerTracker.Entities
    
    let migratePasswords (app : IWebHost) =
        task {
            let config = app.Services.GetService<IConfiguration> ()
            use conn   = new NpgsqlConnection (config.GetConnectionString "PrayerTracker")
            do! conn.OpenAsync ()
            let! v1Users =
                   Sql.existingConnection conn
                |> Sql.query "SELECT id, password_hash FROM pt.pt_user WHERE salt IS NULL"
                |> Sql.executeAsync (fun row -> UserId (row.uuid "id"), row.string "password_hash") 
            for userId, oldHash in v1Users do
                let pw = Convert.ToBase64String [| 254uy; yield! (Encoding.UTF8.GetBytes oldHash) |] 
                let! _ =
                       Sql.existingConnection conn
                    |> Sql.query "UPDATE pt.pt_user SET password_hash = @hash WHERE id = @id"
                    |> Sql.parameters [ "@id", Sql.uuid userId.Value; "@hash", Sql.string pw ]
                    |> Sql.executeNonQueryAsync
                ()
            printfn $"Updated {v1Users.Length} users with version 1 password"
            let! v2Users =
                   Sql.existingConnection conn
                |> Sql.query "SELECT id, password_hash, salt FROM pt.pt_user WHERE salt IS NOT NULL"
                |> Sql.executeAsync (fun row -> UserId (row.uuid "id"), row.string "password_hash", row.uuid "salt")
            for userId, oldHash, salt in v2Users do
                let pw =
                    Convert.ToBase64String
                        [| 255uy; yield! (salt.ToByteArray ()); yield! (Encoding.UTF8.GetBytes oldHash) |]
                let! _ =
                       Sql.existingConnection conn
                    |> Sql.query "UPDATE pt.pt_user SET password_hash = @hash WHERE id = @id"
                    |> Sql.parameters [ "@id", Sql.uuid userId.Value; "@hash", Sql.string pw ]
                    |> Sql.executeNonQueryAsync
                ()
            printfn $"Updated {v2Users.Length} users with version 2 password"
        } |> Async.AwaitTask |> Async.RunSynchronously
    
    open System.IO

    [<EntryPoint>]
    let main args =
        let contentRoot = Directory.GetCurrentDirectory ()
        let app =
            WebHostBuilder()
                .UseContentRoot(contentRoot)
                .ConfigureAppConfiguration(Configure.configuration)
                .UseKestrel(Configure.kestrel)
                .UseWebRoot(Path.Combine (contentRoot, "wwwroot"))
                .ConfigureServices(Configure.services)
                .ConfigureLogging(Configure.logging)
                .Configure(System.Action<IApplicationBuilder> Configure.app)
                .Build()
        if args.Length > 0 then
            if args[0] = "migrate-passwords" then migratePasswords app
            else printfn $"Unrecognized option {args[0]}"
        else app.Run ()
        0
