module PrayerTracker.Handlers.User

open System
open Giraffe
open Microsoft.AspNetCore.Http
open Microsoft.AspNetCore.Identity
open PrayerTracker
open PrayerTracker.Data
open PrayerTracker.Entities
open PrayerTracker.ViewModels

/// Password hashing implementation extending ASP.NET Core's identity implementation
[<AutoOpen>]
module Hashing =
    
    open System.Security.Cryptography
    open System.Text
    
    /// Custom password hasher used to verify and upgrade old password hashes
    type PrayerTrackerPasswordHasher () =
        inherit PasswordHasher<User> ()
        
        override this.VerifyHashedPassword (user, hashedPassword, providedPassword) =
            if isNull hashedPassword   then nullArg (nameof hashedPassword)
            if isNull providedPassword then nullArg (nameof providedPassword)
            
            let hashBytes = Convert.FromBase64String hashedPassword
            
            match hashBytes[0] with
            | 255uy ->
                // v2 hashes - PBKDF2 (RFC 2898), 1,024 rounds
                if hashBytes.Length < 49 then PasswordVerificationResult.Failed
                else
                    let v2Hash =
                        use alg = new Rfc2898DeriveBytes (
                            providedPassword, Encoding.UTF8.GetBytes ((Guid hashBytes[1..16]).ToString "N"), 1024)
                        (alg.GetBytes >> Convert.ToBase64String) 64
                    if Encoding.UTF8.GetString hashBytes[17..] = v2Hash then
                        PasswordVerificationResult.SuccessRehashNeeded
                    else PasswordVerificationResult.Failed
            | 254uy ->
                // v1 hashes - SHA-1
                let v1Hash =
                    use alg = SHA1.Create ()
                    alg.ComputeHash (Encoding.ASCII.GetBytes providedPassword)
                    |> Seq.map (fun byt -> byt.ToString "x2")
                    |> String.concat ""
                if Encoding.UTF8.GetString hashBytes[1..] = v1Hash then
                    PasswordVerificationResult.SuccessRehashNeeded
                else
                    PasswordVerificationResult.Failed
            | _ -> base.VerifyHashedPassword (user, hashedPassword, providedPassword)

    
/// Retrieve a user from the database by password, upgrading password hashes if required
let private findUserByPassword model conn = task {
    match! Users.tryByEmailAndGroup model.Email (idFromShort SmallGroupId model.SmallGroupId) conn with
    | Some user ->
        let hasher = PrayerTrackerPasswordHasher ()
        match hasher.VerifyHashedPassword (user, user.PasswordHash, model.Password) with
        | PasswordVerificationResult.Success -> return Some user
        | PasswordVerificationResult.SuccessRehashNeeded ->
            let upgraded = { user with PasswordHash = hasher.HashPassword (user, model.Password) }
            do! Users.updatePassword upgraded conn
            return Some upgraded
        | _ -> return None
    | None -> return None
}

/// Return a default URL if the given URL is non-local or otherwise questionable
let sanitizeUrl providedUrl defaultUrl =
    let url = match defaultArg providedUrl "" with "" -> defaultUrl | it -> it
    if url.IndexOf "\\" >= 0 || url.IndexOf "//" >= 0 then defaultUrl
    elif Seq.exists Char.IsControl url then defaultUrl
    else url

