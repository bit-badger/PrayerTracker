module PrayerTracker.Views.User

open Giraffe.ViewEngine
open PrayerTracker.ViewModels

/// View for the group assignment page
let assignGroups model groups curGroups ctx viewInfo =
    let s         = I18N.localizer.Force ()
    let pageTitle = sprintf "%s • %A" model.UserName s["Assign Groups"]
    form [ _action "/user/small-groups/save"; _method "post"; _class "pt-center-columns"; Target.content ] [
        csrfToken ctx
        input [ _type "hidden"; _name (nameof model.UserId); _value (flatGuid model.UserId) ]
        input [ _type "hidden"; _name (nameof model.UserName); _value model.UserName ]
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
    let s = I18N.localizer.Force ()
    let model = { OldPassword = ""; NewPassword = ""; NewPasswordConfirm = "" }
    let vi =
        AppViewInfo.withScopedStyles [ "#oldPassword, #newPassword, #newPasswordConfirm { width: 10rem; }"] viewInfo
    p [ _class "pt-center-text" ] [
        locStr s["To change your password, enter your current password in the specified box below, then enter your new password twice."]
    ]
    |> List.singleton
    |> List.append [
        form [ _action   "/user/password/change"
               _method   "post"
               _onsubmit $"""return PT.compareValidation('newPassword','newPasswordConfirm','%A{s["The passwords do not match"]}')"""
               Target.content ] [
            csrfToken ctx
            div [ _fieldRow ] [
                div [ _inputField ] [
                    label [ _for "oldPassword" ] [ locStr s["Current Password"] ]
                    input [ _type "password"
                            _name (nameof model.OldPassword)
                            _id   "oldPassword"
                            _required
                            _autofocus ]
                ]
            ]
            div [ _fieldRow ] [
                div [ _inputField ] [
                    label [ _for "newPassword" ] [ locStr s["New Password Twice"] ]
                    input [ _type "password"; _name (nameof model.NewPassword); _id "newPassword"; _required ]
                ]
                div [ _inputField ] [
                    label [] [ rawText "&nbsp;" ]
                    input [ _type "password"
                            _name (nameof model.NewPasswordConfirm)
                            _id   "newPasswordConfirm"
                            _required ]
                ]
            ]
            div [ _fieldRow ] [
                submit [ _onclick "document.getElementById('newPasswordConfirm').setCustomValidity('')" ] "done"
                       s["Change Your Password"]
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
    let vi =
        viewInfo
        |> AppViewInfo.withScopedStyles [
            "#firstName, #lastName, #password, #passwordConfirm { width: 10rem; }"
            "#email { width: 20rem; }"
        ]
        |> AppViewInfo.withOnLoadScript $"PT.user.edit.onPageLoad({(string model.IsNew).ToLowerInvariant ()})"
    form [ _action   "/user/edit/save"
           _method   "post"
           _class    "pt-center-columns"
           _onsubmit $"""return PT.compareValidation('password','passwordConfirm','%A{s["The passwords do not match"]}')"""
           Target.content ] [
        csrfToken ctx
        input [ _type "hidden"; _name (nameof model.UserId); _value (flatGuid model.UserId) ]
        div [ _fieldRow ] [
            div [ _inputField ] [
                label [ _for "firstName" ] [ locStr s["First Name"] ]
                input [ _type  "text"
                        _name  (nameof model.FirstName)
                        _id    "firstName"
                        _value model.FirstName
                        _required
                        _autofocus ]
            ]
            div [ _inputField ] [
                label [ _for "lastName" ] [ locStr s["Last Name"] ]
                input [ _type "text"; _name (nameof model.LastName); _id "lastName"; _value model.LastName; _required ]
            ]
            div [ _inputField ] [
                label [ _for "email" ] [ locStr s["E-mail Address"] ]
                input [ _type "email"; _name (nameof model.Email); _id "email"; _value model.Email; _required ]
            ]
        ]
        div [ _fieldRow ] [
            div [ _inputField ] [
                label [ _for "password" ] [ locStr s["Password"] ]
                input [ _type "password"; _name (nameof model.Password); _id "password"; _placeholder pwPlaceholder ]
            ]
            div [ _inputField ] [
                label [ _for "passwordConfirm" ] [ locStr s["Password Again"] ]
                input [ _type        "password"
                        _name        (nameof model.PasswordConfirm)
                        _id          "passwordConfirm"
                        _placeholder pwPlaceholder ]
            ]
        ]
        div [ _checkboxField ] [
            input [ _type  "checkbox"
                    _name  (nameof model.IsAdmin)
                    _id    "isAdmin"
                    _value "True"
                    if defaultArg model.IsAdmin false then _checked ]
            label [ _for "isAdmin" ] [ locStr s["This user is a PrayerTracker administrator"] ]
        ]
        div [ _fieldRow ] [ submit [] "save" s["Save User"] ]
    ]
    |> List.singleton
    |> Layout.Content.standard
    |> Layout.standard vi pageTitle

/// View for the user log on page
let logOn (model : UserLogOn) groups ctx viewInfo =
    let s  = I18N.localizer.Force ()
    let vi = AppViewInfo.withScopedStyles [ "#email { width: 20rem; }" ] viewInfo
    form [ _action "/user/log-on"; _method "post"; _class "pt-center-columns"; Target.body ] [
        csrfToken ctx
        input [ _type "hidden"; _name (nameof model.RedirectUrl); _value (defaultArg model.RedirectUrl "") ]
        div [ _fieldRow ] [
            div [ _inputField ] [
                label [ _for "email"] [ locStr s["E-mail Address"] ]
                input [ _type  "email"
                        _name  (nameof model.Email)
                        _id    "email"
                        _value model.Email
                        _required
                        _autofocus ]
            ]
            div [ _inputField ] [
                label [ _for "password" ] [ locStr s["Password"] ]
                input [ _type        "password"
                        _name        (nameof model.Password)
                        _id          "password"
                        _placeholder $"""({s["Case-Sensitive"].Value.ToLower ()})"""
                        _required ]
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
            input [ _type "checkbox"; _name (nameof model.RememberMe); _id "rememberMe"; _value "True" ]
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
                thead [] [
                    tr [] [
                        th [] [ locStr s["Actions"] ]
                        th [] [ locStr s["Name"] ]
                        th [] [ locStr s["Admin?"] ]
                    ]
                ]
                users
                |> List.map (fun user ->
                    let userId    = flatGuid user.userId
                    let delAction = $"/user/{userId}/delete"
                    let delPrompt = s["Are you sure you want to delete this {0}?  This action cannot be undone.",
                                      $"""{s["User"].Value.ToLower ()} ({user.fullName})"""].Value
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
                        td [] [ str user.fullName ]
                        td [ _class "pt-center-text" ] [
                            if user.isAdmin then strong [] [ locStr s["Yes"] ] else locStr s["No"]
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
