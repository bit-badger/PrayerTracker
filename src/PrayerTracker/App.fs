namespace PrayerTracker

open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Hosting

/// Module to hold configuration for the web app
[<RequireQualifiedAccess>]
module Configure =
  
  open Cookies
  open Giraffe
  open Giraffe.EndpointRouting
  open Microsoft.AspNetCore.Localization
  open Microsoft.AspNetCore.Server.Kestrel.Core
  open Microsoft.EntityFrameworkCore
  open Microsoft.Extensions.Configuration
  open Microsoft.Extensions.DependencyInjection
  open Microsoft.Extensions.Hosting
  open Microsoft.Extensions.Localization
  open Microsoft.Extensions.Logging
  open Microsoft.Extensions.Options
  open NodaTime
  open System.Globalization

  /// Set up the configuration for the app
  let configuration (ctx : WebHostBuilderContext) (cfg : IConfigurationBuilder) =
    cfg.SetBasePath(ctx.HostingEnvironment.ContentRootPath)
      .AddJsonFile("appsettings.json", optional = true, reloadOnChange = true)
      .AddJsonFile($"appsettings.{ctx.HostingEnvironment.EnvironmentName}.json", optional = true)
      .AddEnvironmentVariables()
    |> ignore

  /// Configure Kestrel from appsettings.json
  let kestrel (ctx : WebHostBuilderContext) (opts : KestrelServerOptions) =
    (ctx.Configuration.GetSection >> opts.Configure >> ignore) "Kestrel"

  let services (svc : IServiceCollection) =
    svc.AddOptions()
      .AddLocalization(fun options -> options.ResourcesPath <- "Resources")
      .Configure<RequestLocalizationOptions>(
        fun (opts : RequestLocalizationOptions) ->
          let supportedCultures =
            [| CultureInfo "en-US"; CultureInfo "en-GB"; CultureInfo "en-AU"; CultureInfo "en"
               CultureInfo "es-MX"; CultureInfo "es-ES"; CultureInfo "es"
              |]
          opts.DefaultRequestCulture <- RequestCulture ("en-US", "en-US")
          opts.SupportedCultures     <- supportedCultures
          opts.SupportedUICultures   <- supportedCultures)
      .AddDistributedMemoryCache()
      .AddSession()
      .AddAntiforgery()
      .AddRouting()
      .AddSingleton<IClock>(SystemClock.Instance)
    |> ignore
    let config = svc.BuildServiceProvider().GetRequiredService<IConfiguration>()
    let crypto = config.GetSection "CookieCrypto"
    CookieCrypto (crypto.["Key"], crypto.["IV"]) |> setCrypto
    svc.AddDbContext<AppDbContext>(
        fun options ->
          options.UseNpgsql (config.GetConnectionString "PrayerTracker") |> ignore)
    |> ignore

  /// Routes for PrayerTracker
  let routes =
    [ subRoute "/web" [
        GET_HEAD [
          subRoute "/church" [
            route  "es"       Handlers.Church.maintain
            routef "/%O/edit" Handlers.Church.edit
            ]
          route  "/class/logon" (redirectTo true "/web/small-group/log-on")
          routef "/error/%s"    Handlers.Home.error
          routef "/language/%s" Handlers.Home.language
          subRoute "/legal" [
            route "/privacy-policy"   Handlers.Home.privacyPolicy
            route "/terms-of-service" Handlers.Home.tos
            ]
          route "/log-off" Handlers.Home.logOff
          subRoute "/prayer-request" [
            route  "s"           (Handlers.PrayerRequest.maintain true)
            routef "s/email/%s"  Handlers.PrayerRequest.email
            route  "s/inactive"  (Handlers.PrayerRequest.maintain false)
            route  "s/lists"     Handlers.PrayerRequest.lists
            routef "s/%O/list"   Handlers.PrayerRequest.list
            route  "s/maintain"  (redirectTo true "/web/prayer-requests")
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
            route  "/logon"          (redirectTo true "/web/small-group/log-on")
            routef "/member/%O/edit" Handlers.SmallGroup.editMember
            route  "/members"        Handlers.SmallGroup.members
            route  "/preferences"    Handlers.SmallGroup.preferences
            ]
          route "/unauthorized" Handlers.Home.unauthorized
          subRoute "/user" [
            route  "s"                Handlers.User.maintain
            routef "/%O/edit"         Handlers.User.edit
            routef "/%O/small-groups" Handlers.User.smallGroups
            route  "/log-on"          Handlers.User.logOn
            route  "/logon"           (redirectTo true "/web/user/log-on")
            route  "/password"        Handlers.User.password
            ]
          route "/" Handlers.Home.homePage
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
      // Temp redirect to new URLs
      route "/" (redirectTo false "/web/")
      ]

  /// Giraffe error handler
  let errorHandler (ex : exn) (logger : ILogger) =
    logger.LogError(EventId(), ex, "An unhandled exception has occurred while executing the request.")
    clearResponse >=> setStatusCode 500 >=> text ex.Message
  
  /// Configure logging
  let logging (log : ILoggingBuilder) =
    let env = log.Services.BuildServiceProvider().GetService<IWebHostEnvironment> ()
    match env.IsDevelopment () with
    | true -> log
    | false -> log.AddFilter (fun l -> l > LogLevel.Information)
    |> function l -> l.AddConsole().AddDebug()
    |> ignore
  
  let app (app : IApplicationBuilder) =
    let env = app.ApplicationServices.GetRequiredService<IWebHostEnvironment>()
    (match env.IsDevelopment () with
     | true ->
        app.UseDeveloperExceptionPage ()
     | false ->
        try
          use scope = app.ApplicationServices.GetRequiredService<IServiceScopeFactory>().CreateScope ()
          scope.ServiceProvider.GetService<AppDbContext>().Database.Migrate ()
        with _ -> () // om nom nom
        app.UseGiraffeErrorHandler errorHandler)
      .UseStatusCodePagesWithReExecute("/error/{0}")
      .UseStaticFiles()
      .UseRouting()
      .UseSession()
      .UseRequestLocalization(app.ApplicationServices.GetService<IOptions<RequestLocalizationOptions>>().Value)
      .UseEndpoints (fun e -> e.MapGiraffeEndpoints routes)
      |> ignore
    Views.I18N.setUpFactories <| app.ApplicationServices.GetRequiredService<IStringLocalizerFactory> ()


/// The web application
module App =
  
  open System.IO

  [<EntryPoint>]
  let main _ =
    let contentRoot = Directory.GetCurrentDirectory ()
    WebHostBuilder()
      .UseContentRoot(contentRoot)
      .ConfigureAppConfiguration(Configure.configuration)
      .UseKestrel(Configure.kestrel)
      .UseWebRoot(Path.Combine (contentRoot, "wwwroot"))
      .ConfigureServices(Configure.services)
      .ConfigureLogging(Configure.logging)
      .Configure(System.Action<IApplicationBuilder> Configure.app)
      .Build()
      .Run ()
    0
