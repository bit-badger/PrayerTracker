module PrayerTracker.Views.SmallGroup

open Giraffe.ViewEngine
open Giraffe.ViewEngine.Accessibility
open Giraffe.ViewEngine.Htmx
open Microsoft.Extensions.Localization
open PrayerTracker
open PrayerTracker.Entities
open PrayerTracker.ViewModels

/// View for the announcement page
let announcement isAdmin ctx viewInfo =
    let s        = I18N.localizer.Force ()
    let model    = { SendToClass = ""; Text = ""; AddToRequestList = None; RequestType = None }
    let reqTypes = ReferenceList.requestTypeList s
    let vi       = AppViewInfo.withOnLoadScript "PT.smallGroup.announcement.onPageLoad" viewInfo
    form [ _action "/small-group/announcement/send"; _method "post"; _class "pt-center-columns"; Target.content ] [
        csrfToken ctx
        div [ _fieldRow ] [
            div [ _inputFieldWith [ "pt-editor" ] ] [
                label [ _for (nameof model.Text) ] [ locStr s["Announcement Text"] ]
                textarea [ _name (nameof model.Text); _id (nameof model.Text); _autofocus ] []
            ]
        ]
        if isAdmin then
            div [ _fieldRow ] [
                div [ _inputField ] [
                    label [] [ locStr s["Send Announcement to"]; rawText ":" ]
                    div [ _class "pt-center-text" ] [
                        radio (nameof model.SendToClass) $"{nameof model.SendToClass}_Y" "Y" "Y"
                        label [ _for $"{nameof model.SendToClass}_Y" ] [
                            locStr s["This Group"]; rawText " &nbsp; &nbsp; "
                        ]
                        radio (nameof model.SendToClass) $"{nameof model.SendToClass}_N" "N" "Y"
                        label [ _for $"{nameof model.SendToClass}_N" ] [ locStr s["All {0} Users", s["PrayerTracker"]] ]
                    ]
                ]
            ]
        else input [ _type "hidden"; _name (nameof model.SendToClass); _value "Y" ]
        div [ _fieldRowWith [ "pt-fadeable"; "pt-shown" ]; _id "divAddToList" ] [
            div [ _checkboxField ] [
                inputField "checkbox" (nameof model.AddToRequestList) "True" []
                label [ _for (nameof model.AddToRequestList) ] [ locStr s["Add to Request List"] ]
            ]
        ]
        div [ _fieldRowWith [ "pt-fadeable" ]; _id "divCategory" ] [
            div [ _inputField ] [
                label [ _for (nameof model.RequestType) ] [ locStr s["Request Type"] ]
                reqTypes
                |> Seq.ofList
                |> Seq.map (fun (typ, desc) -> PrayerRequestType.toCode typ, desc.Value)
                |> selectList (nameof model.RequestType) (PrayerRequestType.toCode Announcement) []
            ]
        ]
        div [ _fieldRow ] [ submit [] "send" s["Send Announcement"] ]
    ]
    |> List.singleton
    |> Layout.Content.standard
    |> Layout.standard vi "Send Announcement"


/// View for once an announcement has been sent
let announcementSent (model : Announcement) viewInfo =
    let s = I18N.localizer.Force ()
    [   span [ _class "pt-email-heading" ] [ locStr s["HTML Format"]; rawText ":" ]
        div [ _class "pt-email-canvas" ] [ rawText model.Text ]
        br []
        br []
        span [ _class "pt-email-heading" ] [ locStr s["Plain-Text Format"]; rawText ":" ]
        div [ _class "pt-email-canvas" ] [ pre [] [ str model.PlainText ] ]
    ]
    |> Layout.Content.standard
    |> Layout.standard viewInfo "Announcement Sent"


