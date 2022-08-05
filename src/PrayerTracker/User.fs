module PrayerTracker.Handlers.User

open System
open System.Collections.Generic
open Giraffe
open Microsoft.AspNetCore.Http
open PrayerTracker
open PrayerTracker.Entities
open PrayerTracker.ViewModels

/// Retrieve a user from the database by password
// If the hashes do not match, determine if it matches a previous scheme, and upgrade them if it does
let private findUserByPassword model (db : AppDbContext) = task {
    match! db.TryUserByEmailAndGroup model.Email (idFromShort SmallGroupId model.SmallGroupId) with
    | Some u when Option.isSome u.Salt ->
        // Already upgraded; match = success
        let pwHash = pbkdf2Hash (Option.get u.Salt) model.Password
        if u.PasswordHash = pwHash then
            return Some { u with PasswordHash = ""; Salt = None; SmallGroups = List<UserSmallGroup>() }
        else return None
    | Some u when u.PasswordHash = sha1Hash model.Password ->
        // Not upgraded, but password is good; upgrade 'em!
        // Upgrade 'em!
        let salt     = Guid.NewGuid ()
        let pwHash   = pbkdf2Hash salt model.Password
        let upgraded = { u with Salt = Some salt; PasswordHash = pwHash }
        db.UpdateEntry upgraded
        let! _ = db.SaveChangesAsync ()
        return Some { u with PasswordHash = ""; Salt = None; SmallGroups = List<UserSmallGroup>() }
    | _ -> return None
}

open System.Threading.Tasks

/// POST /user/password/change
let changePassword : HttpHandler = requireAccess [ User ] >=> validateCsrf >=> fun next ctx -> task {
    match! ctx.TryBindFormAsync<ChangePassword> () with
    | Ok model ->
        let  s      = Views.I18N.localizer.Force ()
        let  curUsr = ctx.Session.CurrentUser.Value
        let! dbUsr  = ctx.Db.TryUserById curUsr.Id
        let  group  = ctx.Session.CurrentGroup.Value
        let! user   =
            match dbUsr with
            | Some usr ->
                // Check the old password against a possibly non-salted hash
                (match usr.Salt with Some salt -> pbkdf2Hash salt | None -> sha1Hash) model.OldPassword
                |> ctx.Db.TryUserLogOnByCookie curUsr.Id group.Id
            | _ -> Task.FromResult None
        match user with
        | Some _ when model.NewPassword = model.NewPasswordConfirm ->
            match dbUsr with
            | Some usr ->
                // Generate new salt whenever the password is changed
                let salt = Guid.NewGuid ()
                ctx.Db.UpdateEntry { usr with PasswordHash = pbkdf2Hash salt model.NewPassword; Salt = Some salt }
                let! _ = ctx.Db.SaveChangesAsync ()
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
    match! ctx.Db.TryUserById userId with
    | Some user ->
        ctx.Db.RemoveEntry user
        let! _ = ctx.Db.SaveChangesAsync ()
        let  s = Views.I18N.localizer.Force ()
        addInfo ctx s["Successfully deleted user {0}", user.Name]
        return! redirectTo false "/users" next ctx
    | _ -> return! fourOhFour ctx
}

open System.Net
open System.Security.Claims
open Microsoft.AspNetCore.Authentication
open Microsoft.AspNetCore.Authentication.Cookies
open Microsoft.AspNetCore.Html

/// POST /user/log-on
let doLogOn : HttpHandler = requireAccess [ AccessLevel.Public ] >=> validateCsrf >=> fun next ctx -> task {
    match! ctx.TryBindFormAsync<UserLogOn> () with
    | Ok model -> 
        let  s   = Views.I18N.localizer.Force ()
        let! usr = findUserByPassword model ctx.Db
        match! ctx.Db.TryGroupById (idFromShort SmallGroupId model.SmallGroupId) with
        | Some group ->
            let! nextUrl = backgroundTask {
                match usr with
                | Some user ->
                    ctx.Session.CurrentUser  <- usr
                    ctx.Session.CurrentGroup <- Some group
                    let claims = seq {
                        Claim (ClaimTypes.NameIdentifier, shortGuid user.Id.Value)
                        Claim (ClaimTypes.GroupSid, shortGuid group.Id.Value)
                        Claim (ClaimTypes.Role, if user.IsAdmin then "Admin" else "User")
                    }
                    let identity = ClaimsIdentity (claims, CookieAuthenticationDefaults.AuthenticationScheme)
                    do! ctx.SignInAsync
                            (identity.AuthenticationType, ClaimsPrincipal identity,
                             AuthenticationProperties (
                                 IssuedUtc    = DateTimeOffset.UtcNow,
                                 IsPersistent = defaultArg model.RememberMe false))
                    addHtmlInfo ctx s["Log On Successful • Welcome to {0}", s["PrayerTracker"]]
                    return
                        match model.RedirectUrl with
                        | None -> "/small-group"
                        | Some x when x = ""-> "/small-group"
                        | Some x when x.IndexOf "://" < 0 -> x
                        | _ -> "/small-group"
                | _ ->
                    { UserMessage.error with
                        Text        = htmlLocString s["Invalid credentials - log on unsuccessful"]
                        Description =
                            [   s["This is likely due to one of the following reasons"].Value
                                ":<ul><li>"
                                s["The e-mail address “{0}” is invalid.", WebUtility.HtmlEncode model.Email].Value
                                "</li><li>"
                                s["The password entered does not match the password for the given e-mail address."].Value
                                "</li><li>"
                                s["You are not authorized to administer the group “{0}”.",
                                    WebUtility.HtmlEncode group.Name].Value
                                "</li></ul>"
                            ]
                            |> String.concat ""
                            |> (HtmlString >> Some)
                    }
                    |> addUserMessage ctx
                    return "/user/log-on"
            }
            return! redirectTo false nextUrl next ctx
        | None -> return! fourOhFour ctx
    | Result.Error e -> return! bindError e next ctx
}

