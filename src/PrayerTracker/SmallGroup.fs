module PrayerTracker.Handlers.SmallGroup

open Giraffe
open Giraffe.ViewEngine
open Microsoft.AspNetCore.Http
open NodaTime
open PrayerTracker
open PrayerTracker.Cookies
open PrayerTracker.Entities
open PrayerTracker.ViewModels
open PrayerTracker.Views.CommonFunctions
open System
open System.Threading.Tasks

/// Set a small group "Remember Me" cookie
let private setGroupCookie (ctx : HttpContext) pwHash =
    ctx.Response.Cookies.Append
        (Key.Cookie.group, { GroupId = (currentGroup ctx).smallGroupId; PasswordHash = pwHash }.toPayload (),
         autoRefresh)


/// GET /small-group/announcement
let announcement : HttpHandler = requireAccess [ User ] >=> fun next ctx ->
    { viewInfo ctx DateTime.Now.Ticks with HelpLink = Some Help.sendAnnouncement; Script = [ "ckeditor/ckeditor" ] }
    |> Views.SmallGroup.announcement (currentUser ctx).isAdmin ctx
    |> renderHtml next ctx


/// POST /small-group/[group-id]/delete
let delete groupId : HttpHandler = requireAccess [ Admin ] >=> validateCSRF >=> fun next ctx -> task {
    let s = Views.I18N.localizer.Force ()
    match! ctx.db.TryGroupById groupId with
    | Some grp ->
        let! reqs  = ctx.db.CountRequestsBySmallGroup groupId
        let! users = ctx.db.CountUsersBySmallGroup    groupId
        ctx.db.RemoveEntry grp
        let! _ = ctx.db.SaveChangesAsync ()
        addInfo ctx
            s["The group {0} and its {1} prayer request(s) were deleted successfully; revoked access from {2} user(s)",
              grp.name, reqs, users]
        return! redirectTo false "/web/small-groups" next ctx
    | None -> return! fourOhFour next ctx
}


/// POST /small-group/member/[member-id]/delete
let deleteMember memberId : HttpHandler = requireAccess [ User ] >=> validateCSRF >=> fun next ctx -> task {
    let s  = Views.I18N.localizer.Force ()
    match! ctx.db.TryMemberById memberId with
    | Some mbr when mbr.smallGroupId = (currentGroup ctx).smallGroupId ->
        ctx.db.RemoveEntry mbr
        let! _ = ctx.db.SaveChangesAsync ()
        addHtmlInfo ctx s["The group member &ldquo;{0}&rdquo; was deleted successfully", mbr.memberName]
        return! redirectTo false "/web/small-group/members" next ctx
    | Some _
    | None -> return! fourOhFour next ctx
}


/// GET /small-group/[group-id]/edit
let edit (groupId : SmallGroupId) : HttpHandler = requireAccess [ Admin ] >=> fun next ctx -> task {
    let  startTicks = DateTime.Now.Ticks
    let! churches   = ctx.db.AllChurches ()
    if groupId = Guid.Empty then
        return!
            viewInfo ctx startTicks
            |> Views.SmallGroup.edit EditSmallGroup.empty churches ctx
            |> renderHtml next ctx
    else
        match! ctx.db.TryGroupById groupId with
        | Some grp ->
            return!
                viewInfo ctx startTicks
                |> Views.SmallGroup.edit (EditSmallGroup.fromGroup grp) churches ctx
                |> renderHtml next ctx
        | None -> return! fourOhFour next ctx
}


/// GET /small-group/member/[member-id]/edit
let editMember (memberId : MemberId) : HttpHandler = requireAccess [ User ] >=> fun next ctx -> task {
    let startTicks = DateTime.Now.Ticks
    let s          = Views.I18N.localizer.Force ()
    let grp        = currentGroup ctx
    let types      = ReferenceList.emailTypeList grp.preferences.defaultEmailType s
    if memberId = Guid.Empty then
        return!
            viewInfo ctx startTicks
            |> Views.SmallGroup.editMember EditMember.empty types ctx
            |> renderHtml next ctx
    else
        match! ctx.db.TryMemberById memberId with
        | Some mbr when mbr.smallGroupId = grp.smallGroupId ->
            return!
                viewInfo ctx startTicks
                |> Views.SmallGroup.editMember (EditMember.fromMember mbr) types ctx
                |> renderHtml next ctx
        | Some _
        | None -> return! fourOhFour next ctx
}


/// GET /small-group/log-on/[group-id?]
let logOn (groupId : SmallGroupId option) : HttpHandler = requireAccess [ AccessLevel.Public ] >=> fun next ctx -> task {
    let  startTicks = DateTime.Now.Ticks
    let! groups     = ctx.db.ProtectedGroups ()
    let  grpId      = match groupId with Some gid -> flatGuid gid | None -> ""
    return!
        { viewInfo ctx startTicks with HelpLink = Some Help.logOn }
        |> Views.SmallGroup.logOn groups grpId ctx
        |> renderHtml next ctx
}


