module PrayerTracker.Handlers.User

open Giraffe
open Microsoft.AspNetCore.Http
open PrayerTracker
open PrayerTracker.Cookies
open PrayerTracker.Entities
open PrayerTracker.ViewModels

/// Set the user's "remember me" cookie
let private setUserCookie (ctx : HttpContext) pwHash =
    ctx.Response.Cookies.Append (
        Key.Cookie.user,
        { Id = (currentUser ctx).Id.Value; GroupId = (currentGroup ctx).Id.Value; PasswordHash = pwHash }.toPayload (),
        autoRefresh)

open System
open System.Collections.Generic

/// Retrieve a user from the database by password
// If the hashes do not match, determine if it matches a previous scheme, and upgrade them if it does
let private findUserByPassword model (db : AppDbContext) = task {
    match! db.TryUserByEmailAndGroup model.Email (idFromShort SmallGroupId model.SmallGroupId) with
    | Some u when Option.isSome u.Salt ->
        // Already upgraded; match = success
        let pwHash = pbkdf2Hash (Option.get u.Salt) model.Password
        if u.PasswordHash = pwHash then
            return Some { u with PasswordHash = ""; Salt = None; SmallGroups = List<UserSmallGroup>() }, pwHash
        else return None, ""
    | Some u when u.PasswordHash = sha1Hash model.Password ->
        // Not upgraded, but password is good; upgrade 'em!
        // Upgrade 'em!
        let salt     = Guid.NewGuid ()
        let pwHash   = pbkdf2Hash salt model.Password
        let upgraded = { u with Salt = Some salt; PasswordHash = pwHash }
        db.UpdateEntry upgraded
        let! _ = db.SaveChangesAsync ()
        return Some { u with PasswordHash = ""; Salt = None; SmallGroups = List<UserSmallGroup>() }, pwHash
    | _ -> return None, ""
}

open System.Threading.Tasks

/// POST /user/password/change
let changePassword : HttpHandler = requireAccess [ User ] >=> validateCsrf >=> fun next ctx -> task {
    match! ctx.TryBindFormAsync<ChangePassword> () with
    | Ok m ->
        let  s      = Views.I18N.localizer.Force ()
        let  curUsr = currentUser ctx
        let! dbUsr  = ctx.db.TryUserById curUsr.Id
        let! user   =
            match dbUsr with
            | Some usr ->
                // Check the old password against a possibly non-salted hash
                (match usr.Salt with Some salt -> pbkdf2Hash salt | None -> sha1Hash) m.OldPassword
                |> ctx.db.TryUserLogOnByCookie curUsr.Id (currentGroup ctx).Id
            | _ -> Task.FromResult None
        match user with
        | Some _ when m.NewPassword = m.NewPasswordConfirm ->
            match dbUsr with
            | Some usr ->
                // Generate new salt whenever the password is changed
                let salt = Guid.NewGuid ()
                ctx.db.UpdateEntry { usr with PasswordHash = pbkdf2Hash salt m.NewPassword; Salt = Some salt }
                let! _ = ctx.db.SaveChangesAsync ()
                // If the user is remembered, update the cookie with the new hash
                if ctx.Request.Cookies.Keys.Contains Key.Cookie.user then setUserCookie ctx usr.PasswordHash
                addInfo ctx s["Your password was changed successfully"]
            | None -> addError ctx s["Unable to change password"]
            return! redirectTo false "/" next ctx
        | Some _ ->
            addError ctx s["The new passwords did not match - your password was NOT changed"]
            return! redirectTo false "/user/password" next ctx
        | None ->
            addError ctx s["The old password was incorrect - your password was NOT changed"]
            return! redirectTo false "/user/password" next ctx
    | Result.Error e -> return! bindError e next ctx
}

/// POST /user/[user-id]/delete
let delete usrId : HttpHandler = requireAccess [ Admin ] >=> validateCsrf >=> fun next ctx -> task {
    let userId = UserId usrId
    match! ctx.db.TryUserById userId with
    | Some user ->
        ctx.db.RemoveEntry user
        let! _ = ctx.db.SaveChangesAsync ()
        let  s = Views.I18N.localizer.Force ()
        addInfo ctx s["Successfully deleted user {0}", user.Name]
        return! redirectTo false "/users" next ctx
    | _ -> return! fourOhFour next ctx
}

open System.Net
open Microsoft.AspNetCore.Html

