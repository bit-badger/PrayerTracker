module PrayerTracker.Handlers.SmallGroup

open System
open Giraffe
open PrayerTracker
open PrayerTracker.Data
open PrayerTracker.Entities
open PrayerTracker.ViewModels

/// GET /small-group/announcement
let announcement : HttpHandler = requireAccess [ User ] >=> fun next ctx ->
    { viewInfo ctx with HelpLink = Some Help.sendAnnouncement }
    |> Views.SmallGroup.announcement ctx.Session.CurrentUser.Value.IsAdmin ctx
    |> renderHtml next ctx

/// POST /small-group/[group-id]/delete
let delete grpId : HttpHandler = requireAccess [ Admin ] >=> validateCsrf >=> fun next ctx -> task {
    let  s       = Views.I18N.localizer.Force ()
    let  groupId = SmallGroupId grpId
    let! conn    = ctx.Conn
    match! SmallGroups.tryById groupId conn with
    | Some grp ->
        let! reqs  = PrayerRequests.countByGroup groupId conn
        let! users = Users.countByGroup          groupId conn
        do! SmallGroups.deleteById groupId conn
        addInfo ctx
            s["The group {0} and its {1} prayer request(s) were deleted successfully; revoked access from {2} user(s)",
              grp.Name, reqs, users]
        return! redirectTo false "/small-groups" next ctx
    | None -> return! fourOhFour ctx
}

/// POST /small-group/member/[member-id]/delete
let deleteMember mbrId : HttpHandler = requireAccess [ User ] >=> validateCsrf >=> fun next ctx -> task {
    let  s        = Views.I18N.localizer.Force ()
    let  group    = ctx.Session.CurrentGroup.Value
    let  memberId = MemberId mbrId
    let! conn     = ctx.Conn
    match! Members.tryById memberId conn with
    | Some mbr when mbr.SmallGroupId = group.Id ->
        do! Members.deleteById memberId conn
        addHtmlInfo ctx s["The group member &ldquo;{0}&rdquo; was deleted successfully", mbr.Name]
        return! redirectTo false "/small-group/members" next ctx
    | Some _
    | None -> return! fourOhFour ctx
}

/// GET /small-group/[group-id]/edit
let edit grpId : HttpHandler = requireAccess [ Admin ] >=> fun next ctx -> task {
    let! conn     = ctx.Conn
    let! churches = Churches.all conn
    let  groupId  = SmallGroupId grpId
    if groupId.Value = Guid.Empty then
        return!
            viewInfo ctx
            |> Views.SmallGroup.edit EditSmallGroup.empty churches ctx
            |> renderHtml next ctx
    else
        match! SmallGroups.tryById groupId conn with
        | Some grp ->
            return!
                viewInfo ctx
                |> Views.SmallGroup.edit (EditSmallGroup.fromGroup grp) churches ctx
                |> renderHtml next ctx
        | None -> return! fourOhFour ctx
}

/// GET /small-group/member/[member-id]/edit
let editMember mbrId : HttpHandler = requireAccess [ User ] >=> fun next ctx -> task {
    let s        = Views.I18N.localizer.Force ()
    let group    = ctx.Session.CurrentGroup.Value
    let types    = ReferenceList.emailTypeList group.Preferences.DefaultEmailType s
    let memberId = MemberId mbrId
    if memberId.Value = Guid.Empty then
        return!
            viewInfo ctx
            |> Views.SmallGroup.editMember EditMember.empty types ctx
            |> renderHtml next ctx
    else
        let! conn = ctx.Conn
        match! Members.tryById memberId conn with
        | Some mbr when mbr.SmallGroupId = group.Id ->
            return!
                viewInfo ctx
                |> Views.SmallGroup.editMember (EditMember.fromMember mbr) types ctx
                |> renderHtml next ctx
        | Some _
        | None -> return! fourOhFour ctx
}

/// GET /small-group/log-on/[group-id?]
let logOn grpId : HttpHandler = requireAccess [ AccessLevel.Public ] >=> fun next ctx -> task {
    let! conn    = ctx.Conn
    let! groups  = SmallGroups.listProtected conn
    let  groupId = match grpId with Some gid -> shortGuid gid | None -> ""
    return!
        { viewInfo ctx with HelpLink = Some Help.logOn }
        |> Views.SmallGroup.logOn groups groupId ctx
        |> renderHtml next ctx
}

open System.Security.Claims
open Microsoft.AspNetCore.Authentication
open Microsoft.AspNetCore.Authentication.Cookies