/// View for the small group add/edit page
let edit (model : EditSmallGroup) (churches : Church list) ctx viewInfo =
    let s         = I18N.localizer.Force ()
    let pageTitle = if model.IsNew then "Add a New Group" else "Edit Group"
    form [ _action "/small-group/save"; _method "post"; _class "pt-center-columns"; Target.content ] [
        csrfToken ctx
        inputField "hidden" (nameof model.SmallGroupId) model.SmallGroupId []
        div [ _fieldRow ] [
            div [ _inputField ] [
                label [ _for (nameof model.Name) ] [ locStr s["Group Name"] ]
                inputField "text" (nameof model.Name) model.Name [ _required; _autofocus ]
            ]
        ]
        div [ _fieldRow ] [
            div [ _inputField ] [
                label [ _for (nameof model.ChurchId) ] [ locStr s["Church"] ]
                seq {
                    "", selectDefault s["Select Church"].Value
                    yield! churches |> List.map (fun c -> shortGuid c.Id.Value, c.Name)
                }
                |> selectList (nameof model.ChurchId) model.ChurchId [ _required ] 
            ]
        ]
        div [ _fieldRow ] [ submit [] "save" s["Save Group"] ]
    ]
    |> List.singleton
    |> Layout.Content.standard
    |> Layout.standard viewInfo pageTitle


/// View for the member edit page
let editMember (model : EditMember) (types : (string * LocalizedString) seq) ctx viewInfo =
    let s         = I18N.localizer.Force ()
    let pageTitle = if model.IsNew then "Add a New Group Member" else "Edit Group Member"
    let vi        =
        AppViewInfo.withScopedStyles [
            $"#{nameof model.Name} {{ width: 15rem; }}"
            $"#{nameof model.Email} {{ width: 20rem; }}"
        ] viewInfo
    form [ _action "/small-group/member/save"; _method "post"; _class "pt-center-columns"; Target.content ] [
        csrfToken ctx
        inputField "hidden" (nameof model.MemberId) model.MemberId []
        div [ _fieldRow ] [
            div [ _inputField ] [
                label [ _for (nameof model.Name) ] [ locStr s["Member Name"] ]
                inputField "text" (nameof model.Name) model.Name [ _required; _autofocus ]
            ]
            div [ _inputField ] [
                label [ _for (nameof model.Email) ] [ locStr s["E-mail Address"] ]
                inputField "email" (nameof model.Email) model.Email [ _required ]
            ]
        ]
        div [ _fieldRow ] [
            div [ _inputField ] [
                label [ _for (nameof model.Format) ] [ locStr s["E-mail Format"] ]
                types
                |> Seq.map (fun typ -> fst typ, (snd typ).Value)
                |> selectList (nameof model.Format) model.Format []
            ]
        ]
        div [ _fieldRow ] [ submit [] "save" s["Save"] ]
    ]
    |> List.singleton
    |> Layout.Content.standard
    |> Layout.standard vi pageTitle


/// View for the small group log on page
let logOn (groups : (string * string) list) grpId ctx viewInfo =
    let s     = I18N.localizer.Force ()
    let model = { SmallGroupId = emptyGuid; Password = ""; RememberMe = None }
    let vi    = AppViewInfo.withOnLoadScript "PT.smallGroup.logOn.onPageLoad" viewInfo
    form [ _action "/small-group/log-on/submit"; _method "post"; _class "pt-center-columns"; Target.body ] [
        csrfToken ctx
        div [ _fieldRow ] [
            div [ _inputField ] [
                label [ _for (nameof model.SmallGroupId) ] [ locStr s["Group"] ]
                seq {
                    if groups.Length = 0 then "", s["There are no classes with passwords defined"].Value
                    else
                        "", selectDefault s["Select Group"].Value
                        yield! groups
                }
                |> selectList (nameof model.SmallGroupId) grpId [ _required ]
            ]
            div [ _inputField ] [
                label [ _for (nameof model.Password) ] [ locStr s["Password"] ]
                inputField "password" (nameof model.Password) ""
                           [ _placeholder (s["Case-Sensitive"].Value.ToLower ()); _required ]
            ]
        ]
        div [ _checkboxField ] [
            inputField "checkbox" (nameof model.RememberMe) "True" []
            label [ _for (nameof model.RememberMe) ] [ locStr s["Remember Me"] ]
            br []
            small [] [ em [] [ str (s["Requires Cookies"].Value.ToLower ()) ] ]
        ]
        div [ _fieldRow ] [ submit [] "account_circle" s["Log On"] ]
    ]
    |> List.singleton
    |> Layout.Content.standard
    |> Layout.standard vi "Group Log On"


