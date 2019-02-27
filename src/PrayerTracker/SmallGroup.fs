module PrayerTracker.Handlers.SmallGroup

open FSharp.Control.Tasks.V2.ContextInsensitive
open Giraffe
open Giraffe.GiraffeViewEngine
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
    (Key.Cookie.group, { GroupId = (currentGroup ctx).smallGroupId; PasswordHash = pwHash }.toPayload(), autoRefresh)


/// GET /small-group/announcement
let announcement : HttpHandler =
  requireAccess [ User ]
  >=> fun next ctx ->
    let startTicks = DateTime.Now.Ticks
    { viewInfo ctx startTicks with helpLink = Some Help.sendAnnouncement; script = [ "ckeditor/ckeditor" ] }
    |> Views.SmallGroup.announcement (currentUser ctx).isAdmin ctx
    |> renderHtml next ctx


/// POST /small-group/[group-id]/delete
let delete groupId : HttpHandler =
  requireAccess [ Admin ]
  >=> validateCSRF
  >=> fun next ctx ->
    let db = ctx.dbContext ()
    let s  = Views.I18N.localizer.Force ()
    task {
      let! grp = db.TryGroupById groupId
      match grp with
      | Some g ->
          let! reqs = db.CountRequestsBySmallGroup groupId
          let! usrs = db.CountUsersBySmallGroup    groupId
          db.RemoveEntry g
          let! _ = db.SaveChangesAsync ()
          addInfo ctx
            s.["The group {0} and its {1} prayer request(s) were deleted successfully; revoked access from {2} user(s)",
                 g.name, reqs, usrs]
          return! redirectTo false "/small-groups" next ctx
      | None -> return! fourOhFour next ctx
      }


/// POST /small-group/member/[member-id]/delete
let deleteMember memberId : HttpHandler =
  requireAccess [ User ]
  >=> validateCSRF
  >=> fun next ctx ->
    let db = ctx.dbContext ()
    let s  = Views.I18N.localizer.Force ()
    task {
      let! mbr = db.TryMemberById memberId
      match mbr with
      | Some m when m.smallGroupId = (currentGroup ctx).smallGroupId ->
          db.RemoveEntry m
          let! _ = db.SaveChangesAsync ()
          addHtmlInfo ctx s.["The group member &ldquo;{0}&rdquo; was deleted successfully", m.memberName]
          return! redirectTo false "/small-group/members" next ctx
      | Some _
      | None -> return! fourOhFour next ctx
      }


/// GET /small-group/[group-id]/edit
let edit (groupId : SmallGroupId) : HttpHandler =
  requireAccess [ Admin ]
  >=> fun next ctx ->
    let startTicks = DateTime.Now.Ticks
    let db         = ctx.dbContext ()
    task {
      let! churches = db.AllChurches ()
      match groupId = Guid.Empty with
      | true ->
          return!
            viewInfo ctx startTicks
            |> Views.SmallGroup.edit EditSmallGroup.empty churches ctx
            |> renderHtml next ctx
      | false ->
          let! grp = db.TryGroupById groupId
          match grp with
          | Some g ->
              return!
                viewInfo ctx startTicks
                |> Views.SmallGroup.edit (EditSmallGroup.fromGroup g) churches ctx
                |> renderHtml next ctx
          | None -> return! fourOhFour next ctx
      }


/// GET /small-group/member/[member-id]/edit
let editMember (memberId : MemberId) : HttpHandler =
  requireAccess [ User ]
  >=> fun next ctx ->
    let startTicks = DateTime.Now.Ticks
    let db         = ctx.dbContext ()
    let s          = Views.I18N.localizer.Force ()
    let grp        = currentGroup ctx
    let typs       = ReferenceList.emailTypeList grp.preferences.defaultEmailType s
    task {
      match memberId = Guid.Empty with
      | true ->
          return!
            viewInfo ctx startTicks
            |> Views.SmallGroup.editMember EditMember.empty typs ctx
            |> renderHtml next ctx
      | false ->
          let! mbr = db.TryMemberById memberId
          match mbr with
          | Some m when m.smallGroupId = grp.smallGroupId ->
              return!
                viewInfo ctx startTicks
                |> Views.SmallGroup.editMember (EditMember.fromMember m) typs ctx
                |> renderHtml next ctx
          | Some _
          | None -> return! fourOhFour next ctx
      }


/// GET /small-group/log-on/[group-id?]
let logOn (groupId : SmallGroupId option) : HttpHandler =
  requireAccess [ AccessLevel.Public ]
  >=> fun next ctx ->
    let startTicks = DateTime.Now.Ticks
    task {
      let! grps  = ctx.dbContext().ProtectedGroups ()
      let  grpId = match groupId with Some gid -> flatGuid gid |  None -> ""
      return!
        { viewInfo ctx startTicks with helpLink = Some Help.logOn }
        |> Views.SmallGroup.logOn grps grpId ctx
        |> renderHtml next ctx
      }