/// POST /small-group/log-on/submit
let logOnSubmit : HttpHandler = requireAccess [ AccessLevel.Public ] >=> validateCsrf >=> fun next ctx -> task {
    match! ctx.TryBindFormAsync<GroupLogOn> () with
    | Ok model ->
        let s = Views.I18N.localizer.Force ()
        match! ctx.Db.TryGroupLogOnByPassword (idFromShort SmallGroupId model.SmallGroupId) model.Password with
        | Some group ->
            ctx.Session.CurrentGroup <- Some group
            let identity = ClaimsIdentity (
                Seq.singleton (Claim (ClaimTypes.GroupSid, shortGuid group.Id.Value)),
                CookieAuthenticationDefaults.AuthenticationScheme)
            do! ctx.SignInAsync (
                    identity.AuthenticationType, ClaimsPrincipal identity,
                    AuthenticationProperties (
                        IssuedUtc    = DateTimeOffset.UtcNow,
                        IsPersistent = defaultArg model.RememberMe false))
            addInfo ctx s["Log On Successful • Welcome to {0}", s["PrayerTracker"]]
            return! redirectTo false "/prayer-requests/view" next ctx
        | None ->
            addError ctx s["Password incorrect - login unsuccessful"]
            return! redirectTo false $"/small-group/log-on/{model.SmallGroupId}" next ctx
    | Result.Error e -> return! bindError e next ctx
}

/// GET /small-groups
let maintain : HttpHandler = requireAccess [ Admin ] >=> fun next ctx -> task {
    let! groups = ctx.Db.AllGroups ()
    return!
        viewInfo ctx
        |> Views.SmallGroup.maintain groups ctx
        |> renderHtml next ctx
}

/// GET /small-group/members
let members : HttpHandler = requireAccess [ User ] >=> fun next ctx -> task {
    let  group   = ctx.Session.CurrentGroup.Value
    let  s       = Views.I18N.localizer.Force ()
    let! members = ctx.Db.AllMembersForSmallGroup group.Id
    let  types   = ReferenceList.emailTypeList group.Preferences.DefaultEmailType s |> Map.ofSeq
    return!
        { viewInfo ctx with HelpLink = Some Help.maintainGroupMembers }
        |> Views.SmallGroup.members members types ctx
        |> renderHtml next ctx
}

/// GET /small-group
let overview : HttpHandler = requireAccess [ User ] >=> fun next ctx -> task {
    let  group    = ctx.Session.CurrentGroup.Value
    let! conn     = ctx.Conn
    let! reqs     = ctx.Db.AllRequestsForSmallGroup  group ctx.Clock None true 0
    let! reqCount = ctx.Db.CountRequestsBySmallGroup group.Id
    let! mbrCount = ctx.Db.CountMembersForSmallGroup group.Id
    let! admins   = Users.listByGroupId group.Id conn
    let  model    =
        {   TotalActiveReqs  = List.length reqs
            AllReqs          = reqCount
            TotalMembers     = mbrCount
            ActiveReqsByType = (
               reqs
               |> Seq.ofList
               |> Seq.map (fun req -> req.RequestType)
               |> Seq.distinct
               |> Seq.map (fun reqType -> reqType, reqs |> List.filter (fun r -> r.RequestType = reqType) |> List.length)
               |> Map.ofSeq)
            Admins            = admins
        }
    return!
        viewInfo ctx
        |> Views.SmallGroup.overview model
        |> renderHtml next ctx
}

/// GET /small-group/preferences
let preferences : HttpHandler = requireAccess [ User ] >=> fun next ctx -> task {
    let  group = ctx.Session.CurrentGroup.Value
    let! tzs   = ctx.Db.AllTimeZones ()
    return!
        { viewInfo ctx with HelpLink = Some Help.groupPreferences }
        |> Views.SmallGroup.preferences (EditPreferences.fromPreferences group.Preferences) tzs ctx
        |> renderHtml next ctx
}

open System.Threading.Tasks

/// POST /small-group/save
let save : HttpHandler = requireAccess [ Admin ] >=> validateCsrf >=> fun next ctx -> task {
    match! ctx.TryBindFormAsync<EditSmallGroup> () with
    | Ok model ->
        let s = Views.I18N.localizer.Force ()
        let! group =
            if model.IsNew then Task.FromResult (Some { SmallGroup.empty with Id = (Guid.NewGuid >> SmallGroupId) () })
            else ctx.Db.TryGroupById (idFromShort SmallGroupId model.SmallGroupId)
        match group with
        | Some grp ->
            model.populateGroup grp
            |> function
            | grp when model.IsNew ->
                ctx.Db.AddEntry grp
                ctx.Db.AddEntry { grp.Preferences with SmallGroupId = grp.Id }
            | grp -> ctx.Db.UpdateEntry grp
            let! _ = ctx.Db.SaveChangesAsync ()
            let act = s[if model.IsNew then "Added" else "Updated"].Value.ToLower ()
            addHtmlInfo ctx s["Successfully {0} group “{1}”", act, model.Name]
            return! redirectTo false "/small-groups" next ctx
        | None -> return! fourOhFour ctx
    | Result.Error e -> return! bindError e next ctx
}

