module PrayerTracker.Views.User

open Giraffe.ViewEngine
open PrayerTracker
open PrayerTracker.ViewModels

/// View for the group assignment page
let assignGroups model groups curGroups ctx viewInfo =
    let s         = I18N.localizer.Force ()
    let pageTitle = sprintf "%s • %A" model.UserName s["Assign Groups"]
    form [ _action "/user/small-groups/save"; _method "post"; _class "pt-center-columns"; Target.content ] [
        csrfToken ctx
        inputField "hidden" (nameof model.UserId) model.UserId []
        inputField "hidden" (nameof model.UserName) model.UserName []
        table [ _class "pt-table" ] [
            thead [] [
                tr [] [
                    th [] [ rawText "&nbsp;" ]
                    th [] [ locStr s["Group"] ]
                ]
            ]
            groups
            |> List.map (fun (grpId, grpName) ->
                let inputId = $"id-{grpId}"
                tr [] [
                    td [] [
                        input [ _type  "checkbox"
                                _name  (nameof model.SmallGroups)
                                _id    inputId
                                _value grpId
                                if List.contains grpId curGroups then _checked ]
                    ]
                    td [] [ label [ _for inputId ] [ str grpName ] ]
                ])
            |> tbody []
        ]
        div [ _fieldRow ] [ submit [] "save" s["Save Group Assignments"] ]
    ]
    |> List.singleton
    |> Layout.Content.standard
    |> Layout.standard viewInfo pageTitle


/// View for the password change page
let changePassword ctx viewInfo =
    let s    = I18N.localizer.Force ()
    let model = { OldPassword = ""; NewPassword = ""; NewPasswordConfirm = "" }
    let vi    =
        viewInfo
        |> AppViewInfo.withScopedStyles [
            let fields =
                toHtmlIds [ nameof model.OldPassword; nameof model.NewPassword; nameof model.NewPasswordConfirm ]
            $"{fields} {{ width: 10rem; }}"
        ]
    [   p [ _class "pt-center-text" ] [
            locStr s["To change your password, enter your current password in the specified box below, then enter your new password twice."]
        ]
        form [ _action   "/user/password/change"
               _method   "post"
               _onsubmit $"""return PT.compareValidation('{nameof model.NewPassword}','{nameof model.NewPasswordConfirm}','%A{s["The passwords do not match"]}')"""
               Target.content ] [
            csrfToken ctx
            div [ _fieldRow ] [
                div [ _inputField ] [
                    label [ _for (nameof model.OldPassword) ] [ locStr s["Current Password"] ]
                    inputField "password" (nameof model.OldPassword) "" [ _required; _autofocus ]
                ]
            ]
            div [ _fieldRow ] [
                div [ _inputField ] [
                    label [ _for (nameof model.NewPassword) ] [ locStr s["New Password Twice"] ]
                    inputField "password" (nameof model.NewPassword) "" [ _required ]
                ]
                div [ _inputField ] [
                    label [ _for (nameof model.NewPasswordConfirm) ] [ rawText "&nbsp;" ]
                    inputField "password" (nameof model.NewPasswordConfirm) "" [ _required ]
                ]
            ]
            div [ _fieldRow ] [
                submit [
                    _onclick $"document.getElementById('{nameof model.NewPasswordConfirm}').setCustomValidity('')"
                ] "done" s["Change Your Password"]
            ]
        ]
    ]
    |> Layout.Content.standard
    |> Layout.standard vi "Change Your Password"


/// View for the edit user page
let edit (model : EditUser) ctx viewInfo =
    let s             = I18N.localizer.Force ()
    let pageTitle     = if model.IsNew then "Add a New User" else "Edit User"
    let pwPlaceholder = s[if model.IsNew then "" else "No change"].Value
    let vi            =
        viewInfo
        |> AppViewInfo.withScopedStyles [
            let fields =
                [ nameof model.FirstName; nameof model.LastName; nameof model.Password; nameof model.PasswordConfirm ]
                |> toHtmlIds
            $"{fields} {{ width: 10rem; }}"
            $"#{nameof model.Email} {{ width: 20rem; }}"
        ]
        |> AppViewInfo.withOnLoadScript $"PT.user.edit.onPageLoad({(string model.IsNew).ToLowerInvariant ()})"
    form [ _action   "/user/edit/save"
           _method   "post"
           _class    "pt-center-columns"
           _onsubmit $"""return PT.compareValidation('{nameof model.Password}','{nameof model.PasswordConfirm}','%A{s["The passwords do not match"]}')"""
           Target.content ] [
        csrfToken ctx
        inputField "hidden" (nameof model.UserId) model.UserId []
        div [ _fieldRow ] [
            div [ _inputField ] [
                label [ _for (nameof model.FirstName) ] [ locStr s["First Name"] ]
                inputField "text" (nameof model.FirstName) model.FirstName [ _required; _autofocus ]
            ]
            div [ _inputField ] [
                label [ _for (nameof model.LastName) ] [ locStr s["Last Name"] ]
                inputField "text" (nameof model.LastName) model.LastName [ _required ]
            ]
            div [ _inputField ] [
                label [ _for (nameof model.Email) ] [ locStr s["E-mail Address"] ]
                inputField "email" (nameof model.Email) model.Email [ _required ]
            ]
        ]
        div [ _fieldRow ] [
            div [ _inputField ] [
                label [ _for (nameof model.Password) ] [ locStr s["Password"] ]
                inputField "password" (nameof model.Password) "" [ _placeholder pwPlaceholder ]
            ]
            div [ _inputField ] [
                label [ _for "passwordConfirm" ] [ locStr s["Password Again"] ]
                inputField "password" (nameof model.PasswordConfirm) "" [ _placeholder pwPlaceholder ]
            ]
        ]
        div [ _checkboxField ] [
            inputField "checkbox" (nameof model.IsAdmin) "True" [ if defaultArg model.IsAdmin false then _checked ]
            label [ _for (nameof model.IsAdmin) ] [ locStr s["This user is a PrayerTracker administrator"] ]
        ]
        div [ _fieldRow ] [ submit [] "save" s["Save User"] ]
    ]
    |> List.singleton
    |> Layout.Content.standard
    |> Layout.standard vi pageTitle

