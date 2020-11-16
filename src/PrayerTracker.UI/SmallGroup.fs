module PrayerTracker.Views.SmallGroup

open Giraffe.GiraffeViewEngine
open Microsoft.Extensions.Localization
open PrayerTracker
open PrayerTracker.Entities
open PrayerTracker.ViewModels
open System.IO

/// View for the announcement page
let announcement isAdmin ctx vi =
  let s        = I18N.localizer.Force ()
  let reqTypes = ReferenceList.requestTypeList s
  [ form [ _action "/web/small-group/announcement/send"; _method "post"; _class "pt-center-columns" ] [
      csrfToken ctx
      div [ _class "pt-field-row" ] [
        div [ _class "pt-field pt-editor" ] [
          label [ _for "text" ] [ locStr s.["Announcement Text"] ]
          textarea [ _name "text"; _id "text"; _autofocus ] []
          ]
        ]
      match isAdmin with
      | true ->
          div [ _class "pt-field-row" ] [
            div [ _class "pt-field" ] [
              label [] [ locStr s.["Send Announcement to"]; rawText ":" ]
              div [ _class "pt-center-text" ] [
                radio "sendToClass" "sendY" "Y" "Y"
                label [ _for "sendY" ] [ locStr s.["This Group"]; rawText " &nbsp; &nbsp; " ]
                radio "sendToClass" "sendN" "N" "Y"
                label [ _for "sendN" ] [ locStr s.["All {0} Users", s.["PrayerTracker"]] ]
                ]
              ]
            ]
      | false -> input [ _type "hidden"; _name "sendToClass"; _value "Y" ]
      div [ _class "pt-field-row pt-fadeable pt-shown"; _id "divAddToList" ] [
        div [ _class "pt-checkbox-field" ] [
          input [ _type "checkbox"; _name "addToRequestList"; _id "addToRequestList"; _value "True" ]
          label [ _for "addToRequestList" ] [ locStr s.["Add to Request List"] ]
          ]
        ]
      div [ _class "pt-field-row pt-fadeable"; _id "divCategory" ] [
        div [ _class "pt-field" ] [
          label [ _for "requestType" ] [ locStr s.["Request Type"] ]
          reqTypes
          |> Seq.ofList
          |> Seq.map (fun (typ, desc) -> typ.code, desc.Value)
          |> selectList "requestType" "Announcement" []
          ]
        ]
      div [ _class "pt-field-row" ] [ submit [] "send" s.["Send Announcement"] ]
      ]
    script [] [ rawText "PT.onLoad(PT.smallGroup.announcement.onPageLoad)" ]
    ]
  |> Layout.Content.standard
  |> Layout.standard vi "Send Announcement"


/// View for once an announcement has been sent
let announcementSent (m : Announcement) vi =
  let s = I18N.localizer.Force ()
  [ span [ _class "pt-email-heading" ] [ locStr s.["HTML Format"]; rawText ":" ]
    div [ _class "pt-email-canvas" ] [ rawText m.text ]
    br []
    br []
    span [ _class "pt-email-heading" ] [ locStr s.["Plain-Text Format"]; rawText ":" ]
    div [ _class "pt-email-canvas" ] [ pre [] [ str (m.plainText ()) ] ]
    ]
  |> Layout.Content.standard
  |> Layout.standard vi "Announcement Sent"


/// View for the small group add/edit page
let edit (m : EditSmallGroup) (churches : Church list) ctx vi =
  let s         = I18N.localizer.Force ()
  let pageTitle = match m.isNew () with true -> "Add a New Group" | false -> "Edit Group"
  form [ _action "/web/small-group/save"; _method "post"; _class "pt-center-columns" ] [
    csrfToken ctx
    input [ _type "hidden"; _name "smallGroupId"; _value (flatGuid m.smallGroupId) ]
    div [ _class "pt-field-row" ] [
      div [ _class "pt-field" ] [
        label [ _for "name" ] [ locStr s.["Group Name"] ]
        input [ _type "text"; _name "name"; _id "name"; _value m.name; _required; _autofocus ]
        ]
      ]
    div [ _class "pt-field-row" ] [
      div [ _class "pt-field" ] [
        label [ _for "churchId" ] [ locStr s.["Church"] ]
        seq {
          "", selectDefault s.["Select Church"].Value
          yield! churches |> List.map (fun c -> flatGuid c.churchId, c.name)
          }
        |> selectList "churchId" (flatGuid m.churchId) [ _required ] 
        ]
      ]
    div [ _class "pt-field-row" ] [ submit [] "save" s.["Save Group"] ]
    ]
  |> List.singleton
  |> Layout.Content.standard
  |> Layout.standard vi pageTitle


