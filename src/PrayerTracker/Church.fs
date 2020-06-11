module PrayerTracker.Handlers.Church

open FSharp.Control.Tasks.V2.ContextInsensitive
open Giraffe
open PrayerTracker
open PrayerTracker.Entities
open PrayerTracker.ViewModels
open PrayerTracker.Views.CommonFunctions
open System
open System.Threading.Tasks

/// Find statistics for the given church
let private findStats (db : AppDbContext) churchId =
  task {
    let! grps = db.CountGroupsByChurch   churchId
    let! reqs = db.CountRequestsByChurch churchId
    let! usrs = db.CountUsersByChurch    churchId
    return flatGuid churchId, { smallGroups = grps; prayerRequests = reqs; users = usrs }
    }


/// POST /church/[church-id]/delete
let delete churchId : HttpHandler =
  requireAccess [ Admin ]
  >=> validateCSRF
  >=> fun next ctx ->
    let db = ctx.dbContext ()
    task {
      match! db.TryChurchById churchId with
      | Some church ->
          let! _, stats = findStats db churchId
          db.RemoveEntry church
          let! _ = db.SaveChangesAsync ()
          let  s = Views.I18N.localizer.Force ()
          addInfo ctx
            s.["The church {0} and its {1} small groups (with {2} prayer request(s)) were deleted successfully; revoked access from {3} user(s)",
                church.name, stats.smallGroups, stats.prayerRequests, stats.users]
          return! redirectTo false "/web/churches" next ctx
      | None -> return! fourOhFour next ctx
      }


/// GET /church/[church-id]/edit
let edit churchId : HttpHandler =
  requireAccess [ Admin ]
  >=> fun next ctx ->
    let startTicks = DateTime.Now.Ticks
    task {
      match churchId with
      | x when x = Guid.Empty ->
          return!
            viewInfo ctx startTicks
            |> Views.Church.edit EditChurch.empty ctx
            |> renderHtml next ctx
      | _ ->
          let db = ctx.dbContext ()
          match! db.TryChurchById churchId with
          | Some church -> 
              return!
                viewInfo ctx startTicks
                |> Views.Church.edit (EditChurch.fromChurch church) ctx
                |> renderHtml next ctx
          | None -> return! fourOhFour next ctx
      }


/// GET /churches
let maintain : HttpHandler =
  requireAccess [ Admin ]
  >=> fun next ctx ->
    let startTicks = DateTime.Now.Ticks
    let await      = Async.AwaitTask >> Async.RunSynchronously
    let db         = ctx.dbContext ()
    task {
      let! churches = db.AllChurches ()
      let  stats    = churches |> List.map (fun c -> await (findStats db c.churchId))
      return!
        viewInfo ctx startTicks
        |> Views.Church.maintain churches (stats |> Map.ofList) ctx
        |> renderHtml next ctx
      }


/// POST /church/save
let save : HttpHandler =
  requireAccess [ Admin ]
  >=> validateCSRF
  >=> fun next ctx ->
    task {
      match! ctx.TryBindFormAsync<EditChurch> () with
      | Ok m ->
          let db = ctx.dbContext ()
          let! church =
            match m.isNew () with
            | true -> Task.FromResult<Church option>(Some { Church.empty with churchId = Guid.NewGuid () })
            | false -> db.TryChurchById m.churchId
          match church with
          | Some ch ->
              m.populateChurch ch
              |> (match m.isNew () with true -> db.AddEntry | false -> db.UpdateEntry)
              let! _   = db.SaveChangesAsync ()
              let  s   = Views.I18N.localizer.Force ()
              let  act = s.[match m.isNew () with true -> "Added" | _ -> "Updated"].Value.ToLower ()
              addInfo ctx s.["Successfully {0} church “{1}”", act, m.name]
              return! redirectTo false "/web/churches" next ctx
          | None -> return! fourOhFour next ctx
      | Error e -> return! bindError e next ctx
      }
