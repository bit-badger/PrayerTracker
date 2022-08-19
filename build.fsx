#r "paket:
nuget Fake.DotNet.Cli
nuget Fake.DotNet.Testing.Expecto
nuget Fake.IO.FileSystem
nuget Fake.Core.Target //"
#load ".fake/build.fsx/intellisense.fsx"
open Fake.Core
open Fake.DotNet
open Fake.DotNet.Testing
open Fake.IO
open Fake.IO.Globbing.Operators
open Fake.Core.TargetOperators

Target.initEnvironment ()

/// The root path to the projects within this solution
let projPath = "src/PrayerTracker"

Target.create "Clean" (fun _ ->
    !! "src/**/bin"
    ++ "src/**/obj"
    |> Shell.cleanDirs 
)

Target.create "Test" (fun _ ->
    let testPath = $"{projPath}.Tests"
    DotNet.build (fun opts -> { opts with NoLogo = true }) $"{testPath}/PrayerTracker.Tests.fsproj"
    Expecto.run
        (fun opts -> { opts with WorkingDirectory = $"{testPath}/bin/Release/net6.0" })
        [ "PrayerTracker.Tests.dll" ])

Target.create "Publish" (fun _ ->
    DotNet.publish
        (fun opts -> { opts with Runtime = Some "linux-x64"; SelfContained = Some false; NoLogo = true })
        $"{projPath}/PrayerTracker.fsproj")

Target.create "All" ignore

"Clean"
    ==> "Test"
    ==> "Publish"
    ==> "All"

Target.runOrDefault "All"