/// POST /small-group/log-on/submit
let logOnSubmit : HttpHandler = requireAccess [ AccessLevel.Public ] >=> validateCSRF >=> fun next ctx -> task {
    match! ctx.TryBindFormAsync<GroupLogOn> () with
    | Ok m ->
        let s = Views.I18N.localizer.Force ()
        match! ctx.db.TryGroupLogOnByPassword m.SmallGroupId m.Password with
        | Some grp ->
            ctx.Session.smallGroup <- Some grp
            if defaultArg m.RememberMe false then (setGroupCookie ctx << sha1Hash) m.Password
            addInfo ctx s["Log On Successful • Welcome to {0}", s["PrayerTracker"]]
            return! redirectTo false "/web/prayer-requests/view" next ctx
        | None ->
            addError ctx s["Password incorrect - login unsuccessful"]
            return! redirectTo false $"/web/small-group/log-on/{flatGuid m.SmallGroupId}" next ctx
    | Result.Error e -> return! bindError e next ctx
}


/// GET /small-groups
let maintain : HttpHandler = requireAccess [ Admin ] >=> fun next ctx -> task {
    let startTicks = DateTime.Now.Ticks
    let! groups = ctx.db.AllGroups ()
    return!
        viewInfo ctx startTicks
        |> Views.SmallGroup.maintain groups ctx
        |> renderHtml next ctx
}


/// GET /small-group/members
let members : HttpHandler = requireAccess [ User ] >=> fun next ctx -> task {
    let startTicks = DateTime.Now.Ticks
    let grp        = currentGroup ctx
    let s          = Views.I18N.localizer.Force ()
    let! members   = ctx.db.AllMembersForSmallGroup grp.smallGroupId
    let  types     = ReferenceList.emailTypeList grp.preferences.defaultEmailType s |> Map.ofSeq
    return!
        { viewInfo ctx startTicks with HelpLink = Some Help.maintainGroupMembers }
        |> Views.SmallGroup.members members types ctx
        |> renderHtml next ctx
}


/// GET /small-group
let overview : HttpHandler = requireAccess [ User ] >=> fun next ctx -> task {
    let  startTicks = DateTime.Now.Ticks
    let  clock      = ctx.GetService<IClock> ()
    let! reqs       = ctx.db.AllRequestsForSmallGroup  (currentGroup ctx) clock None true 0
    let! reqCount   = ctx.db.CountRequestsBySmallGroup (currentGroup ctx).smallGroupId
    let! mbrCount   = ctx.db.CountMembersForSmallGroup (currentGroup ctx).smallGroupId
    let  m          =
        { TotalActiveReqs  = List.length reqs
          AllReqs          = reqCount
          TotalMembers     = mbrCount
          ActiveReqsByType =
              (reqs
               |> Seq.ofList
               |> Seq.map (fun req -> req.requestType)
               |> Seq.distinct
               |> Seq.map (fun reqType -> reqType, reqs |> List.filter (fun r -> r.requestType = reqType) |> List.length)
               |> Map.ofSeq)
          }
    return!
        viewInfo ctx startTicks
        |> Views.SmallGroup.overview m
        |> renderHtml next ctx
}


/// GET /small-group/preferences
let preferences : HttpHandler = requireAccess [ User ] >=> fun next ctx -> task {
    let  startTicks = DateTime.Now.Ticks
    let! tzs        = ctx.db.AllTimeZones ()
    return!
        { viewInfo ctx startTicks with HelpLink = Some Help.groupPreferences }
        |> Views.SmallGroup.preferences (EditPreferences.fromPreferences (currentGroup ctx).preferences) tzs ctx
        |> renderHtml next ctx
}


/// POST /small-group/save
let save : HttpHandler = requireAccess [ Admin ] >=> validateCSRF >=> fun next ctx -> task {
    match! ctx.TryBindFormAsync<EditSmallGroup> () with
    | Ok m ->
        let s = Views.I18N.localizer.Force ()
        let! group =
            if m.IsNew then Task.FromResult (Some { SmallGroup.empty with smallGroupId = Guid.NewGuid () })
            else ctx.db.TryGroupById m.SmallGroupId
        match group with
        | Some grp ->
            m.populateGroup grp
            |> function
            | grp when m.IsNew ->
                ctx.db.AddEntry grp
                ctx.db.AddEntry { grp.preferences with smallGroupId = grp.smallGroupId }
            | grp -> ctx.db.UpdateEntry grp
            let! _ = ctx.db.SaveChangesAsync ()
            let act = s[if m.IsNew then "Added" else "Updated"].Value.ToLower ()
            addHtmlInfo ctx s["Successfully {0} group “{1}”", act, m.Name]
            return! redirectTo false "/web/small-groups" next ctx
        | None -> return! fourOhFour next ctx
    | Result.Error e -> return! bindError e next ctx
}