/// View for the member edit page
let editMember (m : EditMember) (typs : (string * LocalizedString) seq) ctx vi =
  let s         = I18N.localizer.Force ()
  let pageTitle = match m.isNew () with true -> "Add a New Group Member" | false -> "Edit Group Member"
  form [ _action "/web/small-group/member/save"; _method "post"; _class "pt-center-columns" ] [
    style [ _scoped ] [ rawText "#memberName { width: 15rem; } #emailAddress { width: 20rem; }" ]
    csrfToken ctx
    input [ _type "hidden"; _name "memberId"; _value (flatGuid m.memberId) ]
    div [ _class "pt-field-row" ] [
      div [ _class "pt-field" ] [
        label [ _for "memberName" ] [ locStr s.["Member Name"] ]
        input [ _type "text"; _name "memberName"; _id "memberName"; _required; _autofocus; _value m.memberName ]
        ]
      div [ _class "pt-field" ] [
        label [ _for "emailAddress" ] [ locStr s.["E-mail Address"] ]
        input [ _type "email"; _name "emailAddress"; _id "emailAddress"; _required; _value m.emailAddress ]
        ]
      ]
    div [ _class "pt-field-row" ] [
      div [ _class "pt-field" ] [
        label [ _for "emailType" ] [ locStr s.["E-mail Format"] ]
        typs
        |> Seq.map (fun typ -> fst typ, (snd typ).Value)
        |> selectList "emailType" m.emailType []
        ]
      ]
    div [ _class "pt-field-row" ] [ submit [] "save" s.["Save"] ]
    ]
  |> List.singleton
  |> Layout.Content.standard
  |> Layout.standard vi pageTitle


/// View for the small group log on page
let logOn (grps : SmallGroup list) grpId ctx vi =
  let s = I18N.localizer.Force ()
  [ form [ _action "/web/small-group/log-on/submit"; _method "post"; _class "pt-center-columns" ] [
      csrfToken ctx
      div [ _class "pt-field-row" ] [
        div [ _class "pt-field" ] [
          label [ _for "smallGroupId" ] [ locStr s.["Group"] ]
          seq {
            match grps.Length with
            | 0 -> "", s.["There are no classes with passwords defined"].Value
            | _ ->
                "", selectDefault s.["Select Group"].Value
                yield! grps
                  |> List.map (fun grp -> flatGuid grp.smallGroupId, $"{grp.church.name} | {grp.name}")
            }
          |> selectList "smallGroupId" grpId [ _required ]
          ]
        div [ _class "pt-field" ] [
          label [ _for "password" ] [ locStr s.["Password"] ]
          input [ _type "password"; _name "password"; _id "password"; _required;
                  _placeholder (s.["Case-Sensitive"].Value.ToLower ()) ]
          ]
        ]
      div [ _class "pt-checkbox-field" ] [
        input [ _type "checkbox"; _name "rememberMe"; _id "rememberMe"; _value "True" ]
        label [ _for "rememberMe" ] [ locStr s.["Remember Me"] ]
        br []
        small [] [ em [] [ str (s.["Requires Cookies"].Value.ToLower ()) ] ]
        ]
      div [ _class "pt-field-row" ] [ submit [] "account_circle" s.["Log On"] ]
      ]
    script [] [ rawText "PT.onLoad(PT.smallGroup.logOn.onPageLoad)" ]
    ]
  |> Layout.Content.standard
  |> Layout.standard vi "Group Log On"


