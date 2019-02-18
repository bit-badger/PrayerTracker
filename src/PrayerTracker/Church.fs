module PrayerTracker.Handlers.Church

open FSharp.Control.Tasks.V2.ContextInsensitive
open Giraffe
open PrayerTracker
open PrayerTracker.Entities
open PrayerTracker.ViewModels
open System
open System.Threading.Tasks

/// Find statistics for the given church
let private findStats (db : AppDbContext) churchId =
  task {
    let! grps = db.CountGroupsByChurch   churchId
    let! reqs = db.CountRequestsByChurch churchId
    let! usrs = db.CountUsersByChurch    churchId
    return (churchId.ToString "N"), { smallGroups = grps; prayerRequests = reqs; users = usrs }
    }


/// POST /church/[church-id]/delete
let delete churchId : HttpHandler =
  requireAccess [ Admin ]
  >=> validateCSRF
  >=> fun next ctx ->
    let db = ctx.dbContext ()
    task {
      let! church = db.TryChurchById churchId
      match church with
      | Some ch ->
          let! _, stats = findStats db churchId
          db.RemoveEntry ch
          let! _ = db.SaveChangesAsync ()
          let  s = Views.I18N.localizer.Force ()
          addInfo ctx
            s.["The church {0} and its {1} small groups (with {2} prayer request(s)) were deleted successfully; revoked access from {3} user(s)",
                ch.name, stats.smallGroups, stats.prayerRequests, stats.users]
          return! redirectTo false "/churches" next ctx
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
          let  db     = ctx.dbContext ()
          let! church = db.TryChurchById churchId
          match church with
          | Some ch -> 
              return!
                viewInfo ctx startTicks
                |> Views.Church.edit (EditChurch.fromChurch ch) ctx
                |> renderHtml next ctx
          | None -> return! fourOhFour next ctx
      }


/// GET /churches
let maintain : HttpHandler =
  requireAccess [ Admin ]
  >=> fun next ctx ->
    let startTicks = DateTime.Now.Ticks
    let db         = ctx.dbContext ()
    task {
      let! churches = db.AllChurches ()
      let! stats =
        churches
        |> Seq.ofList
        |> Seq.map (fun c -> findStats db c.churchId)
        |> Task.WhenAll
      return!
        viewInfo ctx startTicks
        |> Views.Church.maintain churches (stats |> Map.ofArray) ctx
        |> renderHtml next ctx
      }


/// POST /church/save
let save : HttpHandler =
  requireAccess [ Admin ]
  >=> validateCSRF
  >=> fun next ctx ->
    task {
      let! result = ctx.TryBindFormAsync<EditChurch> ()
      match result with
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
              return! redirectTo false "/churches" next ctx
          | None -> return! fourOhFour next ctx
      | Error e -> return! bindError e next ctx
      }
