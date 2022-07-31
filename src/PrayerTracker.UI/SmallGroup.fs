module PrayerTracker.Views.SmallGroup

open Giraffe.ViewEngine
open Microsoft.Extensions.Localization
open PrayerTracker.Entities
open PrayerTracker.ViewModels

/// View for the announcement page
let announcement isAdmin ctx vi =
    let s        = I18N.localizer.Force ()
    let m        = { SendToClass = ""; Text = ""; AddToRequestList = None; RequestType = None }
    let reqTypes = ReferenceList.requestTypeList s
    [   form [ _action "/small-group/announcement/send"; _method "post"; _class "pt-center-columns"; Target.content ] [
            csrfToken ctx
            div [ _class "pt-field-row" ] [
                div [ _class "pt-field pt-editor" ] [
                    label [ _for "text" ] [ locStr s["Announcement Text"] ]
                    textarea [ _name (nameof m.Text); _id "text"; _autofocus ] []
                ]
            ]
            if isAdmin then
                div [ _class "pt-field-row" ] [
                    div [ _class "pt-field" ] [
                        label [] [ locStr s["Send Announcement to"]; rawText ":" ]
                        div [ _class "pt-center-text" ] [
                            radio (nameof m.SendToClass) "sendY" "Y" "Y"
                            label [ _for "sendY" ] [ locStr s["This Group"]; rawText " &nbsp; &nbsp; " ]
                            radio (nameof m.SendToClass) "sendN" "N" "Y"
                            label [ _for "sendN" ] [ locStr s["All {0} Users", s["PrayerTracker"]] ]
                        ]
                    ]
                ]
            else input [ _type "hidden"; _name (nameof m.SendToClass); _value "Y" ]
            div [ _class "pt-field-row pt-fadeable pt-shown"; _id "divAddToList" ] [
                div [ _class "pt-checkbox-field" ] [
                    input [ _type "checkbox"; _name (nameof m.AddToRequestList); _id "addToRequestList"; _value "True" ]
                    label [ _for "addToRequestList" ] [ locStr s["Add to Request List"] ]
                ]
            ]
            div [ _class "pt-field-row pt-fadeable"; _id "divCategory" ] [
                div [ _class "pt-field" ] [
                    label [ _for (nameof m.RequestType) ] [ locStr s["Request Type"] ]
                    reqTypes
                    |> Seq.ofList
                    |> Seq.map (fun (typ, desc) -> typ.code, desc.Value)
                    |> selectList (nameof m.RequestType) Announcement.code []
                ]
            ]
            div [ _class "pt-field-row" ] [ submit [] "send" s["Send Announcement"] ]
        ]
        script [] [ rawText "PT.onLoad(PT.smallGroup.announcement.onPageLoad)" ]
    ]
    |> Layout.Content.standard
    |> Layout.standard vi "Send Announcement"


/// View for once an announcement has been sent
let announcementSent (m : Announcement) vi =
    let s = I18N.localizer.Force ()
    [   span [ _class "pt-email-heading" ] [ locStr s["HTML Format"]; rawText ":" ]
        div [ _class "pt-email-canvas" ] [ rawText m.Text ]
        br []
        br []
        span [ _class "pt-email-heading" ] [ locStr s["Plain-Text Format"]; rawText ":" ]
        div [ _class "pt-email-canvas" ] [ pre [] [ str m.PlainText ] ]
    ]
    |> Layout.Content.standard
    |> Layout.standard vi "Announcement Sent"


/// View for the small group add/edit page
let edit (m : EditSmallGroup) (churches : Church list) ctx vi =
    let s         = I18N.localizer.Force ()
    let pageTitle = if m.IsNew then "Add a New Group" else "Edit Group"
    form [ _action "/small-group/save"; _method "post"; _class "pt-center-columns"; Target.content ] [
        csrfToken ctx
        input [ _type "hidden"; _name (nameof m.SmallGroupId); _value (flatGuid m.SmallGroupId) ]
        div [ _class "pt-field-row" ] [
            div [ _class "pt-field" ] [
                label [ _for "name" ] [ locStr s["Group Name"] ]
                input [ _type "text"; _name (nameof m.Name); _id "name"; _value m.Name; _required; _autofocus ]
            ]
        ]
        div [ _class "pt-field-row" ] [
            div [ _class "pt-field" ] [
                label [ _for (nameof m.ChurchId) ] [ locStr s["Church"] ]
                seq {
                  "", selectDefault s["Select Church"].Value
                  yield! churches |> List.map (fun c -> flatGuid c.churchId, c.name)
                  }
                |> selectList (nameof m.ChurchId) (flatGuid m.ChurchId) [ _required ] 
            ]
        ]
        div [ _class "pt-field-row" ] [ submit [] "save" s["Save Group"] ]
    ]
    |> List.singleton
    |> Layout.Content.standard
    |> Layout.standard vi pageTitle