/// View for the user log on page
let logOn (model : UserLogOn) groups ctx viewInfo =
    let s  = I18N.localizer.Force ()
    let vi = AppViewInfo.withScopedStyles [ $"#{nameof model.Email} {{ width: 20rem; }}" ] viewInfo
    form [ _action "/user/log-on"; _method "post"; _class "pt-center-columns"; Target.body ] [
        csrfToken ctx
        inputField "hidden" (nameof model.RedirectUrl) (defaultArg model.RedirectUrl "") []
        div [ _fieldRow ] [
            div [ _inputField ] [
                label [ _for (nameof model.Email) ] [ locStr s["E-mail Address"] ]
                inputField "email" (nameof model.Email) model.Email [ _required; _autofocus ]
            ]
            div [ _inputField ] [
                label [ _for (nameof model.Password) ] [ locStr s["Password"] ]
                inputField "password" (nameof model.Password) "" [
                    _placeholder $"""({s["Case-Sensitive"].Value.ToLower ()})"""; _required
                ]
            ]
        ]
        div [ _fieldRow ] [
            div [ _inputField ] [
                label [ _for (nameof model.SmallGroupId) ] [ locStr s["Group"] ]
                seq { "", selectDefault s["Select Group"].Value; yield! groups }
                |> selectList (nameof model.SmallGroupId) "" [ _required ]
            ]
        ]
        div [ _checkboxField ] [
            inputField "checkbox" (nameof model.RememberMe) "True" []
            label [ _for "rememberMe" ] [ locStr s["Remember Me"] ]
            br []
            small [] [ em [] [ str $"""({s["Requires Cookies"].Value.ToLower ()})""" ] ]
        ]
        div [ _fieldRow ] [ submit [] "account_circle" s["Log On"] ]
    ]
    |> List.singleton
    |> Layout.Content.standard
    |> Layout.standard vi "User Log On"


open PrayerTracker.Entities

/// View for the user maintenance page
let maintain (users : User list) ctx viewInfo =
    let s      = I18N.localizer.Force ()
    let usrTbl =
        match users with
        | [] -> space
        | _ ->
            table [ _class "pt-table pt-action-table" ] [
                tableHeadings s [ "Actions"; "Name"; "Last Seen"; "Admin?" ]
                users
                |> List.map (fun user ->
                    let userId    = shortGuid user.Id.Value
                    let delAction = $"/user/{userId}/delete"
                    let delPrompt = s["Are you sure you want to delete this {0}?  This action cannot be undone.",
                                      $"""{s["User"].Value.ToLower ()} ({user.Name})"""].Value
                    tr [] [
                        td [] [
                            a [ _href $"/user/{userId}/edit"; _title s["Edit This User"].Value ] [ icon "edit" ]
                            a [ _href $"/user/{userId}/small-groups"; _title s["Assign Groups to This User"].Value ] [
                                icon "group"
                            ]
                            a [ _href     delAction
                                _title   s["Delete This User"].Value
                                _onclick $"return PT.confirmDelete('{delAction}','{delPrompt}')" ] [
                                icon "delete_forever"
                            ]
                        ]
                        td [] [ str user.Name ]
                        td [] [
                            str (match user.LastSeen with Some dt -> dt.ToString s["MMMM d, yyyy"] | None -> "--")
                        ]
                        td [ _class "pt-center-text" ] [
                            if user.IsAdmin then strong [] [ locStr s["Yes"] ] else locStr s["No"]
                        ]
                    ])
              |> tbody []
            ]
    [   div [ _class "pt-center-text" ] [
            br []
            a [ _href $"/user/{emptyGuid}/edit"; _title s["Add a New User"].Value ] [
                icon "add_circle"; rawText " &nbsp;"; locStr s["Add a New User"]
            ]
            br []
            br []
        ]
        tableSummary users.Length s
        usrTbl
        form [ _id "DeleteForm"; _action ""; _method "post" ] [ csrfToken ctx ]
    ]
    |> Layout.Content.standard
    |> Layout.standard viewInfo "Maintain Users"
