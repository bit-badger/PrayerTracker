module PrayerTracker.Tests.TestLocalization

open Microsoft.Extensions.Localization
open Microsoft.Extensions.Logging.Abstractions
open Microsoft.Extensions.Options
open PrayerTracker

let _s =
  let asm  = typeof<Common>.Assembly.GetName().Name
  let opts = 
    { new IOptions<LocalizationOptions> with
        member __.Value with get () = LocalizationOptions (ResourcesPath = "Resources")
      }
  ResourceManagerStringLocalizerFactory(opts, new NullLoggerFactory ()).Create("Common", asm)