/// View for the member edit page
let editMember (m : EditMember) (types : (string * LocalizedString) seq) ctx vi =
    let s         = I18N.localizer.Force ()
    let pageTitle = if m.IsNew then "Add a New Group Member" else "Edit Group Member"
    form [ _action "/small-group/member/save"; _method "post"; _class "pt-center-columns"; Target.content ] [
        style [ _scoped ] [ rawText "#name { width: 15rem; } #email { width: 20rem; }" ]
        csrfToken ctx
        input [ _type "hidden"; _name (nameof m.MemberId); _value (flatGuid m.MemberId) ]
        div [ _class "pt-field-row" ] [
            div [ _class "pt-field" ] [
                label [ _for "name" ] [ locStr s["Member Name"] ]
                input [ _type "text"; _name (nameof m.Name); _id "name"; _required; _autofocus; _value m.Name ]
            ]
            div [ _class "pt-field" ] [
                label [ _for "email" ] [ locStr s["E-mail Address"] ]
                input [ _type "email"; _name (nameof m.Email); _id "email"; _required; _value m.Email ]
            ]
        ]
        div [ _class "pt-field-row" ] [
            div [ _class "pt-field" ] [
                label [ _for (nameof m.Format) ] [ locStr s["E-mail Format"] ]
                types
                |> Seq.map (fun typ -> fst typ, (snd typ).Value)
                |> selectList (nameof m.Format) m.Format []
            ]
        ]
        div [ _class "pt-field-row" ] [ submit [] "save" s["Save"] ]
    ]
    |> List.singleton
    |> Layout.Content.standard
    |> Layout.standard vi pageTitle


/// View for the small group log on page
let logOn (groups : SmallGroup list) grpId ctx vi =
    let s = I18N.localizer.Force ()
    let m = { SmallGroupId = System.Guid.Empty; Password = ""; RememberMe = None }
    [   form [ _action "/small-group/log-on/submit"; _method "post"; _class "pt-center-columns"; Target.body ] [
            csrfToken ctx
            div [ _class "pt-field-row" ] [
                div [ _class "pt-field" ] [
                    label [ _for (nameof m.SmallGroupId) ] [ locStr s["Group"] ]
                    seq {
                        if groups.Length = 0 then "", s["There are no classes with passwords defined"].Value
                        else
                            "", selectDefault s["Select Group"].Value
                            yield!
                                groups
                                |> List.map (fun grp -> flatGuid grp.smallGroupId, $"{grp.church.name} | {grp.name}")
                    }
                    |> selectList (nameof m.SmallGroupId) grpId [ _required ]
                ]
                div [ _class "pt-field" ] [
                    label [ _for "password" ] [ locStr s["Password"] ]
                    input [ _type        "password"
                            _name        (nameof m.Password)
                            _id          "password"
                            _placeholder (s["Case-Sensitive"].Value.ToLower ())
                            _required ]
                ]
            ]
            div [ _class "pt-checkbox-field" ] [
                input [ _type "checkbox"; _name (nameof m.RememberMe); _id "rememberMe"; _value "True" ]
                label [ _for "rememberMe" ] [ locStr s["Remember Me"] ]
                br []
                small [] [ em [] [ str (s["Requires Cookies"].Value.ToLower ()) ] ]
            ]
            div [ _class "pt-field-row" ] [ submit [] "account_circle" s["Log On"] ]
        ]
        script [] [ rawText "PT.onLoad(PT.smallGroup.logOn.onPageLoad)" ]
    ]
    |> Layout.Content.standard
    |> Layout.standard vi "Group Log On"