/// View for the small group maintenance page
let maintain (groups : SmallGroupInfo list) ctx viewInfo =
    let s  = I18N.localizer.Force ()
    let vi = AppViewInfo.withScopedStyles [ "#groupList { grid-template-columns: repeat(4, auto); }" ] viewInfo
    let groupTable =
        match groups with
        | [] -> space
        | _ ->
            section [ _id "groupList"; _class "pt-table"; _ariaLabel "Small group list" ] [
                div [ _class "row head" ] [
                    header [ _class "cell" ] [ locStr s["Actions"] ]
                    header [ _class "cell" ] [ locStr s["Name"] ]
                    header [ _class "cell" ] [ locStr s["Church"] ]
                    header [ _class "cell" ] [ locStr s["Time Zone"] ]
                ]
                for group in groups do
                    let delAction = $"/small-group/{group.Id}/delete"
                    let delPrompt = s["Are you sure you want to delete this {0}?  This action cannot be undone.",
                                         $"""{s["Small Group"].Value.ToLower ()} ({group.Name})""" ].Value
                    div [ _class "row" ] [
                        div [ _class "cell actions" ] [
                            a [ _href $"/small-group/{group.Id}/edit"; _title s["Edit This Group"].Value ] [
                                iconSized 18 "edit"
                            ]
                            a [ _href      delAction
                                _title     s["Delete This Group"].Value
                                _hxDelete  delAction
                                _hxConfirm delPrompt ] [
                                iconSized 18 "delete_forever"
                            ]
                        ]
                        div [ _class "cell" ] [ str group.Name ]
                        div [ _class "cell" ] [ str group.ChurchName ]
                        div [ _class "cell" ] [ locStr (TimeZones.name group.TimeZoneId s) ]
                    ]
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
        form [ _method "post" ] [
            csrfToken ctx
            groupTable
        ]
    ]
    |> Layout.Content.standard
    |> Layout.standard vi "Maintain Groups"


/// View for the member maintenance page
let members (members : Member list) (emailTypes : Map<string, LocalizedString>) ctx viewInfo =
    let s  = I18N.localizer.Force ()
    let vi = AppViewInfo.withScopedStyles [ "#memberList { grid-template-columns: repeat(4, auto); }" ] viewInfo
    let memberTable =
        match members with
        | [] -> space
        | _ ->
            section [ _id "memberList"; _class "pt-table"; _ariaLabel "Small group member list" ] [
                div [ _class "row head" ] [
                    header [ _class "cell"] [ locStr s["Actions"] ]
                    header [ _class "cell"] [ locStr s["Name"] ]
                    header [ _class "cell"] [ locStr s["E-mail Address"] ]
                    header [ _class "cell"] [ locStr s["Format"] ]
                ]
                for mbr in members do
                    let mbrId     = shortGuid mbr.Id.Value
                    let delAction = $"/small-group/member/{mbrId}/delete"
                    let delPrompt =
                        s["Are you sure you want to delete this {0}?  This action cannot be undone.", s["group member"]]
                            .Value.Replace("?", $" ({mbr.Name})?")
                    div [ _class "row" ] [
                        div [ _class "cell actions" ] [
                            a [ _href $"/small-group/member/{mbrId}/edit"; _title s["Edit This Group Member"].Value ] [
                                iconSized 18 "edit"
                            ]
                            a [ _href      delAction
                                _title     s["Delete This Group Member"].Value
                                _hxPost    delAction
                                _hxConfirm delPrompt ] [
                                iconSized 18 "delete_forever"
                            ]
                        ]
                        div [ _class "cell" ] [ str mbr.Name ]
                        div [ _class "cell" ] [ str mbr.Email ]
                        div [ _class "cell" ] [
                            locStr emailTypes[defaultArg (mbr.Format |> Option.map EmailFormat.toCode) ""]
                        ]
                    ]
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
        form [ _method "post" ] [
            csrfToken ctx
            memberTable
        ]
    ]
    |> Layout.Content.standard
    |> Layout.standard vi "Maintain Group Members"


