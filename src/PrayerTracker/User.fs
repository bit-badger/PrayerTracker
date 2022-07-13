module PrayerTracker.Handlers.User

open System
open System.Collections.Generic
open System.Net
open System.Threading.Tasks
open Giraffe
open Microsoft.AspNetCore.Html
open Microsoft.AspNetCore.Http
open PrayerTracker
open PrayerTracker.Cookies
open PrayerTracker.Entities
open PrayerTracker.ViewModels
open PrayerTracker.Views.CommonFunctions

/// Set the user's "remember me" cookie
let private setUserCookie (ctx : HttpContext) pwHash =
    ctx.Response.Cookies.Append (
        Key.Cookie.user,
        { Id = (currentUser ctx).userId; GroupId = (currentGroup ctx).smallGroupId; PasswordHash = pwHash }.toPayload (),
        autoRefresh)

/// Retrieve a user from the database by password
// If the hashes do not match, determine if it matches a previous scheme, and upgrade them if it does
let private findUserByPassword m (db : AppDbContext) = task {
    match! db.TryUserByEmailAndGroup m.emailAddress m.smallGroupId with
    | Some u when Option.isSome u.salt ->
        // Already upgraded; match = success
        let pwHash = pbkdf2Hash (Option.get u.salt) m.password
        if u.passwordHash = pwHash then
            return Some { u with passwordHash = ""; salt = None; smallGroups = List<UserSmallGroup>() }, pwHash
        else return None, ""
    | Some u when u.passwordHash = sha1Hash m.password ->
        // Not upgraded, but password is good; upgrade 'em!
        // Upgrade 'em!
        let salt     = Guid.NewGuid ()
        let pwHash   = pbkdf2Hash salt m.password
        let upgraded = { u with salt = Some salt; passwordHash = pwHash }
        db.UpdateEntry upgraded
        let! _ = db.SaveChangesAsync ()
        return Some { u with passwordHash = ""; salt = None; smallGroups = List<UserSmallGroup>() }, pwHash
    | _ -> return None, ""
}


/// POST /user/password/change
let changePassword : HttpHandler = requireAccess [ User ] >=> validateCSRF >=> fun next ctx -> task {
    match! ctx.TryBindFormAsync<ChangePassword> () with
    | Ok m ->
        let  s      = Views.I18N.localizer.Force ()
        let  curUsr = currentUser ctx
        let! dbUsr  = ctx.db.TryUserById curUsr.userId
        let! user   =
            match dbUsr with
            | Some usr ->
                // Check the old password against a possibly non-salted hash
                (match usr.salt with Some salt -> pbkdf2Hash salt | None -> sha1Hash) m.oldPassword
                |> ctx.db.TryUserLogOnByCookie curUsr.userId (currentGroup ctx).smallGroupId
            | _ -> Task.FromResult None
        match user with
        | Some _ when m.newPassword = m.newPasswordConfirm ->
            match dbUsr with
            | Some usr ->
                // Generate new salt whenever the password is changed
                let salt = Guid.NewGuid ()
                ctx.db.UpdateEntry { usr with passwordHash = pbkdf2Hash salt m.newPassword; salt = Some salt }
                let! _ = ctx.db.SaveChangesAsync ()
                // If the user is remembered, update the cookie with the new hash
                if ctx.Request.Cookies.Keys.Contains Key.Cookie.user then setUserCookie ctx usr.passwordHash
                addInfo ctx s["Your password was changed successfully"]
            | None -> addError ctx s["Unable to change password"]
            return! redirectTo false "/web/" next ctx
        | Some _ ->
            addError ctx s["The new passwords did not match - your password was NOT changed"]
            return! redirectTo false "/web/user/password" next ctx
        | None ->
            addError ctx s["The old password was incorrect - your password was NOT changed"]
            return! redirectTo false "/web/user/password" next ctx
    | Error e -> return! bindError e next ctx
}