/// View for the small group maintenance page
let maintain (groups : SmallGroup list) ctx vi =
    let s      = I18N.localizer.Force ()
    let grpTbl =
        match groups with
        | [] -> space
        | _ ->
            table [ _class "pt-table pt-action-table" ] [
                thead [] [
                    tr [] [
                        th [] [ locStr s["Actions"] ]
                        th [] [ locStr s["Name"] ]
                        th [] [ locStr s["Church"] ]
                        th [] [ locStr s["Time Zone"] ]
                    ]
                ]
                groups
                |> List.map (fun g ->
                    let grpId     = flatGuid g.smallGroupId
                    let delAction = $"/small-group/{grpId}/delete"
                    let delPrompt = s["Are you sure you want to delete this {0}?  This action cannot be undone.",
                                         $"""{s["Small Group"].Value.ToLower ()} ({g.name})""" ].Value
                    tr [] [
                        td [] [
                            a [ _href $"/small-group/{grpId}/edit"; _title s["Edit This Group"].Value ] [ icon "edit" ]
                            a [ _href    delAction
                                _title   s["Delete This Group"].Value
                                _onclick $"return PT.confirmDelete('{delAction}','{delPrompt}')" ] [
                                icon "delete_forever"
                            ]
                        ]
                        td [] [ str g.name ]
                        td [] [ str g.church.name ]
                        td [] [ locStr (TimeZones.name g.preferences.timeZoneId s) ]
                    ])
                |> tbody []
            ]
    [   div [ _class "pt-center-text" ] [
            br []
            a [ _href $"/small-group/{emptyGuid}/edit"; _title s["Add a New Group"].Value ] [
                icon "add_circle"; rawText " &nbsp;"; locStr s["Add a New Group"]
            ]
            br []
            br []
        ]
        tableSummary groups.Length s
        grpTbl
        form [ _id "DeleteForm"; _action ""; _method "post" ] [ csrfToken ctx ]
    ]
    |> Layout.Content.standard
    |> Layout.standard vi "Maintain Groups"


/// View for the member maintenance page
let members (members : Member list) (emailTyps : Map<string, LocalizedString>) ctx vi =
    let s      = I18N.localizer.Force ()
    let mbrTbl =
        match members with
        | [] -> space
        | _ ->
            table [ _class "pt-table pt-action-table" ] [
                thead [] [
                    tr [] [
                        th [] [ locStr s["Actions"] ]
                        th [] [ locStr s["Name"] ]
                        th [] [ locStr s["E-mail Address"] ]
                        th [] [ locStr s["Format"] ]
                    ]
                ]
                members
                |> List.map (fun mbr ->
                    let mbrId     = flatGuid mbr.memberId
                    let delAction = $"/small-group/member/{mbrId}/delete"
                    let delPrompt =
                        s["Are you sure you want to delete this {0}?  This action cannot be undone.", s["group member"]]
                            .Value.Replace("?", $" ({mbr.memberName})?")
                    tr [] [
                        td [] [
                            a [ _href $"/small-group/member/{mbrId}/edit"; _title s["Edit This Group Member"].Value ] [
                                icon "edit"
                            ]
                            a [ _href    delAction
                                _title   s["Delete This Group Member"].Value
                                _onclick $"return PT.confirmDelete('{delAction}','{delPrompt}')" ] [
                                icon "delete_forever"
                            ]
                        ]
                        td [] [ str mbr.memberName ]
                        td [] [ str mbr.email ]
                        td [] [ locStr emailTyps[defaultArg mbr.format ""] ]
                    ])
                |> tbody []
            ]
    [   div [ _class"pt-center-text" ] [
            br []
            a [ _href $"/small-group/member/{emptyGuid}/edit"; _title s["Add a New Group Member"].Value ] [
                icon "add_circle"; rawText " &nbsp;"; locStr s["Add a New Group Member"]
            ]
            br []
            br []
        ]
        tableSummary members.Length s
        mbrTbl
        form [ _id "DeleteForm"; _action ""; _method "post" ] [ csrfToken ctx ]
    ]
    |> Layout.Content.standard
    |> Layout.standard vi "Maintain Group Members"


open Giraffe.ViewEngine.Accessibility

