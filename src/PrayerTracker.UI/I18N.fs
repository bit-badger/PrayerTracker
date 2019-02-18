/// Internationalization for PrayerTracker
module PrayerTracker.Views.I18N

open Microsoft.AspNetCore.Mvc.Localization
open Microsoft.Extensions.Localization
open PrayerTracker

let mutable private stringLocFactory : IStringLocalizerFactory = null
let mutable private htmlLocFactory   : IHtmlLocalizerFactory = null
let private resAsmName = typeof<Common>.Assembly.GetName().Name
  
/// Set up the string and HTML localizer factories
let setUpFactories fac =
  stringLocFactory <- fac
  htmlLocFactory <- HtmlLocalizerFactory stringLocFactory

/// An instance of the common string localizer
let localizer = lazy (stringLocFactory.Create ("Common", resAsmName))
  
/// Get a view localizer
let forView (view : string) =
  htmlLocFactory.Create (sprintf "Views.%s" (view.Replace ('/', '.')), resAsmName)