/// POST /user/[user-id]/delete
let delete userId : HttpHandler = requireAccess [ Admin ] >=> validateCSRF >=> fun next ctx -> task {
    match! ctx.db.TryUserById userId with
    | Some user ->
        ctx.db.RemoveEntry user
        let! _ = ctx.db.SaveChangesAsync ()
        let  s = Views.I18N.localizer.Force ()
        addInfo ctx s["Successfully deleted user {0}", user.fullName]
        return! redirectTo false "/web/users" next ctx
    | _ -> return! fourOhFour next ctx
}


/// POST /user/log-on
let doLogOn : HttpHandler = requireAccess [ AccessLevel.Public ] >=> validateCSRF >=> fun next ctx -> task {
    match! ctx.TryBindFormAsync<UserLogOn> () with
    | Ok m -> 
        let  s           = Views.I18N.localizer.Force ()
        let! usr, pwHash = findUserByPassword m ctx.db
        let! grp         = ctx.db.TryGroupById m.smallGroupId
        let  nextUrl     =
            match usr with
            | Some _ ->
                ctx.Session.user       <- usr
                ctx.Session.smallGroup <- grp
                match m.rememberMe with Some x when x -> setUserCookie ctx pwHash | _ -> ()
                addHtmlInfo ctx s["Log On Successful • Welcome to {0}", s["PrayerTracker"]]
                match m.redirectUrl with
                | None -> "/web/small-group"
                | Some x when x = "" -> "/web/small-group"
                | Some x -> x
            | _ ->
                let grpName = match grp with Some g -> g.name | _ -> "N/A"
                { UserMessage.error with
                    text        = htmlLocString s["Invalid credentials - log on unsuccessful"]
                    description =
                        [ s["This is likely due to one of the following reasons"].Value
                          ":<ul><li>"
                          s["The e-mail address “{0}” is invalid.", WebUtility.HtmlEncode m.emailAddress].Value
                          "</li><li>"
                          s["The password entered does not match the password for the given e-mail address."].Value
                          "</li><li>"
                          s["You are not authorized to administer the group “{0}”.",
                            WebUtility.HtmlEncode grpName].Value
                          "</li></ul>"
                        ]
                        |> String.concat ""
                        |> (HtmlString >> Some)
                }
                |> addUserMessage ctx
                "/web/user/log-on"
        return! redirectTo false nextUrl next ctx
    | Error e -> return! bindError e next ctx
}


/// GET /user/[user-id]/edit
let edit (userId : UserId) : HttpHandler = requireAccess [ Admin ] >=> fun next ctx -> task {
    let startTicks = DateTime.Now.Ticks
    if userId = Guid.Empty then
        return!
            viewInfo ctx startTicks
            |> Views.User.edit EditUser.empty ctx
            |> renderHtml next ctx
    else
        match! ctx.db.TryUserById userId with
        | Some user ->
            return!
                viewInfo ctx startTicks
                |> Views.User.edit (EditUser.fromUser user) ctx
                |> renderHtml next ctx
        | _ -> return! fourOhFour next ctx
}


/// GET /user/log-on
let logOn : HttpHandler = requireAccess [ AccessLevel.Public ] >=> fun next ctx -> task {
    let  startTicks = DateTime.Now.Ticks
    let  s          = Views.I18N.localizer.Force ()
    let! groups     = ctx.db.GroupList ()
    let  url        = Option.ofObj <| ctx.Session.GetString Key.Session.redirectUrl
    match url with
    | Some _ ->
        ctx.Session.Remove Key.Session.redirectUrl
        addWarning ctx s["The page you requested requires authentication; please log on below."]
    | None -> ()
    return!
        { viewInfo ctx startTicks with helpLink = Some Help.logOn }
        |> Views.User.logOn { UserLogOn.empty with redirectUrl = url } groups ctx
        |> renderHtml next ctx
}


/// GET /users
let maintain : HttpHandler = requireAccess [ Admin ] >=> fun next ctx -> task {
    let  startTicks = DateTime.Now.Ticks
    let! users      = ctx.db.AllUsers ()
    return!
        viewInfo ctx startTicks
        |> Views.User.maintain users ctx
        |> renderHtml next ctx
}


/// GET /user/password
let password : HttpHandler = requireAccess [ User ] >=> fun next ctx ->
    { viewInfo ctx DateTime.Now.Ticks with helpLink = Some Help.changePassword }
    |> Views.User.changePassword ctx
    |> renderHtml next ctx