/// POST /small-group/log-on/submit
let logOnSubmit : HttpHandler =
  requireAccess [ AccessLevel.Public ]
  >=> validateCSRF
  >=> fun next ctx ->
    task {
      let! result = ctx.TryBindFormAsync<GroupLogOn> ()
      match result with
      | Ok m ->
        let  s   = Views.I18N.localizer.Force ()
        let! grp = ctx.dbContext().TryGroupLogOnByPassword m.smallGroupId m.password
        match grp with
        | Some _ ->
            ctx.Session.SetSmallGroup grp
            match m.rememberMe with
            | Some x when x -> (setGroupCookie ctx << Utils.sha1Hash) m.password
            | _ -> ()
            addInfo ctx s.["Log On Successful • Welcome to {0}", s.["PrayerTracker"]]
            return! redirectTo false "/prayer-requests/view" next ctx
        | None ->
            addError ctx s.["Password incorrect - login unsuccessful"]
            return! redirectTo false (sprintf "/small-group/log-on/%s" (flatGuid m.smallGroupId)) next ctx
      | Error e -> return! bindError e next ctx
      }


/// GET /small-groups
let maintain : HttpHandler =
  requireAccess [ Admin ]
  >=> fun next ctx ->
    let startTicks = DateTime.Now.Ticks
    task {
      let! grps = ctx.dbContext().AllGroups ()
      return!
        viewInfo ctx startTicks
        |> Views.SmallGroup.maintain grps ctx
        |> renderHtml next ctx
      }


/// GET /small-group/members
let members : HttpHandler =
  requireAccess [ User ]
  >=> fun next ctx ->
    let startTicks = DateTime.Now.Ticks
    let db         = ctx.dbContext ()
    let grp        = currentGroup ctx
    let s          = Views.I18N.localizer.Force ()
    task {
      let! mbrs = db.AllMembersForSmallGroup grp.smallGroupId
      let  typs = ReferenceList.emailTypeList grp.preferences.defaultEmailType s |> Map.ofSeq
      return!
        { viewInfo ctx startTicks with helpLink = Some Help.maintainGroupMembers }
        |> Views.SmallGroup.members mbrs typs ctx
        |> renderHtml next ctx
      }