/// POST /small-group/member/save
let saveMember : HttpHandler = requireAccess [ User ] >=> validateCsrf >=> fun next ctx -> task {
    match! ctx.TryBindFormAsync<EditMember> () with
    | Ok model ->
        let  group = ctx.Session.CurrentGroup.Value
        let! mMbr  =
            if model.IsNew then
                Task.FromResult (Some { Member.empty with Id = (Guid.NewGuid >> MemberId) (); SmallGroupId = group.Id })
            else ctx.Db.TryMemberById (idFromShort MemberId model.MemberId)
        match mMbr with
        | Some mbr when mbr.SmallGroupId = group.Id ->
            { mbr with
                Name   = model.Name
                Email  = model.Email
                Format = match model.Format with "" | null -> None | _ -> Some (EmailFormat.fromCode model.Format)
            }
            |> if model.IsNew then ctx.Db.AddEntry else ctx.Db.UpdateEntry
            let! _ = ctx.Db.SaveChangesAsync ()
            let s = Views.I18N.localizer.Force ()
            let act = s[if model.IsNew then "Added" else "Updated"].Value.ToLower ()
            addInfo ctx s["Successfully {0} group member", act]
            return! redirectTo false "/small-group/members" next ctx
        | Some _
        | None -> return! fourOhFour ctx
    | Result.Error e -> return! bindError e next ctx
}

/// POST /small-group/preferences/save
let savePreferences : HttpHandler = requireAccess [ User ] >=> validateCsrf >=> fun next ctx -> task {
    match! ctx.TryBindFormAsync<EditPreferences> () with
    | Ok model ->
        // Since the class is stored in the session, we'll use an intermediate instance to persist it; once that works,
        // we can repopulate the session instance. That way, if the update fails, the page should still show the
        // database values, not the then out-of-sync session ones.
        let group = ctx.Session.CurrentGroup.Value
        match! ctx.Db.TryGroupById group.Id with
        | Some grp ->
            let prefs = model.PopulatePreferences grp.Preferences
            ctx.Db.UpdateEntry prefs
            let! _ = ctx.Db.SaveChangesAsync ()
            // Refresh session instance
            ctx.Session.CurrentGroup <- Some { grp with Preferences = prefs }
            let s = Views.I18N.localizer.Force ()
            addInfo ctx s["Group preferences updated successfully"]
            return! redirectTo false "/small-group/preferences" next ctx
        | None -> return! fourOhFour ctx
    | Result.Error e -> return! bindError e next ctx
}

open Giraffe.ViewEngine
open PrayerTracker.Views.CommonFunctions

/// POST /small-group/announcement/send
let sendAnnouncement : HttpHandler = requireAccess [ User ] >=> validateCsrf >=> fun next ctx -> task {
    match! ctx.TryBindFormAsync<Announcement> () with
    | Ok model ->
        let group = ctx.Session.CurrentGroup.Value
        let pref  = group.Preferences
        let usr   = ctx.Session.CurrentUser.Value
        let now   = SmallGroup.localTimeNow ctx.Clock group
        let s     = Views.I18N.localizer.Force ()
        // Reformat the text to use the class's font stylings
        let requestText = ckEditorToText model.Text
        let htmlText =
            p [ _style $"font-family:{pref.Fonts};font-size:%d{pref.TextFontSize}pt;" ] [ rawText requestText ]
            |> renderHtmlNode
        let plainText = (htmlToPlainText >> wordWrap 74) htmlText
        // Send the e-mails
        let! recipients =
            match model.SendToClass with
            | "N" when usr.IsAdmin -> ctx.Db.AllUsersAsMembers ()
            | _ -> ctx.Db.AllMembersForSmallGroup group.Id
        use! client = Email.getConnection ()
        do! Email.sendEmails
                {   Client        = client
                    Recipients    = recipients
                    Group         = group
                    Subject       = s["Announcement for {0} - {1:MMMM d, yyyy} {2}", group.Name, now.Date,
                                      (now.ToString ("h:mm tt", null)).ToLower ()].Value
                    HtmlBody      = htmlText
                    PlainTextBody = plainText
                    Strings       = s
                }
        // Add to the request list if desired
        match model.SendToClass, model.AddToRequestList with
        | "N", _
        | _, None  -> ()
        | _, Some x when not x -> ()
        | _, _ ->
            let zone = SmallGroup.timeZone group
            { PrayerRequest.empty with
                Id           = (Guid.NewGuid >> PrayerRequestId) ()
                SmallGroupId = group.Id
                UserId       = usr.Id
                RequestType  = (Option.get >> PrayerRequestType.fromCode) model.RequestType
                Text         = requestText
                EnteredDate  = now.Date.AtStartOfDayInZone(zone).ToInstant()
                UpdatedDate  = now.InZoneLeniently(zone).ToInstant()
            }
            |> ctx.Db.AddEntry
            let! _ = ctx.Db.SaveChangesAsync ()
            ()
        // Tell 'em what they've won, Johnny!
        let toWhom =
            match model.SendToClass with
            | "N" -> s["{0} users", s["PrayerTracker"]].Value
            | _ -> s["Group Members"].Value.ToLower ()
        let andAdded = match model.AddToRequestList with Some x when x -> "and added it to the request list" | _ -> ""
        addInfo ctx s["Successfully sent announcement to all {0} {1}", toWhom, s[andAdded]]
        return!
            viewInfo ctx
            |> Views.SmallGroup.announcementSent { model with Text = htmlText }
            |> renderHtml next ctx
    | Result.Error e -> return! bindError e next ctx
}