/// POST /user/log-on
let doLogOn : HttpHandler = requireAccess [ AccessLevel.Public ] >=> validateCsrf >=> fun next ctx -> task {
    match! ctx.TryBindFormAsync<UserLogOn> () with
    | Ok model -> 
        let  s           = Views.I18N.localizer.Force ()
        let! usr, pwHash = findUserByPassword model ctx.db
        let! grp         = ctx.db.TryGroupById (idFromShort SmallGroupId model.SmallGroupId)
        let  nextUrl     =
            match usr with
            | Some _ ->
                ctx.Session.user       <- usr
                ctx.Session.smallGroup <- grp
                if defaultArg model.RememberMe false then setUserCookie ctx pwHash
                addHtmlInfo ctx s["Log On Successful • Welcome to {0}", s["PrayerTracker"]]
                match model.RedirectUrl with
                | None -> "/small-group"
                // TODO: ensure "x" is a local URL
                | Some x when x = "" -> "/small-group"
                | Some x -> x
            | _ ->
                let grpName = match grp with Some g -> g.Name | _ -> "N/A"
                { UserMessage.error with
                    Text        = htmlLocString s["Invalid credentials - log on unsuccessful"]
                    Description =
                        [ s["This is likely due to one of the following reasons"].Value
                          ":<ul><li>"
                          s["The e-mail address “{0}” is invalid.", WebUtility.HtmlEncode model.Email].Value
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
                "/user/log-on"
        return! redirectTo false nextUrl next ctx
    | Result.Error e -> return! bindError e next ctx
}


/// GET /user/[user-id]/edit
let edit usrId : HttpHandler = requireAccess [ Admin ] >=> fun next ctx -> task {
    let startTicks = DateTime.Now.Ticks
    let userId = UserId usrId
    if userId.Value = Guid.Empty then
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
        { viewInfo ctx startTicks with HelpLink = Some Help.logOn }
        |> Views.User.logOn { UserLogOn.empty with RedirectUrl = url } groups ctx
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
    { viewInfo ctx DateTime.Now.Ticks with HelpLink = Some Help.changePassword }
    |> Views.User.changePassword ctx
    |> renderHtml next ctx


/// POST /user/save
let save : HttpHandler = requireAccess [ Admin ] >=> validateCsrf >=> fun next ctx -> task {
    match! ctx.TryBindFormAsync<EditUser> () with
    | Ok m ->
        let! user =
            if m.IsNew then Task.FromResult (Some { User.empty with Id = (Guid.NewGuid >> UserId) () })
            else ctx.db.TryUserById (idFromShort UserId m.UserId)
        let saltedUser = 
            match user with
            | Some u ->
                match u.Salt with
                | None when m.Password <> "" ->
                    // Generate salt so that a new password hash can be generated
                    Some { u with Salt = Some (Guid.NewGuid ()) }
                | _ ->
                    // Leave the user with no salt, so prior hash can be validated/upgraded
                    user
            | _ -> user
        match saltedUser with
        | Some u ->
            let updatedUser = m.PopulateUser u (pbkdf2Hash (Option.get u.Salt))
            updatedUser |> if m.IsNew then ctx.db.AddEntry else ctx.db.UpdateEntry
            let! _ = ctx.db.SaveChangesAsync ()
            let  s = Views.I18N.localizer.Force ()
            if m.IsNew then
                let h = CommonFunctions.htmlString
                { UserMessage.info with
                    Text        = h s["Successfully {0} user", s["Added"].Value.ToLower ()]
                    Description = 
                      h s["Please select at least one group for which this user ({0}) is authorized", updatedUser.Name]
                      |> Some
                }
                |> addUserMessage ctx
                return! redirectTo false $"/user/{shortGuid u.Id.Value}/small-groups" next ctx
            else
                addInfo ctx s["Successfully {0} user", s["Updated"].Value.ToLower ()]
                return! redirectTo false "/users" next ctx
        | None -> return! fourOhFour next ctx
    | Result.Error e -> return! bindError e next ctx
}


/// POST /user/small-groups/save
let saveGroups : HttpHandler = requireAccess [ Admin ] >=> validateCsrf >=> fun next ctx -> task {
    match! ctx.TryBindFormAsync<AssignGroups> () with
    | Ok model ->
        let s = Views.I18N.localizer.Force ()
        match Seq.length model.SmallGroups with
        | 0 ->
            addError ctx s["You must select at least one group to assign"]
            return! redirectTo false $"/user/{model.UserId}/small-groups" next ctx
        | _ ->
            match! ctx.db.TryUserByIdWithGroups (idFromShort UserId model.UserId) with
            | Some user ->
                let groups =
                    model.SmallGroups.Split ','
                    |> Array.map (idFromShort SmallGroupId)
                    |> List.ofArray
                user.SmallGroups
                |> Seq.filter (fun x -> not (groups |> List.exists (fun y -> y = x.SmallGroupId)))
                |> ctx.db.UserGroupXref.RemoveRange
                groups
                |> Seq.ofList
                |> Seq.filter (fun x -> not (user.SmallGroups |> Seq.exists (fun y -> y.SmallGroupId = x)))
                |> Seq.map (fun x -> { UserSmallGroup.empty with UserId = user.Id; SmallGroupId = x })
                |> List.ofSeq
                |> List.iter ctx.db.AddEntry
                let! _ = ctx.db.SaveChangesAsync ()
                addInfo ctx s["Successfully updated group permissions for {0}", model.UserName]
                return! redirectTo false "/users" next ctx
              | _ -> return! fourOhFour next ctx
    | Result.Error e -> return! bindError e next ctx
}


/// GET /user/[user-id]/small-groups
let smallGroups usrId : HttpHandler = requireAccess [ Admin ] >=> fun next ctx -> task {
    let startTicks = DateTime.Now.Ticks
    let userId     = UserId usrId
    match! ctx.db.TryUserByIdWithGroups userId with
    | Some user ->
        let! groups    = ctx.db.GroupList ()
        let  curGroups = user.SmallGroups |> Seq.map (fun g -> shortGuid g.SmallGroupId.Value) |> List.ofSeq
        return! 
            viewInfo ctx startTicks
            |> Views.User.assignGroups (AssignGroups.fromUser user) groups curGroups ctx
            |> renderHtml next ctx
    | None -> return! fourOhFour next ctx
}