/// View for the small group overview page
let overview m vi =
    let s          = I18N.localizer.Force ()
    let linkSpacer = rawText "&nbsp; "
    let types      = ReferenceList.requestTypeList s |> dict
    article [ _class "pt-overview" ] [
        section [ _ariaLabel "Quick actions" ] [
            header [ _roleHeading ] [ iconSized 72 "bookmark_border"; locStr s["Quick Actions"] ]
            div [] [
                a [ _href "/prayer-requests/view" ] [ icon "list"; linkSpacer; locStr s["View Prayer Request List"] ]
                hr []
                a [ _href "/small-group/announcement" ] [ icon "send"; linkSpacer; locStr s["Send Announcement"] ]
                hr []
                a [ _href "/small-group/preferences" ] [ icon "build"; linkSpacer; locStr s["Change Preferences"] ]
            ]
        ]
        section [ _ariaLabel "Prayer requests" ] [
            header [ _roleHeading ] [ iconSized 72 "question_answer"; locStr s["Prayer Requests"] ]
            div [] [
                p [ _class "pt-center-text" ] [
                      strong [] [ str (m.TotalActiveReqs.ToString "N0"); space; locStr s["Active Requests"] ]
                ]
                hr []
                for cat in m.ActiveReqsByType do
                    str (cat.Value.ToString "N0")
                    space
                    locStr types[cat.Key]
                    br []
                br []
                str (m.AllReqs.ToString "N0")
                space
                locStr s["Total Requests"]
                hr []
                a [ _href "/prayer-requests/maintain" ] [
                    icon "compare_arrows"; linkSpacer; locStr s["Maintain Prayer Requests"]
                ]
            ]
        ]
        section [ _ariaLabel "Small group members" ] [
            header [ _roleHeading ] [ iconSized 72 "people_outline"; locStr s["Group Members"] ]
            div [ _class "pt-center-text" ] [
                strong [] [ str (m.TotalMembers.ToString "N0"); space; locStr s["Members"] ]
                hr []
                a [ _href "/small-group/members" ] [ icon "email"; linkSpacer; locStr s["Maintain Group Members"] ]
            ]
        ]
    ]
    |> List.singleton
    |> Layout.Content.wide
    |> Layout.standard vi "Small Group Overview"


open System.IO
open PrayerTracker

