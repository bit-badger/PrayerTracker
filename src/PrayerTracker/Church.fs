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
    let  churchId = ChurchId chId
    use! conn     = ctx.Conn
    match! Data.Churches.tryById churchId conn with
    | Some church ->
        let! _, stats = findStats ctx.Db churchId
        ctx.Db.RemoveEntry church
        let! _ = ctx.Db.SaveChangesAsync ()
        let  s = Views.I18N.localizer.Force ()
        addInfo ctx
          s["The church {0} and its {1} small groups (with {2} prayer request(s)) were deleted successfully; revoked access from {3} user(s)",
              church.Name, stats.SmallGroups, stats.PrayerRequests, stats.Users]
        return! redirectTo false "/churches" next ctx
    | None -> return! fourOhFour ctx
}

open System

/// GET /church/[church-id]/edit
let edit churchId : HttpHandler = requireAccess [ Admin ] >=> fun next ctx -> task {
    if churchId = Guid.Empty then
        return!
            viewInfo ctx
            |> Views.Church.edit EditChurch.empty ctx
            |> renderHtml next ctx
    else
        use! conn = ctx.Conn
        match! Data.Churches.tryById (ChurchId churchId) conn with
        | Some church -> 
            return!
                viewInfo ctx
                |> Views.Church.edit (EditChurch.fromChurch church) ctx
                |> renderHtml next ctx
        | None -> return! fourOhFour ctx
}

/// GET /churches
let maintain : HttpHandler = requireAccess [ Admin ] >=> fun next ctx -> task {
    let  await    = Async.AwaitTask >> Async.RunSynchronously
    let! churches = ctx.Db.AllChurches ()
    let  stats    = churches |> List.map (fun c -> await (findStats ctx.Db c.Id))
    return!
        viewInfo ctx
        |> Views.Church.maintain churches (stats |> Map.ofList) ctx
        |> renderHtml next ctx
}

open System.Threading.Tasks

/// POST /church/save
let save : HttpHandler = requireAccess [ Admin ] >=> validateCsrf >=> fun next ctx -> task {
    match! ctx.TryBindFormAsync<EditChurch> () with
    | Ok model ->
        let! conn = ctx.Conn
        let! church =
            if model.IsNew then Task.FromResult (Some { Church.empty with Id = (Guid.NewGuid >> ChurchId) () })
            else Data.Churches.tryById (idFromShort ChurchId model.ChurchId) conn
        match church with
        | Some ch ->
            model.PopulateChurch ch
            |> (if model.IsNew then ctx.Db.AddEntry else ctx.Db.UpdateEntry)
            let! _   = ctx.Db.SaveChangesAsync ()
            let  s   = Views.I18N.localizer.Force ()
            let  act = s[if model.IsNew then "Added" else "Updated"].Value.ToLower ()
            addInfo ctx s["Successfully {0} church “{1}”", act, model.Name]
            return! redirectTo false "/churches" next ctx
        | None -> return! fourOhFour ctx
    | Result.Error e -> return! bindError e next ctx
}
