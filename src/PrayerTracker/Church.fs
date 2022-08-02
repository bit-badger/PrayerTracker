module PrayerTracker.Handlers.Church

open Giraffe
open PrayerTracker
open PrayerTracker.Entities
open PrayerTracker.ViewModels

/// Find statistics for the given church
let private findStats (db : AppDbContext) churchId = task {
    let! grps = db.CountGroupsByChurch   churchId
    let! reqs = db.CountRequestsByChurch churchId
    let! usrs = db.CountUsersByChurch    churchId
    return shortGuid churchId.Value, { SmallGroups = grps; PrayerRequests = reqs; Users = usrs }
}

/// POST /church/[church-id]/delete
let delete chId : HttpHandler = requireAccess [ Admin ] >=> validateCsrf >=> fun next ctx -> task {
    let churchId = ChurchId chId
    match! ctx.db.TryChurchById churchId with
    | Some church ->
        let! _, stats = findStats ctx.db churchId
        ctx.db.RemoveEntry church
        let! _ = ctx.db.SaveChangesAsync ()
        let  s = Views.I18N.localizer.Force ()
        addInfo ctx
          s["The church {0} and its {1} small groups (with {2} prayer request(s)) were deleted successfully; revoked access from {3} user(s)",
              church.Name, stats.SmallGroups, stats.PrayerRequests, stats.Users]
        return! redirectTo false "/churches" next ctx
    | None -> return! fourOhFour next ctx
}

open System

/// GET /church/[church-id]/edit
let edit churchId : HttpHandler = requireAccess [ Admin ] >=> fun next ctx -> task {
    let startTicks = DateTime.Now.Ticks
    if churchId = Guid.Empty then
        return!
            viewInfo ctx startTicks
            |> Views.Church.edit EditChurch.empty ctx
            |> renderHtml next ctx
    else
        match! ctx.db.TryChurchById (ChurchId churchId) with
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
    let  stats      = churches |> List.map (fun c -> await (findStats ctx.db c.Id))
    return!
        viewInfo ctx startTicks
        |> Views.Church.maintain churches (stats |> Map.ofList) ctx
        |> renderHtml next ctx
}

open System.Threading.Tasks

/// POST /church/save
let save : HttpHandler = requireAccess [ Admin ] >=> validateCsrf >=> fun next ctx -> task {
    match! ctx.TryBindFormAsync<EditChurch> () with
    | Ok model ->
        let! church =
            if model.IsNew then Task.FromResult (Some { Church.empty with Id = (Guid.NewGuid >> ChurchId) () })
            else ctx.db.TryChurchById (idFromShort ChurchId model.ChurchId)
        match church with
        | Some ch ->
            model.PopulateChurch ch
            |> (if model.IsNew then ctx.db.AddEntry else ctx.db.UpdateEntry)
            let! _   = ctx.db.SaveChangesAsync ()
            let  s   = Views.I18N.localizer.Force ()
            let  act = s[if model.IsNew then "Added" else "Updated"].Value.ToLower ()
            addInfo ctx s["Successfully {0} church “{1}”", act, model.Name]
            return! redirectTo false "/churches" next ctx
        | None -> return! fourOhFour next ctx
    | Result.Error e -> return! bindError e next ctx
}