/// GET /user/[user-id]/edit
let edit usrId : HttpHandler = requireAccess [ Admin ] >=> fun next ctx -> task {
    let userId = UserId usrId
    if userId.Value = Guid.Empty then
        return!
            viewInfo ctx
            |> Views.User.edit EditUser.empty ctx
            |> renderHtml next ctx
    else
        match! ctx.Db.TryUserById userId with
        | Some user ->
            return!
                viewInfo ctx
                |> Views.User.edit (EditUser.fromUser user) ctx
                |> renderHtml next ctx
        | _ -> return! fourOhFour ctx
}

/// GET /user/log-on
let logOn : HttpHandler = requireAccess [ AccessLevel.Public ] >=> fun next ctx -> task {
    let  s      = Views.I18N.localizer.Force ()
    let! groups = ctx.Db.GroupList ()
    let  url    = Option.ofObj <| ctx.Session.GetString Key.Session.redirectUrl
    match url with
    | Some _ ->
        ctx.Session.Remove Key.Session.redirectUrl
        addWarning ctx s["The page you requested requires authentication; please log on below."]
    | None -> ()
    return!
        { viewInfo ctx with HelpLink = Some Help.logOn }
        |> Views.User.logOn { UserLogOn.empty with RedirectUrl = url } groups ctx
        |> renderHtml next ctx
}

/// GET /users
let maintain : HttpHandler = requireAccess [ Admin ] >=> fun next ctx -> task {
    let! users = ctx.Db.AllUsers ()
    return!
        viewInfo ctx
        |> Views.User.maintain users ctx
        |> renderHtml next ctx
}

/// GET /user/password
let password : HttpHandler = requireAccess [ User ] >=> fun next ctx ->
    { viewInfo ctx with HelpLink = Some Help.changePassword }
    |> Views.User.changePassword ctx
    |> renderHtml next ctx

/// POST /user/save
let save : HttpHandler = requireAccess [ Admin ] >=> validateCsrf >=> fun next ctx -> task {
    match! ctx.TryBindFormAsync<EditUser> () with
    | Ok model ->
        let! user =
            if model.IsNew then Task.FromResult (Some { User.empty with Id = (Guid.NewGuid >> UserId) () })
            else ctx.Db.TryUserById (idFromShort UserId model.UserId)
        let saltedUser = 
            match user with
            | Some u ->
                match u.Salt with
                | None when model.Password <> "" ->
                    // Generate salt so that a new password hash can be generated
                    Some { u with Salt = Some (Guid.NewGuid ()) }
                | _ ->
                    // Leave the user with no salt, so prior hash can be validated/upgraded
                    user
            | _ -> user
        match saltedUser with
        | Some u ->
            let updatedUser = model.PopulateUser u (pbkdf2Hash (Option.get u.Salt))
            updatedUser |> if model.IsNew then ctx.Db.AddEntry else ctx.Db.UpdateEntry
            let! _ = ctx.Db.SaveChangesAsync ()
            let  s = Views.I18N.localizer.Force ()
            if model.IsNew then
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
        | None -> return! fourOhFour ctx
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
            match! ctx.Db.TryUserByIdWithGroups (idFromShort UserId model.UserId) with
            | Some user ->
                let groups =
                    model.SmallGroups.Split ','
                    |> Array.map (idFromShort SmallGroupId)
                    |> List.ofArray
                user.SmallGroups
                |> Seq.filter (fun x -> not (groups |> List.exists (fun y -> y = x.SmallGroupId)))
                |> ctx.Db.UserGroupXref.RemoveRange
                groups
                |> Seq.ofList
                |> Seq.filter (fun x -> not (user.SmallGroups |> Seq.exists (fun y -> y.SmallGroupId = x)))
                |> Seq.map (fun x -> { UserSmallGroup.empty with UserId = user.Id; SmallGroupId = x })
                |> List.ofSeq
                |> List.iter ctx.Db.AddEntry
                let! _ = ctx.Db.SaveChangesAsync ()
                addInfo ctx s["Successfully updated group permissions for {0}", model.UserName]
                return! redirectTo false "/users" next ctx
              | _ -> return! fourOhFour ctx
    | Result.Error e -> return! bindError e next ctx
}

/// GET /user/[user-id]/small-groups
let smallGroups usrId : HttpHandler = requireAccess [ Admin ] >=> fun next ctx -> task {
    let userId = UserId usrId
    match! ctx.Db.TryUserByIdWithGroups userId with
    | Some user ->
        let! groups    = ctx.Db.GroupList ()
        let  curGroups = user.SmallGroups |> Seq.map (fun g -> shortGuid g.SmallGroupId.Value) |> List.ofSeq
        return! 
            viewInfo ctx
            |> Views.User.assignGroups (AssignGroups.fromUser user) groups curGroups ctx
            |> renderHtml next ctx
    | None -> return! fourOhFour ctx
}