/// View for the small group maintenance page
let maintain (grps : SmallGroup list) ctx vi =
  let s      = I18N.localizer.Force ()
  let grpTbl =
    match grps with
    | [] -> space
    | _ ->
        table [ _class "pt-table pt-action-table" ] [
          thead [] [
            tr [] [
              th [] [ locStr s.["Actions"] ]
              th [] [ locStr s.["Name"] ]
              th [] [ locStr s.["Church"] ]
              th [] [ locStr s.["Time Zone"] ]
              ]
            ]
          grps
          |> List.map (fun g ->
              let grpId     = flatGuid g.smallGroupId
              let delAction = $"/web/small-group/{grpId}/delete"
              let delPrompt = s.["Are you sure you want to delete this {0}?  This action cannot be undone.",
                                   $"""{s.["Small Group"].Value.ToLower ()} ({g.name})""" ].Value
              tr [] [
                td [] [
                  a [ _href $"/web/small-group/{grpId}/edit"; _title s.["Edit This Group"].Value ] [ icon "edit" ]
                  a [ _href delAction
                      _title s.["Delete This Group"].Value
                      _onclick $"return PT.confirmDelete('{delAction}','{delPrompt}')" ]
                    [ icon "delete_forever" ]
                  ]
                td [] [ str g.name ]
                td [] [ str g.church.name ]
                td [] [ locStr (TimeZones.name g.preferences.timeZoneId s) ]
                ])
          |> tbody []
          ]
  [ div [ _class "pt-center-text" ] [
      br []
      a [ _href $"/web/small-group/{emptyGuid}/edit"; _title s.["Add a New Group"].Value ] [
        icon "add_circle"
        rawText " &nbsp;"
        locStr s.["Add a New Group"]
        ]
      br []
      br []
      ]
    tableSummary grps.Length s
    grpTbl
    form [ _id "DeleteForm"; _action ""; _method "post" ] [ csrfToken ctx ]
    ]
  |> Layout.Content.standard
  |> Layout.standard vi "Maintain Groups"


/// View for the member maintenance page
let members (mbrs : Member list) (emailTyps : Map<string, LocalizedString>) ctx vi =
  let s      = I18N.localizer.Force ()
  let mbrTbl =
    match mbrs with
    | [] -> space
    | _ ->
        table [ _class "pt-table pt-action-table" ] [
          thead [] [
            tr [] [
              th [] [ locStr s.["Actions"] ]
              th [] [ locStr s.["Name"] ]
              th [] [ locStr s.["E-mail Address"] ]
              th [] [ locStr s.["Format"] ]
              ]
            ]
          mbrs
          |> List.map (fun mbr ->
              let mbrId     = flatGuid mbr.memberId
              let delAction = $"/web/small-group/member/{mbrId}/delete"
              let delPrompt =
                s.["Are you sure you want to delete this {0}?  This action cannot be undone.", s.["group member"]]
                  .Value
                  .Replace("?", $" ({mbr.memberName})?")
              tr [] [
                td [] [
                  a [ _href $"/web/small-group/member/{mbrId}/edit"; _title s.["Edit This Group Member"].Value ]
                    [ icon "edit" ]
                  a [ _href delAction
                      _title s.["Delete This Group Member"].Value
                      _onclick $"return PT.confirmDelete('{delAction}','{delPrompt}')" ]
                    [ icon "delete_forever" ]
                  ]
                td [] [ str mbr.memberName ]
                td [] [ str mbr.email ]
                td [] [ locStr emailTyps.[defaultArg mbr.format ""] ]
                ])
          |> tbody []
          ]
  [ div [ _class"pt-center-text" ] [
      br []
      a [ _href $"/web/small-group/member/{emptyGuid}/edit"; _title s.["Add a New Group Member"].Value ]
        [ icon "add_circle"; rawText " &nbsp;"; locStr s.["Add a New Group Member"] ]
      br []
      br []
      ]
    tableSummary mbrs.Length s
    mbrTbl
    form [ _id "DeleteForm"; _action ""; _method "post" ] [ csrfToken ctx ]
    ]
  |> Layout.Content.standard
  |> Layout.standard vi "Maintain Group Members"