/// View for the small group overview page
let overview model viewInfo =
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
                      strong [] [ str (model.TotalActiveReqs.ToString "N0"); space; locStr s["Active Requests"] ]
                ]
                hr []
                for cat in model.ActiveReqsByType do
                    str (cat.Value.ToString "N0")
                    space
                    locStr types[cat.Key]
                    br []
                br []
                str (model.AllReqs.ToString "N0")
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
                strong [] [ str (model.TotalMembers.ToString "N0"); space; locStr s["Members"] ]
                hr []
                a [ _href "/small-group/members" ] [ icon "email"; linkSpacer; locStr s["Maintain Group Members"] ]
                hr []
                strong [] [ str ((List.length model.Admins).ToString "N0"); space; locStr s["Administrators"] ]
                for admin in model.Admins do
                    hr []
                    str admin.Name
                    br []
                    small [] [ a [ _href $"mailto:{admin.Email}" ] [ str admin.Email ] ]
            ]
        ]
    ]
    |> List.singleton
    |> Layout.Content.wide
    |> Layout.standard viewInfo "Small Group Overview"


open System.IO

/// View for the small group preferences page
let preferences (model : EditPreferences) ctx viewInfo =
    let s   = I18N.localizer.Force ()
    let l   = I18N.forView "SmallGroup/Preferences"
    use sw  = new StringWriter ()
    let raw = rawLocText sw
    let vi  =
        viewInfo
        |> AppViewInfo.withScopedStyles [
            let numberFields =
                [   nameof model.ExpireDays;      nameof model.DaysToKeepNew; nameof model.LongTermUpdateWeeks
                    nameof model.HeadingFontSize; nameof model.ListFontSize;  nameof model.PageSize
                ]
                |> toHtmlIds
            let fontsId = $"#{nameof model.Fonts}"
            $"{numberFields} {{ width: 3rem; }}"
            $"#{nameof model.EmailFromAddress} {{ width: 20rem; }}"
            $"{fontsId} {{ width: 40rem; }}"
            $"@media screen and (max-width: 40rem) {{ {fontsId} {{ width: 100%%; }} }}"
        ]
        |> AppViewInfo.withOnLoadScript "PT.smallGroup.preferences.onPageLoad"
    [   form [ _action "/small-group/preferences/save"; _method "post"; _class "pt-center-columns"; Target.content ] [
            csrfToken ctx
            fieldset [] [
                legend [] [ strong [] [ icon "date_range"; rawText " &nbsp;"; locStr s["Dates"] ] ]
                div [ _fieldRow ] [
                    div [ _inputField ] [
                        label [ _for (nameof model.ExpireDays) ] [ locStr s["Requests Expire After"] ]
                        span [] [
                            inputField "number" (nameof model.ExpireDays) (string model.ExpireDays) [
                                _min "1"; _max "30"; _required; _autofocus
                            ]
                            space
                            str (s["Days"].Value.ToLower ())
                        ]
                    ]
                    div [ _inputField ] [
                        label [ _for (nameof model.DaysToKeepNew) ] [ locStr s["Requests “New” For"] ]
                        span [] [
                            inputField "number" (nameof model.DaysToKeepNew) (string model.DaysToKeepNew) [
                                _min "1"; _max "30"; _required
                            ]
                            space; str (s["Days"].Value.ToLower ())
                        ]
                    ]
                    div [ _inputField ] [
                        label [ _for (nameof model.LongTermUpdateWeeks) ] [
                            locStr s["Long-Term Requests Alerted for Update"]
                        ]
                        span [] [
                            inputField "number" (nameof model.LongTermUpdateWeeks) (string model.LongTermUpdateWeeks) [
                                _min "1"; _max "30"; _required
                            ]
                            space; str (s["Weeks"].Value.ToLower ())
                        ]
                    ]
                ]
            ]
            fieldset [] [
                legend [] [ strong [] [ icon "sort"; rawText " &nbsp;"; locStr s["Request Sorting"] ] ]
                radio (nameof model.RequestSort) $"{nameof model.RequestSort}_D" "D" model.RequestSort
                label [ _for $"{nameof model.RequestSort}_D" ] [ locStr s["Sort by Last Updated Date"] ]
                rawText " &nbsp; "
                radio (nameof model.RequestSort) $"{nameof model.RequestSort}_R" "R" model.RequestSort
                label [ _for $"{nameof model.RequestSort}_R" ] [ locStr s["Sort by Requestor Name"] ]
            ]
            fieldset [] [
                legend [] [ strong [] [ icon "mail_outline"; rawText " &nbsp;"; locStr s["E-mail"] ] ]
                div [ _fieldRow ] [
                    div [ _inputField ] [
                        label [ _for (nameof model.EmailFromName) ] [ locStr s["From Name"] ]
                        inputField "text" (nameof model.EmailFromName) model.EmailFromName [ _required ]
                    ]
                    div [ _inputField ] [
                        label [ _for (nameof model.EmailFromAddress) ] [ locStr s["From Address"] ]
                        inputField "email" (nameof model.EmailFromAddress) model.EmailFromAddress [ _required ]
                    ]
                ]
                div [ _fieldRow ] [
                    div [ _inputField ] [
                        label [ _for (nameof model.DefaultEmailType) ] [ locStr s["E-mail Format"] ]
                        seq {
                            "", selectDefault s["Select"].Value
                            yield!
                                ReferenceList.emailTypeList HtmlFormat s
                                |> Seq.skip 1
                                |> Seq.map (fun typ -> fst typ, (snd typ).Value)
                        }
                        |> selectList (nameof model.DefaultEmailType) model.DefaultEmailType [ _required ]
                    ]
                ]
            ]
            fieldset [] [
                legend [] [ strong [] [ icon "color_lens"; rawText " &nbsp;"; locStr s["Colors"] ]; rawText " ***" ]
                div [ _fieldRow ] [
                    div [ _inputField ] [
                        label [ _class "pt-center-text" ] [ locStr s["Color of Heading Lines"] ]
                        span [] [
                            radio (nameof model.LineColorType) $"{nameof model.LineColorType}_Name" "Name"
                                  model.LineColorType
                            label [ _for $"{nameof model.LineColorType}_Name" ] [ locStr s["Named Color"] ]
                            namedColorList (nameof model.LineColor) model.LineColor [
                                _id $"{nameof model.LineColor}_Select"
                                if model.LineColor.StartsWith "#" then _disabled ] s
                            rawText "&nbsp; &nbsp; "; str (s["or"].Value.ToUpper ())
                            radio (nameof model.LineColorType) $"{nameof model.LineColorType}_RGB" "RGB"
                                   model.LineColorType
                            label [ _for $"{nameof model.LineColorType}_RGB" ] [ locStr s["Custom Color"] ]
                            input [ _type  "color" 
                                    _name  (nameof model.LineColor)
                                    _id    $"{nameof model.LineColor}_Color"
                                    _value model.LineColor // TODO: convert to hex or skip if named
                                    if not (model.LineColor.StartsWith "#") then _disabled ]
                        ]
                    ]
                ]
                div [ _fieldRow ] [
                    div [ _inputField ] [
                        label [ _class "pt-center-text" ] [ locStr s["Color of Heading Text"] ]
                        span [] [
                            radio (nameof model.HeadingColorType) $"{nameof model.HeadingColorType}_Name" "Name"
                                  model.HeadingColorType
                            label [ _for $"{nameof model.HeadingColorType}_Name" ] [ locStr s["Named Color"] ]
                            namedColorList (nameof model.HeadingColor) model.HeadingColor [
                                _id $"{nameof model.HeadingColor}_Select"
                                if model.HeadingColor.StartsWith "#" then _disabled ] s
                            rawText "&nbsp; &nbsp; "; str (s["or"].Value.ToUpper ())
                            radio (nameof model.HeadingColorType) $"{nameof model.HeadingColorType}_RGB" "RGB"
                                  model.HeadingColorType
                            label [ _for $"{nameof model.HeadingColorType}_RGB" ] [ locStr s["Custom Color"] ]
                            input [ _type  "color"
                                    _name  (nameof model.HeadingColor)
                                    _id    $"{nameof model.HeadingColor}_Color"
                                    _value model.HeadingColor // TODO: convert to hex or skip if named
                                    if not (model.HeadingColor.StartsWith "#") then _disabled ]
                        ]
                    ]
                ]
            ]
            fieldset [] [
                legend [] [ strong [] [ icon "font_download"; rawText " &nbsp;"; locStr s["Fonts"] ] ]
                div [ _inputField ] [
                    label [ _for (nameof model.Fonts) ] [ locStr s["Fonts** for List"] ]
                    inputField "text" (nameof model.Fonts) model.Fonts [ _required ]
                ]
                div [ _fieldRow ] [
                    div [ _inputField ] [
                        label [ _for (nameof model.HeadingFontSize) ] [ locStr s["Heading Text Size"] ]
                        inputField "number" (nameof model.HeadingFontSize) (string model.HeadingFontSize) [
                            _min "8"; _max "24"; _required
                        ]
                    ]
                    div [ _inputField ] [
                        label [ _for (nameof model.ListFontSize) ] [ locStr s["List Text Size"] ]
                        inputField "number" (nameof model.ListFontSize) (string model.ListFontSize) [
                            _min "8"; _max "24"; _required
                        ]
                    ]
                ]
            ]
            fieldset [] [
                legend [] [ strong [] [ icon "settings"; rawText " &nbsp;"; locStr s["Other Settings"] ] ]
                div [ _fieldRow ] [
                    div [ _inputField ] [
                        label [ _for (nameof model.TimeZone) ] [ locStr s["Time Zone"] ]
                        seq {
                            "", selectDefault s["Select"].Value
                            yield!
                                TimeZones.all
                                |> List.map (fun tz -> TimeZoneId.toString tz, (TimeZones.name tz s).Value)
                        }
                        |> selectList (nameof model.TimeZone) model.TimeZone [ _required ]
                    ]
                ]
                div [ _inputField ] [
                    label [] [ locStr s["Request List Visibility"] ]
                    span [] [
                        let name  = nameof model.Visibility
                        let value = string model.Visibility
                        radio name $"{name}_Public" (string GroupVisibility.PublicList) value
                        label [ _for $"{name}_Public" ] [ locStr s["Public"] ]
                        rawText " &nbsp;"
                        radio name $"{name}_Private" (string GroupVisibility.PrivateList) value
                        label [ _for $"{name}_Private" ] [ locStr s["Private"] ]
                        rawText " &nbsp;"
                        radio name $"{name}_Password" (string GroupVisibility.HasPassword) value
                        label [ _for $"{name}_Password" ] [ locStr s["Password Protected"] ]
                    ]
                ]
                let classSuffix = if model.Visibility = GroupVisibility.HasPassword then [ "pt-show" ] else []
                div [ _id "divClassPassword"; _fieldRowWith ("pt-fadeable" :: classSuffix) ] [
                    div [ _inputField ] [
                        label [ _for (nameof model.GroupPassword) ] [ locStr s["Group Password (Used to Read Online)"] ]
                        inputField "text" (nameof model.GroupPassword) (defaultArg model.GroupPassword "") []
                    ]
                ]
                div [ _fieldRow ] [
                    div [ _inputField ] [
                        label [ _for (nameof model.PageSize) ] [ locStr s["Page Size"] ]
                        inputField "number" (nameof model.PageSize) (string model.PageSize) [
                            _min "10"; _max "255"; _required
                        ]
                    ]
                    div [ _inputField ] [
                        label [ _for (nameof model.AsOfDate) ] [ locStr s["“As of” Date Display"] ]
                        ReferenceList.asOfDateList s
                        |> List.map (fun (code, desc) -> code, desc.Value)
                        |> selectList (nameof model.AsOfDate) model.AsOfDate [ _required ]
                    ]
                ]
            ]
            div [ _fieldRow ] [ submit [] "save" s["Save Preferences"] ]
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
    ]
    |> Layout.Content.standard
    |> Layout.standard vi "Group Preferences"