/// POST /user/password/change
let changePassword : HttpHandler = requireAccess [ User ] >=> validateCsrf >=> fun next ctx -> task {
    match! ctx.TryBindFormAsync<ChangePassword> () with
    | Ok model ->
        let  s      = Views.I18N.localizer.Force ()
        let  curUsr = ctx.Session.CurrentUser.Value
        let  hasher = PrayerTrackerPasswordHasher ()
        let! conn   = ctx.Conn
        let! user   = task {
            match! Users.tryById curUsr.Id conn with
            | Some usr ->
                if hasher.VerifyHashedPassword (usr, usr.PasswordHash, model.OldPassword)
                       = PasswordVerificationResult.Success then
                    return Some usr
                else return None
            | _ -> return None
        }
        match user with
        | Some usr when model.NewPassword = model.NewPasswordConfirm ->
            do! Users.updatePassword { usr with PasswordHash = hasher.HashPassword (usr, model.NewPassword) } conn
            addInfo ctx s["Your password was changed successfully"]
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
    let  userId = UserId usrId
    let! conn   = ctx.Conn
    match! Users.tryById userId conn with
    | Some user ->
        do! Users.deleteById userId conn
        let s = Views.I18N.localizer.Force ()
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
        let  s    = Views.I18N.localizer.Force ()
        let! conn = ctx.Conn
        match! findUserByPassword model conn with
        | Some user ->
            match! SmallGroups.tryByIdWithPreferences (idFromShort SmallGroupId model.SmallGroupId) conn with
            | Some group ->
                ctx.Session.CurrentUser  <- Some user
                ctx.Session.CurrentGroup <- Some group
                let identity = ClaimsIdentity (
                    seq {
                        Claim (ClaimTypes.NameIdentifier, shortGuid user.Id.Value)
                        Claim (ClaimTypes.GroupSid, shortGuid group.Id.Value)
                    }, CookieAuthenticationDefaults.AuthenticationScheme)
                do! ctx.SignInAsync (
                        identity.AuthenticationType, ClaimsPrincipal identity,
                        AuthenticationProperties (
                            IssuedUtc    = DateTimeOffset.UtcNow,
                            IsPersistent = defaultArg model.RememberMe false))
                do! Users.updateLastSeen user.Id ctx.Now conn
                addHtmlInfo ctx s["Log On Successful • Welcome to {0}", s["PrayerTracker"]]
                return! redirectTo false (sanitizeUrl model.RedirectUrl "/small-group") next ctx
            | None -> return! fourOhFour ctx
        | None ->
            { UserMessage.error with
                Text        = htmlLocString s["Invalid credentials - log on unsuccessful"]
                Description =
                    let detail =
                        [   "This is likely due to one of the following reasons:<ul>"
                            "<li>The e-mail address “{0}” is invalid.</li>"
                            "<li>The password entered does not match the password for the given e-mail address.</li>"
                            "<li>You are not authorized to administer the selected group.</li></ul>"
                        ]
                        |> String.concat ""
                    Some (HtmlString (s[detail, WebUtility.HtmlEncode model.Email].Value))
            }
            |> addUserMessage ctx
            return! redirectTo false "/user/log-on" next ctx
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
        let! conn = ctx.Conn
        match! Users.tryById userId conn with
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
    let! conn   = ctx.Conn
    let! groups = SmallGroups.listAll conn
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
    let! conn  = ctx.Conn
    let! users = Users.all conn
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

open System.Threading.Tasks

/// POST /user/save
let save : HttpHandler = requireAccess [ Admin ] >=> validateCsrf >=> fun next ctx -> task {
    match! ctx.TryBindFormAsync<EditUser> () with
    | Ok model ->
        let! conn = ctx.Conn
        let! user =
            if model.IsNew then Task.FromResult (Some { User.empty with Id = (Guid.NewGuid >> UserId) () })
            else Users.tryById (idFromShort UserId model.UserId) conn
        match user with
        | Some usr ->
            let hasher      = PrayerTrackerPasswordHasher ()
            let updatedUser = model.PopulateUser usr (fun pw -> hasher.HashPassword (usr, pw))
            do! Users.save updatedUser conn
            let s = Views.I18N.localizer.Force ()
            if model.IsNew then
                let h = CommonFunctions.htmlString
                { UserMessage.info with
                    Text        = h s["Successfully {0} user", s["Added"].Value.ToLower ()]
                    Description = 
                      h s["Please select at least one group for which this user ({0}) is authorized", updatedUser.Name]
                      |> Some
                }
                |> addUserMessage ctx
                return! redirectTo false $"/user/{shortGuid usr.Id.Value}/small-groups" next ctx
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
            let! conn = ctx.Conn
            do! Users.updateSmallGroups (idFromShort UserId model.UserId)
                    (model.SmallGroups.Split ',' |> Array.map (idFromShort SmallGroupId) |> List.ofArray) conn
            addInfo ctx s["Successfully updated group permissions for {0}", model.UserName]
            return! redirectTo false "/users" next ctx
    | Result.Error e -> return! bindError e next ctx
}

/// GET /user/[user-id]/small-groups
let smallGroups usrId : HttpHandler = requireAccess [ Admin ] >=> fun next ctx -> task {
    let! conn   = ctx.Conn
    let  userId = UserId usrId
    match! Users.tryById userId conn with
    | Some user ->
        let! groups    = SmallGroups.listAll conn
        let! groupIds  = Users.groupIdsByUserId userId conn
        let  curGroups = groupIds |> List.map (fun g -> shortGuid g.Value)
        return! 
            viewInfo ctx
            |> Views.User.assignGroups (AssignGroups.fromUser user) groups curGroups ctx
            |> renderHtml next ctx
    | None -> return! fourOhFour ctx
}
