module PrayerTracker.Handlers.Church

open System
open System.Threading.Tasks
open Giraffe
open PrayerTracker
open PrayerTracker.Entities
open PrayerTracker.ViewModels
open PrayerTracker.Views.CommonFunctions

/// Find statistics for the given church
let private findStats (db : AppDbContext) churchId = task {
    let! grps = db.CountGroupsByChurch   churchId
    let! reqs = db.CountRequestsByChurch churchId
    let! usrs = db.CountUsersByChurch    churchId
    return flatGuid churchId, { smallGroups = grps; prayerRequests = reqs; users = usrs }
}


/// POST /church/[church-id]/delete
let delete churchId : HttpHandler = requireAccess [ Admin ] >=> validateCSRF >=> fun next ctx -> task {
    match! ctx.db.TryChurchById churchId with
    | Some church ->
        let! _, stats = findStats ctx.db churchId
        ctx.db.RemoveEntry church
        let! _ = ctx.db.SaveChangesAsync ()
        let  s = Views.I18N.localizer.Force ()
        addInfo ctx
          s["The church {0} and its {1} small groups (with {2} prayer request(s)) were deleted successfully; revoked access from {3} user(s)",
              church.name, stats.smallGroups, stats.prayerRequests, stats.users]
        return! redirectTo false "/churches" next ctx
    | None -> return! fourOhFour next ctx
}


/// GET /church/[church-id]/edit
let edit churchId : HttpHandler = requireAccess [ Admin ] >=> fun next ctx -> task {
    let startTicks = DateTime.Now.Ticks
    if churchId = Guid.Empty then
        return!
            viewInfo ctx startTicks
            |> Views.Church.edit EditChurch.empty ctx
            |> renderHtml next ctx
    else
        match! ctx.db.TryChurchById churchId with
        | Some church -> 
            return!
                viewInfo ctx startTicks
                |> Views.Church.edit (EditChurch.fromChurch church) ctx
                |> renderHtml next ctx
        | None -> return! fourOhFour next ctx
}


/// GET /churches
let maintain : HttpHandler = requireAccess [ Admin ] >=> fun next ctx -> task {
    let  startTicks = DateTime.Now.Ticks
    let  await      = Async.AwaitTask >> Async.RunSynchronously
    let! churches   = ctx.db.AllChurches ()
    let  stats      = churches |> List.map (fun c -> await (findStats ctx.db c.churchId))
    return!
        viewInfo ctx startTicks
        |> Views.Church.maintain churches (stats |> Map.ofList) ctx
        |> renderHtml next ctx
}


/// POST /church/save
let save : HttpHandler = requireAccess [ Admin ] >=> validateCSRF >=> fun next ctx -> task {
    match! ctx.TryBindFormAsync<EditChurch> () with
    | Ok m ->
        let! church =
            if m.IsNew then Task.FromResult (Some { Church.empty with churchId = Guid.NewGuid () })
            else ctx.db.TryChurchById m.ChurchId
        match church with
        | Some ch ->
            m.PopulateChurch ch
            |> (if m.IsNew then ctx.db.AddEntry else ctx.db.UpdateEntry)
            let! _   = ctx.db.SaveChangesAsync ()
            let  s   = Views.I18N.localizer.Force ()
            let  act = s[if m.IsNew then "Added" else "Updated"].Value.ToLower ()
            addInfo ctx s["Successfully {0} church “{1}”", act, m.Name]
            return! redirectTo false "/churches" next ctx
        | None -> return! fourOhFour next ctx
    | Result.Error e -> return! bindError e next ctx
}