/// View for the small group preferences page
let preferences (m : EditPreferences) (tzs : TimeZone list) ctx vi =
    let s   = I18N.localizer.Force ()
    let l   = I18N.forView "SmallGroup/Preferences"
    use sw  = new StringWriter ()
    let raw = rawLocText sw
    [   style [ _scoped ] [
            rawText "#expireDays, #daysToKeepNew, #longTermUpdateWeeks, #headingFontSize, #listFontSize, #pageSize { width: 3rem; } #emailFromAddress { width: 20rem; } #fonts { width: 40rem; } @media screen and (max-width: 40rem) { #fonts { width: 100%; } }"
        ]
        form [ _action "/small-group/preferences/save"; _method "post"; _class "pt-center-columns"; Target.content ] [
            csrfToken ctx
            fieldset [] [
                legend [] [ strong [] [ icon "date_range"; rawText " &nbsp;"; locStr s["Dates"] ] ]
                div [ _class "pt-field-row" ] [
                    div [ _class "pt-field" ] [
                        label [ _for "expireDays" ] [ locStr s["Requests Expire After"] ]
                        span [] [
                            input [ _type  "number"
                                    _name  (nameof m.ExpireDays)
                                    _id    "expireDays"
                                    _value (string m.ExpireDays)
                                    _min   "1"; _max "30"
                                    _required
                                    _autofocus ]
                            space
                            str (s["Days"].Value.ToLower ())
                        ]
                    ]
                    div [ _class "pt-field" ] [
                        label [ _for "daysToKeepNew" ] [ locStr s["Requests “New” For"] ]
                        span [] [
                            input [ _type  "number"
                                    _name  (nameof m.DaysToKeepNew)
                                    _id    "daysToKeepNew"
                                    _min   "1"; _max "30"
                                    _value (string m.DaysToKeepNew)
                                    _required ]
                            space; str (s["Days"].Value.ToLower ())
                        ]
                    ]
                    div [ _class "pt-field" ] [
                        label [ _for "longTermUpdateWeeks" ] [ locStr s["Long-Term Requests Alerted for Update"] ]
                        span [] [
                            input [ _type  "number"
                                    _name  (nameof m.LongTermUpdateWeeks)
                                    _id    "longTermUpdateWeeks"
                                    _min   "1"; _max "30"
                                    _value (string m.LongTermUpdateWeeks)
                                    _required ]
                            space; str (s["Weeks"].Value.ToLower ())
                        ]
                    ]
                ]
            ]
            fieldset [] [
                legend [] [ strong [] [ icon "sort"; rawText " &nbsp;"; locStr s["Request Sorting"] ] ]
                radio (nameof m.RequestSort) "requestSort_D" "D" m.RequestSort
                label [ _for "requestSort_D" ] [ locStr s["Sort by Last Updated Date"] ]
                rawText " &nbsp; "
                radio (nameof m.RequestSort) "requestSort_R" "R" m.RequestSort
                label [ _for "requestSort_R" ] [ locStr s["Sort by Requestor Name"] ]
            ]
            fieldset [] [
                legend [] [ strong [] [ icon "mail_outline"; rawText " &nbsp;"; locStr s["E-mail"] ] ]
                div [ _class "pt-field-row" ] [
                    div [ _class "pt-field" ] [
                        label [ _for "emailFromName" ] [ locStr s["From Name"] ]
                        input [ _type  "text"
                                _name  (nameof m.EmailFromName)
                                _id    "emailFromName"
                                _value m.EmailFromName
                                _required ]
                    ]
                    div [ _class "pt-field" ] [
                        label [ _for "emailFromAddress" ] [ locStr s["From Address"] ]
                        input [ _type  "email"
                                _name  (nameof m.EmailFromAddress)
                                _id    "emailFromAddress"
                                _value m.EmailFromAddress
                                _required ]
                    ]
                ]
                div [ _class "pt-field-row" ] [
                    div [ _class "pt-field" ] [
                        label [ _for (nameof m.DefaultEmailType) ] [ locStr s["E-mail Format"] ]
                        seq {
                            "", selectDefault s["Select"].Value
                            yield!
                                ReferenceList.emailTypeList HtmlFormat s
                                |> Seq.skip 1
                                |> Seq.map (fun typ -> fst typ, (snd typ).Value)
                        }
                        |> selectList (nameof m.DefaultEmailType) m.DefaultEmailType [ _required ]
                    ]
                ]
            ]
            fieldset [] [
                legend [] [ strong [] [ icon "color_lens"; rawText " &nbsp;"; locStr s["Colors"] ]; rawText " ***" ]
                div [ _class "pt-field-row" ] [
                    div [ _class "pt-field" ] [
                        label [ _class "pt-center-text" ] [ locStr s["Color of Heading Lines"] ]
                        span [] [
                            radio (nameof m.LineColorType) "lineColorType_Name" "Name" m.LineColorType
                            label [ _for "lineColorType_Name" ] [ locStr s["Named Color"] ]
                            namedColorList (nameof m.LineColor) m.LineColor [
                                _id "lineColor_Select"
                                if m.LineColor.StartsWith "#" then _disabled ] s
                            rawText "&nbsp; &nbsp; "; str (s["or"].Value.ToUpper ())
                            radio (nameof m.LineColorType) "lineColorType_RGB" "RGB" m.LineColorType
                            label [ _for "lineColorType_RGB" ] [ locStr s["Custom Color"] ]
                            input [ _type  "color" 
                                    _name  (nameof m.LineColor)
                                    _id    "lineColor_Color"
                                    _value m.LineColor // TODO: convert to hex or skip if named
                                    if not (m.LineColor.StartsWith "#") then _disabled ]
                        ]
                    ]
                ]
                div [ _class "pt-field-row" ] [
                    div [ _class "pt-field" ] [
                        label [ _class "pt-center-text" ] [ locStr s["Color of Heading Text"] ]
                        span [] [
                            radio (nameof m.HeadingColorType) "headingColorType_Name" "Name" m.HeadingColorType
                            label [ _for "headingColorType_Name" ] [ locStr s["Named Color"] ]
                            namedColorList (nameof m.HeadingColor) m.HeadingColor [
                                _id "headingColor_Select"
                                if m.HeadingColor.StartsWith "#" then _disabled ] s
                            rawText "&nbsp; &nbsp; "; str (s["or"].Value.ToUpper ())
                            radio (nameof m.HeadingColorType) "headingColorType_RGB" "RGB" m.HeadingColorType
                            label [ _for "headingColorType_RGB" ] [ locStr s["Custom Color"] ]
                            input [ _type  "color"
                                    _name  (nameof m.HeadingColor)
                                    _id    "headingColor_Color"
                                    _value m.HeadingColor // TODO: convert to hex or skip if named
                                    if not (m.HeadingColor.StartsWith "#") then _disabled ]
                        ]
                    ]
                ]
            ]
            fieldset [] [
                legend [] [ strong [] [ icon "font_download"; rawText " &nbsp;"; locStr s["Fonts"] ] ]
                div [ _class "pt-field" ] [
                    label [ _for "fonts" ] [ locStr s["Fonts** for List"] ]
                    input [ _type "text"; _name (nameof m.Fonts); _id "fonts"; _required; _value m.Fonts ]
                ]
                div [ _class "pt-field-row" ] [
                    div [ _class "pt-field" ] [
                        label [ _for "headingFontSize" ] [ locStr s["Heading Text Size"] ]
                        input [ _type  "number"
                                _name  (nameof m.HeadingFontSize)
                                _id    "headingFontSize"
                                _min   "8"; _max "24"
                                _value (string m.HeadingFontSize)
                                _required ]
                    ]
                    div [ _class "pt-field" ] [
                        label [ _for "listFontSize" ] [ locStr s["List Text Size"] ]
                        input [ _type  "number"
                                _name  (nameof m.ListFontSize)
                                _id    "listFontSize"
                                _min   "8"; _max "24"
                                _value (string m.ListFontSize)
                                _required ]
                    ]
                ]
            ]
            fieldset [] [
                legend [] [ strong [] [ icon "settings"; rawText " &nbsp;"; locStr s["Other Settings"] ] ]
                div [ _class "pt-field-row" ] [
                    div [ _class "pt-field" ] [
                        label [ _for (nameof m.TimeZone) ] [ locStr s["Time Zone"] ]
                        seq {
                            "", selectDefault s["Select"].Value
                            yield! tzs |> List.map (fun tz -> tz.timeZoneId, (TimeZones.name tz.timeZoneId s).Value)
                        }
                        |> selectList (nameof m.TimeZone) m.TimeZone [ _required ]
                    ]
                ]
                div [ _class "pt-field" ] [
                    label [] [ locStr s["Request List Visibility"] ]
                    span [] [
                        radio (nameof m.Visibility) "viz_Public" (string RequestVisibility.``public``)
                              (string m.Visibility)
                        label [ _for "viz_Public" ] [ locStr s["Public"] ]
                        rawText " &nbsp;"
                        radio (nameof m.Visibility) "viz_Private" (string RequestVisibility.``private``)
                              (string m.Visibility)
                        label [ _for "viz_Private" ] [ locStr s["Private"] ]
                        rawText " &nbsp;"
                        radio (nameof m.Visibility) "viz_Password" (string RequestVisibility.passwordProtected)
                              (string m.Visibility)
                        label [ _for "viz_Password" ] [ locStr s["Password Protected"] ]
                    ]
                ]
                let classSuffix = if m.Visibility = RequestVisibility.passwordProtected then " pt-show" else ""
                div [ _id "divClassPassword"; _class $"pt-field-row pt-fadeable{classSuffix}" ] [
                    div [ _class "pt-field" ] [
                        label [ _for "groupPassword" ] [ locStr s["Group Password (Used to Read Online)"] ]
                        input [ _type  "text"
                                _name  (nameof m.GroupPassword)
                                _id    "groupPassword"
                                _value (defaultArg m.GroupPassword "") ]
                    ]
                ]
                div [ _class "pt-field-row" ] [
                    div [ _class "pt-field" ] [
                        label [ _for "pageSize" ] [ locStr s["Page Size"] ]
                        input [ _type  "number"
                                _name  (nameof m.PageSize)
                                _id    "pageSize"
                                _min   "10"; _max "255"
                                _value (string m.PageSize)
                                _required ]
                    ]
                    div [ _class "pt-field" ] [
                        label [ _for (nameof m.AsOfDate) ] [ locStr s["“As of” Date Display"] ]
                        ReferenceList.asOfDateList s
                        |> List.map (fun (code, desc) -> code, desc.Value)
                        |> selectList (nameof m.AsOfDate) m.AsOfDate [ _required ]
                    ]
                ]
            ]
            div [ _class "pt-field-row" ] [ submit [] "save" s["Save Preferences"] ]
        ]
        p [] [
            rawText "** "
            raw l["List font names, separated by commas."]
            space
            raw l["The first font that is matched is the one that is used."]
            space
            raw l["Ending with either “serif” or “sans-serif” will cause the user's browser to use the default “serif” font (“Times New Roman” on Windows) or “sans-serif” font (“Arial” on Windows) if no other fonts in the list are found."]
        ]
        p [] [
            rawText "*** "
            raw l["If you want a custom color, you may be able to get some ideas (and a list of RGB values for those colors) from the W3 School's <a href=\"http://www.w3schools.com/html/html_colornames.asp\" title=\"HTML Color List - W3 School\">HTML color name list</a>."]
        ]
        script [] [ rawText "PT.onLoad(PT.smallGroup.preferences.onPageLoad)" ]
    ]
    |> Layout.Content.standard
    |> Layout.standard vi "Group Preferences"