/// View for the small group overview page
let overview m vi =
  let s          = I18N.localizer.Force ()
  let linkSpacer = rawText "&nbsp; "
  let typs       = ReferenceList.requestTypeList s |> dict
  article [ _class "pt-overview" ] [
    section [] [
      header [ _role "heading" ] [
        iconSized 72 "bookmark_border"
        locStr s.["Quick Actions"]
        ]
      div [] [
        a [ _href "/web/prayer-requests/view" ] [ icon "list"; linkSpacer; locStr s.["View Prayer Request List"] ]
        hr []
        a [ _href "/web/small-group/announcement" ] [ icon "send"; linkSpacer; locStr s.["Send Announcement"] ]
        hr []
        a [ _href "/web/small-group/preferences" ] [ icon "build"; linkSpacer; locStr s.["Change Preferences"] ]
        ]
      ]
    section [] [
      header [ _role "heading" ] [
        iconSized 72 "question_answer"
        locStr s.["Prayer Requests"]
        ]
      div [] [
        p [ _class "pt-center-text" ] [
          strong [] [ str (m.totalActiveReqs.ToString "N0"); space; locStr s.["Active Requests"] ]
          ]
        hr []
        for cat in m.activeReqsByCat do
          str (cat.Value.ToString "N0")
          space
          locStr typs.[cat.Key]
          br []
        br []
        str (m.allReqs.ToString "N0")
        space
        locStr s.["Total Requests"]
        hr []
        a [ _href "/web/prayer-requests/maintain" ] [
          icon "compare_arrows"
          linkSpacer
          locStr s.["Maintain Prayer Requests"]
          ]
        ]
      ]
    section [] [
      header [ _role "heading" ] [
        iconSized 72 "people_outline"
        locStr s.["Group Members"]
        ]
      div [ _class "pt-center-text" ] [
        strong [] [ str (m.totalMbrs.ToString "N0"); space; locStr s.["Members"] ]
        hr []
        a [ _href "/web/small-group/members" ] [ icon "email"; linkSpacer; locStr s.["Maintain Group Members"] ]
        ]
      ]
    ]
  |> List.singleton
  |> Layout.Content.wide
  |> Layout.standard vi "Small Group Overview"