/// POST /user/save
let save : HttpHandler = requireAccess [ Admin ] >=> validateCSRF >=> fun next ctx -> task {
    match! ctx.TryBindFormAsync<EditUser> () with
    | Ok m ->
        let! user =
            if m.isNew () then Task.FromResult (Some { User.empty with userId = Guid.NewGuid () })
            else ctx.db.TryUserById m.userId
        let saltedUser = 
            match user with
            | Some u ->
                match u.salt with
                | None when m.password <> "" ->
                    // Generate salt so that a new password hash can be generated
                    Some { u with salt = Some (Guid.NewGuid ()) }
                | _ ->
                    // Leave the user with no salt, so prior hash can be validated/upgraded
                    user
            | _ -> user
        match saltedUser with
        | Some u ->
            let updatedUser = m.populateUser u (pbkdf2Hash (Option.get u.salt))
            updatedUser |> (if m.isNew () then ctx.db.AddEntry else ctx.db.UpdateEntry)
            let! _ = ctx.db.SaveChangesAsync ()
            let  s = Views.I18N.localizer.Force ()
            if m.isNew () then
                let h = CommonFunctions.htmlString
                { UserMessage.info with
                    text = h s["Successfully {0} user", s["Added"].Value.ToLower ()]
                    description = 
                      h s["Please select at least one group for which this user ({0}) is authorized",
                          updatedUser.fullName]
                      |> Some
                }
                |> addUserMessage ctx
                return! redirectTo false $"/web/user/{flatGuid u.userId}/small-groups" next ctx
            else
                addInfo ctx s["Successfully {0} user", s["Updated"].Value.ToLower ()]
                return! redirectTo false "/web/users" next ctx
        | None -> return! fourOhFour next ctx
    | Error e -> return! bindError e next ctx
}


/// POST /user/small-groups/save
let saveGroups : HttpHandler = requireAccess [ Admin ] >=> validateCSRF >=> fun next ctx -> task {
    match! ctx.TryBindFormAsync<AssignGroups> () with
    | Ok m ->
        let s = Views.I18N.localizer.Force ()
        match Seq.length m.smallGroups with
        | 0 ->
            addError ctx s["You must select at least one group to assign"]
            return! redirectTo false $"/web/user/{flatGuid m.userId}/small-groups" next ctx
        | _ ->
            match! ctx.db.TryUserByIdWithGroups m.userId with
            | Some user ->
                let grps =
                    m.smallGroups.Split ','
                    |> Array.map Guid.Parse
                    |> List.ofArray
                user.smallGroups
                |> Seq.filter (fun x -> not (grps |> List.exists (fun y -> y = x.smallGroupId)))
                |> ctx.db.UserGroupXref.RemoveRange
                grps
                |> Seq.ofList
                |> Seq.filter (fun x -> not (user.smallGroups |> Seq.exists (fun y -> y.smallGroupId = x)))
                |> Seq.map (fun x -> { UserSmallGroup.empty with userId = user.userId; smallGroupId = x })
                |> List.ofSeq
                |> List.iter ctx.db.AddEntry
                let! _ = ctx.db.SaveChangesAsync ()
                addInfo ctx s["Successfully updated group permissions for {0}", m.userName]
                return! redirectTo false "/web/users" next ctx
              | _ -> return! fourOhFour next ctx
    | Error e -> return! bindError e next ctx
}


/// GET /user/[user-id]/small-groups
let smallGroups userId : HttpHandler = requireAccess [ Admin ] >=> fun next ctx -> task {
    let startTicks = DateTime.Now.Ticks
    match! ctx.db.TryUserByIdWithGroups userId with
    | Some user ->
        let! grps      = ctx.db.GroupList ()
        let  curGroups = user.smallGroups |> Seq.map (fun g -> flatGuid g.smallGroupId) |> List.ofSeq
        return! 
            viewInfo ctx startTicks
            |> Views.User.assignGroups (AssignGroups.fromUser user) grps curGroups ctx
            |> renderHtml next ctx
    | None -> return! fourOhFour next ctx
}