/// GET /small-group
let overview : HttpHandler =
  requireAccess [ User ]
  >=> fun next ctx ->
    let startTicks = DateTime.Now.Ticks
    let db         = ctx.dbContext ()
    let clock      = ctx.GetService<IClock> ()
    task {
      let  reqs     = db.AllRequestsForSmallGroup  (currentGroup ctx) clock None true |> List.ofSeq
      let! reqCount = db.CountRequestsBySmallGroup (currentGroup ctx).smallGroupId
      let! mbrCount = db.CountMembersForSmallGroup (currentGroup ctx).smallGroupId
      let m =
        { totalActiveReqs = List.length reqs
          allReqs         = reqCount
          totalMbrs       = mbrCount
          activeReqsByCat =
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
let preferences : HttpHandler =
  requireAccess [ User ]
  >=> fun next ctx ->
    let startTicks = DateTime.Now.Ticks
    task {
      let! tzs = ctx.dbContext().AllTimeZones ()
      return!
        { viewInfo ctx startTicks with helpLink = Some Help.groupPreferences }
        |> Views.SmallGroup.preferences (EditPreferences.fromPreferences (currentGroup ctx).preferences) tzs ctx
        |> renderHtml next ctx
      }


/// POST /small-group/save
let save : HttpHandler =
  requireAccess [ Admin ]
  >=> validateCSRF
  >=> fun next ctx ->
    let s = Views.I18N.localizer.Force ()
    task {
      let! result = ctx.TryBindFormAsync<EditSmallGroup> ()
      match result with
      | Ok m ->
          let db = ctx.dbContext ()
          let! grp =
            match m.isNew () with
            | true -> Task.FromResult<SmallGroup option>(Some { SmallGroup.empty with smallGroupId = Guid.NewGuid () })
            | false -> db.TryGroupById m.smallGroupId
          match grp with
          | Some g ->
              m.populateGroup g
              |> function
              | g when m.isNew () ->
                  db.AddEntry g
                  db.AddEntry { g.preferences with smallGroupId = g.smallGroupId }
              | g -> db.UpdateEntry g
              let! _ = db.SaveChangesAsync ()
              let act = s.[match m.isNew () with true -> "Added" | false -> "Updated"].Value.ToLower ()
              addHtmlInfo ctx s.["Successfully {0} group “{1}”", act, m.name]
              return! redirectTo false "/small-groups" next ctx
          | None -> return! fourOhFour next ctx
      | Error e -> return! bindError e next ctx
      }


/// POST /small-group/member/save
let saveMember : HttpHandler =
  requireAccess [ User ]
  >=> validateCSRF
  >=> fun next ctx ->
    task {
      let! result = ctx.TryBindFormAsync<EditMember> ()
      match result with
      | Ok m ->
          let  grp  = currentGroup ctx
          let  db   = ctx.dbContext ()
          let! mMbr =
            match m.isNew () with
            | true ->
                Task.FromResult<Member option>
                  (Some
                    { Member.empty with
                        memberId     = Guid.NewGuid ()
                        smallGroupId = grp.smallGroupId
                      })
            | false -> db.TryMemberById m.memberId
          match mMbr with
          | Some mbr when mbr.smallGroupId = grp.smallGroupId ->
              { mbr with
                  memberName = m.memberName
                  email      = m.emailAddress
                  format     = match m.emailType with "" | null -> None | _ -> Some m.emailType
                }
              |> (match m.isNew () with true -> db.AddEntry | false -> db.UpdateEntry)
              let! _ = db.SaveChangesAsync ()
              let s = Views.I18N.localizer.Force ()
              let act = s.[match m.isNew () with true -> "Added" | false -> "Updated"].Value.ToLower ()
              addInfo ctx s.["Successfully {0} group member", act]
              return! redirectTo false "/small-group/members" next ctx
          | Some _
          | None -> return! fourOhFour next ctx
      | Error e -> return! bindError e next ctx
      }


/// POST /small-group/preferences/save
let savePreferences : HttpHandler =
  requireAccess [ User ]
  >=> validateCSRF
  >=> fun next ctx ->
    task {
      let! result = ctx.TryBindFormAsync<EditPreferences> ()
      match result with
      | Ok m ->
          let db = ctx.dbContext ()
          // Since the class is stored in the session, we'll use an intermediate instance to persist it; once that
          // works, we can repopulate the session instance.  That way, if the update fails, the page should still show
          // the database values, not the then out-of-sync session ones.
          let! grp = db.TryGroupById (currentGroup ctx).smallGroupId
          match grp with
          | Some g ->
              let prefs = m.populatePreferences g.preferences
              db.UpdateEntry prefs
              let! _ = db.SaveChangesAsync ()
              // Refresh session instance
              ctx.Session.SetSmallGroup <| Some { g with preferences = prefs }
              let s = Views.I18N.localizer.Force ()
              addInfo ctx s.["Group preferences updated successfully"]
              return! redirectTo false "/small-group/preferences" next ctx
          | None -> return! fourOhFour next ctx
      | Error e -> return! bindError e next ctx
      }


/// POST /small-group/announcement/send
let sendAnnouncement : HttpHandler =
  requireAccess [ User ]
  >=> validateCSRF
  >=> fun next ctx ->
    let startTicks = DateTime.Now.Ticks
    task {
      let! result = ctx.TryBindFormAsync<Announcement> ()
      match result with
      | Ok m ->
          let grp = currentGroup ctx
          let usr = currentUser ctx
          let db  = ctx.dbContext ()
          let now = grp.localTimeNow (ctx.GetService<IClock> ())
          let s   = Views.I18N.localizer.Force ()
          // Reformat the text to use the class's font stylings
          let requestText = ckEditorToText m.text
          let htmlText =
            p [ _style (sprintf "font-family:%s;font-size:%dpt;" grp.preferences.listFonts grp.preferences.textFontSize) ]
              [ rawText requestText ]
            |> renderHtmlNode
          let plainText = (htmlToPlainText >> wordWrap 74) htmlText
          // Send the e-mails
          let! recipients =
            match m.sendToClass with
            | "N" when usr.isAdmin -> db.AllUsersAsMembers ()
            | _ -> db.AllMembersForSmallGroup grp.smallGroupId
          use! client = Email.getConnection ()
          do! Email.sendEmails client recipients grp
                s.["Announcement for {0} - {1:MMMM d, yyyy} {2}",
                    grp.name, now.Date, (now.ToString "h:mm tt").ToLower ()].Value
                htmlText plainText s
          // Add to the request list if desired
          match m.sendToClass, m.addToRequestList with
          | "N", _
          | _, None  -> ()
          | _, Some x when not x -> ()
          | _, _ ->
              { PrayerRequest.empty with
                  prayerRequestId = Guid.NewGuid ()
                  smallGroupId    = grp.smallGroupId
                  userId          = usr.userId
                  requestType     = Option.get m.requestType
                  text            = requestText
                  enteredDate     = now
                  updatedDate     = now
                }
              |> db.AddEntry
              let! _ = db.SaveChangesAsync ()
              ()
          // Tell 'em what they've won, Johnny!
          let toWhom =
            match m.sendToClass with
            | "N" -> s.["{0} users", s.["PrayerTracker"]].Value
            | _ -> s.["Group Members"].Value.ToLower ()
          let andAdded = match m.addToRequestList with Some x when x -> "and added it to the request list" | _ -> ""
          addInfo ctx s.["Successfully sent announcement to all {0} {1}", toWhom, s.[andAdded]]
          return!
            viewInfo ctx startTicks
            |> Views.SmallGroup.announcementSent { m with text = htmlText }
            |> renderHtml next ctx
      | Error e -> return! bindError e next ctx
      }