/// POST /small-group/member/save
let saveMember : HttpHandler = requireAccess [ User ] >=> validateCSRF >=> fun next ctx -> task {
    match! ctx.TryBindFormAsync<EditMember> () with
    | Ok m ->
        let  grp  = currentGroup ctx
        let! mMbr =
            if m.IsNew then
                Task.FromResult (Some { Member.empty with memberId = Guid.NewGuid (); smallGroupId = grp.smallGroupId })
            else ctx.db.TryMemberById m.MemberId
        match mMbr with
        | Some mbr when mbr.smallGroupId = grp.smallGroupId ->
            { mbr with
                memberName = m.Name
                email      = m.Email
                format     = match m.Format with "" | null -> None | _ -> Some m.Format
            }
            |> if m.IsNew then ctx.db.AddEntry else ctx.db.UpdateEntry
            let! _ = ctx.db.SaveChangesAsync ()
            let s = Views.I18N.localizer.Force ()
            let act = s[if m.IsNew then "Added" else "Updated"].Value.ToLower ()
            addInfo ctx s["Successfully {0} group member", act]
            return! redirectTo false "/web/small-group/members" next ctx
        | Some _
        | None -> return! fourOhFour next ctx
    | Result.Error e -> return! bindError e next ctx
}


/// POST /small-group/preferences/save
let savePreferences : HttpHandler = requireAccess [ User ] >=> validateCSRF >=> fun next ctx -> task {
    match! ctx.TryBindFormAsync<EditPreferences> () with
    | Ok m ->
        // Since the class is stored in the session, we'll use an intermediate instance to persist it; once that works,
        // we can repopulate the session instance. That way, if the update fails, the page should still show the
        // database values, not the then out-of-sync session ones.
        match! ctx.db.TryGroupById (currentGroup ctx).smallGroupId with
        | Some grp ->
            let prefs = m.PopulatePreferences grp.preferences
            ctx.db.UpdateEntry prefs
            let! _ = ctx.db.SaveChangesAsync ()
            // Refresh session instance
            ctx.Session.smallGroup <- Some { grp with preferences = prefs }
            let s = Views.I18N.localizer.Force ()
            addInfo ctx s["Group preferences updated successfully"]
            return! redirectTo false "/web/small-group/preferences" next ctx
        | None -> return! fourOhFour next ctx
    | Result.Error e -> return! bindError e next ctx
}


/// POST /small-group/announcement/send
let sendAnnouncement : HttpHandler = requireAccess [ User ] >=> validateCSRF >=> fun next ctx -> task {
    let startTicks = DateTime.Now.Ticks
    match! ctx.TryBindFormAsync<Announcement> () with
    | Ok m ->
        let grp = currentGroup ctx
        let usr = currentUser ctx
        let now = grp.localTimeNow (ctx.GetService<IClock> ())
        let s   = Views.I18N.localizer.Force ()
        // Reformat the text to use the class's font stylings
        let requestText = ckEditorToText m.Text
        let htmlText =
            p [ _style $"font-family:{grp.preferences.listFonts};font-size:%d{grp.preferences.textFontSize}pt;" ]
              [ rawText requestText ]
            |> renderHtmlNode
        let plainText = (htmlToPlainText >> wordWrap 74) htmlText
        // Send the e-mails
        let! recipients =
            match m.SendToClass with
            | "N" when usr.isAdmin -> ctx.db.AllUsersAsMembers ()
            | _ -> ctx.db.AllMembersForSmallGroup grp.smallGroupId
        use! client = Email.getConnection ()
        do! Email.sendEmails client recipients grp
                s["Announcement for {0} - {1:MMMM d, yyyy} {2}", grp.name, now.Date,
                  (now.ToString "h:mm tt").ToLower ()].Value
                htmlText plainText s
        // Add to the request list if desired
        match m.SendToClass, m.AddToRequestList with
        | "N", _
        | _, None  -> ()
        | _, Some x when not x -> ()
        | _, _ ->
            { PrayerRequest.empty with
                prayerRequestId = Guid.NewGuid ()
                smallGroupId    = grp.smallGroupId
                userId          = usr.userId
                requestType     = (Option.get >> PrayerRequestType.fromCode) m.RequestType
                text            = requestText
                enteredDate     = now
                updatedDate     = now
            }
            |> ctx.db.AddEntry
            let! _ = ctx.db.SaveChangesAsync ()
            ()
        // Tell 'em what they've won, Johnny!
        let toWhom =
            match m.SendToClass with
            | "N" -> s["{0} users", s["PrayerTracker"]].Value
            | _ -> s["Group Members"].Value.ToLower ()
        let andAdded = match m.AddToRequestList with Some x when x -> "and added it to the request list" | _ -> ""
        addInfo ctx s["Successfully sent announcement to all {0} {1}", toWhom, s[andAdded]]
        return!
            viewInfo ctx startTicks
            |> Views.SmallGroup.announcementSent { m with Text = htmlText }
            |> renderHtml next ctx
    | Result.Error e -> return! bindError e next ctx
}