/// View for the small group preferences page
let preferences (m : EditPreferences) (tzs : TimeZone list) ctx vi =
  let s   = I18N.localizer.Force ()
  let l   = I18N.forView "SmallGroup/Preferences"
  use sw  = new StringWriter ()
  let raw = rawLocText sw
  [ form [ _action "/web/small-group/preferences/save"; _method "post"; _class "pt-center-columns" ] [
      style [ _scoped ] [ rawText "#expireDays, #daysToKeepNew, #longTermUpdateWeeks, #headingFontSize, #listFontSize, #pageSize { width: 3rem; } #emailFromAddress { width: 20rem; } #listFonts { width: 40rem; } @media screen and (max-width: 40rem) { #listFonts { width: 100%; } }" ]
      csrfToken ctx
      fieldset [] [
        legend [] [ strong [] [ icon "date_range"; rawText " &nbsp;"; locStr s.["Dates"] ] ]
        div [ _class "pt-field-row" ] [
          div [ _class "pt-field" ] [
            label [ _for "expireDays" ] [ locStr s.["Requests Expire After"] ]
            span [] [
              input [ _type "number"; _name "expireDays"; _id "expireDays"; _min "1"; _max "30"; _required; _autofocus
                      _value (string m.expireDays) ]
              space; str (s.["Days"].Value.ToLower ())
              ]
            ]
          div [ _class "pt-field" ] [
            label [ _for "daysToKeepNew" ] [ locStr s.["Requests “New” For"] ]
            span [] [
              input [ _type "number"; _name "daysToKeepNew"; _id "daysToKeepNew"; _min "1"; _max "30"; _required
                      _value (string m.daysToKeepNew) ]
              space; str (s.["Days"].Value.ToLower ())
              ]
            ]
          div [ _class "pt-field" ] [
            label [ _for "longTermUpdateWeeks" ] [ locStr s.["Long-Term Requests Alerted for Update"] ]
            span [] [
              input [ _type "number"; _name "longTermUpdateWeeks"; _id "longTermUpdateWeeks"; _min "1"; _max "30"
                      _required; _value (string m.longTermUpdateWeeks) ]
              space; str (s.["Weeks"].Value.ToLower ())
              ]
            ]
          ]
        ]
      fieldset [] [
        legend [] [ strong [] [ icon "sort"; rawText " &nbsp;"; locStr s.["Request Sorting"] ] ]
        radio "requestSort" "requestSort_D" "D" m.requestSort
        label [ _for "requestSort_D" ] [ locStr s.["Sort by Last Updated Date"] ]
        rawText " &nbsp; "
        radio "requestSort" "requestSort_R" "R" m.requestSort
        label [ _for "requestSort_R" ] [ locStr s.["Sort by Requestor Name"] ]
        ]
      fieldset [] [
        legend [] [ strong [] [ icon "mail_outline"; rawText " &nbsp;"; locStr s.["E-mail"] ] ]
        div [ _class "pt-field-row" ] [
          div [ _class "pt-field" ] [
            label [ _for "emailFromName" ] [ locStr s.["From Name"] ]
            input [ _type "text"; _name "emailFromName"; _id "emailFromName"; _required; _value m.emailFromName ]
            ]
          div [ _class "pt-field" ] [
            label [ _for "emailFromAddress" ] [ locStr s.["From Address"] ]
            input [ _type "email"; _name "emailFromAddress"; _id "emailFromAddress"; _required
                    _value m.emailFromAddress ]
            ]
          ]
        div [ _class "pt-field-row" ] [
          div [ _class "pt-field" ] [
            label [ _for "defaultEmailType" ] [ locStr s.["E-mail Format"] ]
            seq {
              "", selectDefault s.["Select"].Value
              yield! ReferenceList.emailTypeList HtmlFormat s
                |> Seq.skip 1
                |> Seq.map (fun typ -> fst typ, (snd typ).Value)
              }
            |> selectList "defaultEmailType" m.defaultEmailType [ _required ]
            ]
          ]
        ]
      fieldset [] [
        legend [] [ strong [] [ icon "color_lens"; rawText " &nbsp;"; locStr s.["Colors"] ]; rawText " ***" ]
        div [ _class "pt-field-row" ] [
          div [ _class "pt-field" ] [
            label [ _class "pt-center-text" ] [ locStr s.["Color of Heading Lines"] ]
            span [] [
              radio "headingLineType" "headingLineType_Name" "Name" m.headingLineType
              label [ _for "headingLineType_Name" ] [ locStr s.["Named Color"] ]
              namedColorList "headingLineColor" m.headingLineColor
                [ _id "headingLineColor_Select"
                  match m.headingLineColor.StartsWith "#" with true -> _disabled | false -> () ] s
              rawText "&nbsp; &nbsp; "; str (s.["or"].Value.ToUpper ())
              radio "headingLineType" "headingLineType_RGB" "RGB" m.headingLineType
              label [ _for "headingLineType_RGB" ] [ locStr s.["Custom Color"] ]
              input [ _type "color" 
                      _name "headingLineColor"
                      _id "headingLineColor_Color"
                      _value m.headingLineColor
                      match m.headingLineColor.StartsWith "#" with true -> () | false -> _disabled ]
              ]
            ]
          ]
        div [ _class "pt-field-row" ] [
          div [ _class "pt-field" ] [
            label [ _class "pt-center-text" ] [ locStr s.["Color of Heading Text"] ]
            span [] [
              radio "headingTextType" "headingTextType_Name" "Name" m.headingTextType
              label [ _for "headingTextType_Name" ] [ locStr s.["Named Color"] ]
              namedColorList "headingTextColor" m.headingTextColor
                [ _id "headingTextColor_Select"
                  match m.headingTextColor.StartsWith "#" with true -> _disabled | false -> () ] s
              rawText "&nbsp; &nbsp; "; str (s.["or"].Value.ToUpper ())
              radio "headingTextType" "headingTextType_RGB" "RGB" m.headingTextType
              label [ _for "headingTextType_RGB" ] [ locStr s.["Custom Color"] ]
              input [ _type "color"
                      _name "headingTextColor"
                      _id "headingTextColor_Color"
                      _value m.headingTextColor
                      match m.headingTextColor.StartsWith "#" with true -> () | false -> _disabled ]
              ]
            ]
          ]
        ]
      fieldset [] [
        legend [] [ strong [] [ icon "font_download"; rawText " &nbsp;"; locStr s.["Fonts"] ] ]
        div [ _class "pt-field" ] [
          label [ _for "listFonts" ] [ locStr s.["Fonts** for List"] ]
          input [ _type "text"; _name "listFonts"; _id "listFonts"; _required; _value m.listFonts ]
          ]
        div [ _class "pt-field-row" ] [
          div [ _class "pt-field" ] [
            label [ _for "headingFontSize" ] [ locStr s.["Heading Text Size"] ]
            input [ _type "number"; _name "headingFontSize"; _id "headingFontSize"; _min "8"; _max "24"; _required
                    _value (string m.headingFontSize) ]
            ]
          div [ _class "pt-field" ] [
            label [ _for "listFontSize" ] [ locStr s.["List Text Size"] ]
            input [ _type "number"; _name "listFontSize"; _id "listFontSize"; _min "8"; _max "24"; _required
                    _value (string m.listFontSize) ]
            ]
          ]
        ]
      fieldset [] [
        legend [] [ strong [] [ icon "settings"; rawText " &nbsp;"; locStr s.["Other Settings"] ] ]
        div [ _class "pt-field-row" ] [
          div [ _class "pt-field" ] [
            label [ _for "timeZone" ] [ locStr s.["Time Zone"] ]
            seq {
              "", selectDefault s.["Select"].Value
              yield! tzs |> List.map (fun tz -> tz.timeZoneId, (TimeZones.name tz.timeZoneId s).Value)
              }
            |> selectList "timeZone" m.timeZone [ _required ]
            ]
          ]
        div [ _class "pt-field" ] [
          label [] [ locStr s.["Request List Visibility"] ]
          span [] [
            radio "listVisibility" "viz_Public" (string RequestVisibility.``public``) (string m.listVisibility)
            label [ _for "viz_Public" ] [ locStr s.["Public"] ]
            rawText " &nbsp;"
            radio "listVisibility" "viz_Private" (string RequestVisibility.``private``) (string m.listVisibility)
            label [ _for "viz_Private" ] [ locStr s.["Private"] ]
            rawText " &nbsp;"
            radio "listVisibility" "viz_Password" (string RequestVisibility.passwordProtected) (string m.listVisibility)
            label [ _for "viz_Password" ] [ locStr s.["Password Protected"] ]
            ]
          ]
        div [ _id "divClassPassword"
              match m.listVisibility = RequestVisibility.passwordProtected with
              | true -> _class "pt-field-row pt-fadeable pt-show"
              | false -> _class "pt-field-row pt-fadeable"
              ] [
          div [ _class "pt-field" ] [
            label [ _for "groupPassword" ] [ locStr s.["Group Password (Used to Read Online)"] ]
            input [ _type "text"; _name "groupPassword"; _id "groupPassword";
                    _value (match m.groupPassword with Some x -> x | None -> "") ]
            ]
          ]
        div [ _class "pt-field-row" ] [
          div [ _class "pt-field" ] [
            label [ _for "pageSize" ] [ locStr s.["Page Size"] ]
            input [ _type "number"; _name "pageSize"; _id "pageSize"; _min "10"; _max "255"; _required
                    _value (string m.pageSize) ]
            ]
          div [ _class "pt-field" ] [
            label [ _for "asOfDate" ] [ locStr s.["“As of” Date Display"] ]
            ReferenceList.asOfDateList s
            |> List.map (fun (code, desc) -> code, desc.Value)
            |> selectList "asOfDate" m.asOfDate [ _required ]
            ]
          ]
        ]
      div [ _class "pt-field-row" ] [ submit [] "save" s.["Save Preferences"] ]
      ]
    p [] [
      rawText "** "
      raw l.["List font names, separated by commas."]
      space
      raw l.["The first font that is matched is the one that is used."]
      space
      raw l.["Ending with either “serif” or “sans-serif” will cause the user's browser to use the default “serif” font (“Times New Roman” on Windows) or “sans-serif” font (“Arial” on Windows) if no other fonts in the list are found."]
      ]
    p [] [
      rawText "*** "
      raw l.["If you want a custom color, you may be able to get some ideas (and a list of RGB values for those colors) from the W3 School's <a href=\"http://www.w3schools.com/html/html_colornames.asp\" title=\"HTML Color List - W3 School\">HTML color name list</a>."]
      ]
    script [] [ rawText "PT.onLoad(PT.smallGroup.preferences.onPageLoad)" ]
    ]
  |> Layout.Content.standard
  |> Layout.standard vi "Group Preferences"
