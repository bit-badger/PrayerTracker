module PrayerTracker.Handlers.Church

open System.Threading.Tasks
open Giraffe
open PrayerTracker
open PrayerTracker.Data
open PrayerTracker.Entities
open PrayerTracker.ViewModels

/// Find statistics for the given church
let private findStats churchId = task {
    let! groups   = SmallGroups.countByChurch    churchId
    let! requests = PrayerRequests.countByChurch churchId
    let! users    = Users.countByChurch          churchId
    return shortGuid churchId.Value, { SmallGroups = groups; PrayerRequests = requests; Users = users }
}

// POST /church/[church-id]/delete
let delete chId : HttpHandler = requireAccess [ Admin ] >=> validateCsrf >=> fun next ctx -> task {
    let churchId = ChurchId chId
    match! Churches.tryById churchId with
    | Some church ->
        let! _, stats = findStats churchId
        do! Churches.deleteById churchId
        addInfo ctx
            ctx.Strings["The church “{0}” and its {1} small group(s) (with {2} prayer request(s)) were deleted successfully; revoked access from {3} user(s)",
                        church.Name, stats.SmallGroups, stats.PrayerRequests, stats.Users]
        return! redirectTo false "/churches" next ctx
    | None -> return! fourOhFour ctx
}

open System

// GET /church/[church-id]/edit
let edit churchId : HttpHandler = requireAccess [ Admin ] >=> fun next ctx -> task {
    if churchId = Guid.Empty then
        return!
            viewInfo ctx
            |> Views.Church.edit EditChurch.empty ctx
            |> renderHtml next ctx
    else
        match! Churches.tryById (ChurchId churchId) with
        | Some church -> 
            return!
                viewInfo ctx
                |> Views.Church.edit (EditChurch.fromChurch church) ctx
                |> renderHtml next ctx
        | None -> return! fourOhFour ctx
}

// GET /churches
let maintain : HttpHandler = requireAccess [ Admin ] >=> fun next ctx -> task {
    let! churches = Churches.all ()
    let  stats    = churches |> List.map (fun c -> findStats c.Id |> Async.AwaitTask |> Async.RunSynchronously)
    return!
        viewInfo ctx
        |> Views.Church.maintain churches (stats |> Map.ofList) ctx
        |> renderHtml next ctx
}

// POST /church/save
let save : HttpHandler = requireAccess [ Admin ] >=> validateCsrf >=> fun next ctx -> task {
    match! ctx.TryBindFormAsync<EditChurch> () with
    | Ok model ->
        let! church =
            if model.IsNew then Task.FromResult (Some { Church.empty with Id = (Guid.NewGuid >> ChurchId) () })
            else Churches.tryById (idFromShort ChurchId model.ChurchId)
        match church with
        | Some ch ->
            do! Churches.save (model.PopulateChurch ch)
            let act = ctx.Strings[if model.IsNew then "Added" else "Updated"].Value.ToLower ()
            addInfo ctx ctx.Strings["Successfully {0} church “{1}”", act, model.Name]
            return! redirectTo false "/churches" next ctx
        | None -> return! fourOhFour ctx
    | Result.Error e -> return! bindError e next ctx
}
