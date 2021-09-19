module PrayerTracker.Views.User

open Giraffe.ViewEngine
open PrayerTracker.Entities
open PrayerTracker.ViewModels

/// View for the group assignment page
let assignGroups m groups curGroups ctx vi =
  let s         = I18N.localizer.Force ()
  let pageTitle = sprintf "%s • %A" m.userName s.["Assign Groups"]
  form [ _action "/web/user/small-groups/save"; _method "post"; _class "pt-center-columns" ] [
    csrfToken ctx
    input [ _type "hidden"; _name "userId"; _value (flatGuid m.userId) ]
    input [ _type "hidden"; _name "userName"; _value m.userName ]
    table [ _class "pt-table" ] [
      thead [] [
        tr [] [
          th [] [ rawText "&nbsp;" ]
          th [] [ locStr s.["Group"] ]
          ]
        ]
      groups
      |> List.map (fun (grpId, grpName) ->
          let inputId = $"id-{grpId}"
          tr [] [
            td [] [
              input [ _type "checkbox"
                      _name "smallGroups"
                      _id inputId
                      _value grpId
                      match curGroups |> List.contains grpId with true -> _checked | false -> () ]
              ]
            td [] [ label [ _for inputId ] [ str grpName ] ]
            ])
      |> tbody []
      ]
    div [ _class "pt-field-row" ] [ submit [] "save" s.["Save Group Assignments"] ]
    ]
  |> List.singleton
  |> Layout.Content.standard
  |> Layout.standard vi pageTitle


/// View for the password change page
let changePassword ctx vi =
  let s = I18N.localizer.Force ()
  [ p [ _class "pt-center-text" ] [
      locStr s.["To change your password, enter your current password in the specified box below, then enter your new password twice."]
      ]
    form [ _action "/web/user/password/change"
           _method "post"
           _onsubmit $"""return PT.compareValidation('newPassword','newPasswordConfirm','%A{s.["The passwords do not match"]}')""" ] [
      style [ _scoped ] [ rawText "#oldPassword, #newPassword, #newPasswordConfirm { width: 10rem; } "]
      csrfToken ctx
      div [ _class "pt-field-row" ] [
        div [ _class "pt-field" ] [
          label [ _for "oldPassword" ] [ locStr s.["Current Password"] ]
          input [ _type "password"; _name "oldPassword"; _id "oldPassword"; _required; _autofocus ]
          ]
        ]
      div [ _class "pt-field-row" ] [
        div [ _class "pt-field" ] [
          label [ _for "newPassword" ] [ locStr s.["New Password Twice"] ]
          input [ _type "password"; _name "newPassword"; _id "newPassword"; _required ]
          ]
        div [ _class "pt-field" ] [
          label [] [ rawText "&nbsp;" ]
          input [ _type "password"; _name "newPasswordConfirm"; _id "newPasswordConfirm"; _required ]
          ]
        ]
      div [ _class "pt-field-row" ] [
        submit [ _onclick "document.getElementById('newPasswordConfirm').setCustomValidity('')" ] "done"
               s.["Change Your Password"]
        ]
      ]
    ]
  |> Layout.Content.standard
  |> Layout.standard vi "Change Your Password"


/// View for the edit user page
let edit (m : EditUser) ctx vi =
  let s             = I18N.localizer.Force ()
  let pageTitle     = match m.isNew () with true -> "Add a New User" | false -> "Edit User"
  let pwPlaceholder = s.[match m.isNew () with true -> "" | false -> "No change"].Value
  [ form [ _action "/web/user/edit/save"; _method "post"; _class "pt-center-columns"
           _onsubmit $"""return PT.compareValidation('password','passwordConfirm','%A{s.["The passwords do not match"]}')""" ] [
      style [ _scoped ]
        [ rawText "#firstName, #lastName, #password, #passwordConfirm { width: 10rem; } #emailAddress { width: 20rem; } " ]
      csrfToken ctx
      input [ _type "hidden"; _name "userId"; _value (flatGuid m.userId) ]
      div [ _class "pt-field-row" ] [
        div [ _class "pt-field" ] [
          label [ _for "firstName" ] [ locStr s.["First Name"] ]
          input [ _type "text"; _name "firstName"; _id "firstName"; _value m.firstName; _required; _autofocus ]
          ]
        div [ _class "pt-field" ] [
          label [ _for "lastName" ] [ locStr s.["Last Name"] ]
          input [ _type "text"; _name "lastName"; _id "lastName"; _value m.lastName; _required ]
          ]
        div [ _class "pt-field" ] [
          label [ _for "emailAddress" ] [ locStr s.["E-mail Address"] ]
          input [ _type "email"; _name "emailAddress"; _id "emailAddress"; _value m.emailAddress; _required ]
          ]
        ]
      div [ _class "pt-field-row" ] [
        div [ _class "pt-field" ] [
          label [ _for "password" ] [ locStr s.["Password"] ]
          input [ _type "password"; _name "password"; _id "password"; _placeholder pwPlaceholder ]
          ]
        div [ _class "pt-field" ] [
          label [ _for "passwordConfirm" ] [ locStr s.["Password Again"] ]
          input [ _type "password"; _name "passwordConfirm"; _id "passwordConfirm"; _placeholder pwPlaceholder ]
          ]
        ]
      div [ _class "pt-checkbox-field" ] [
        input [ _type "checkbox"
                _name "isAdmin"
                _id "isAdmin"
                _value "True"
                match m.isAdmin with Some x when x -> _checked | _ -> () ]
        label [ _for "isAdmin" ] [ locStr s.["This user is a PrayerTracker administrator"] ]
        ]
      div [ _class "pt-field-row" ] [ submit [] "save" s.["Save User"] ]
      ]
    script [] [ rawText $"PT.onLoad(PT.user.edit.onPageLoad({(string (m.isNew ())).ToLower ()}))" ]
    ]
  |> Layout.Content.standard
  |> Layout.standard vi pageTitle


/// View for the user log on page
let logOn (m : UserLogOn) groups ctx vi =
  let s = I18N.localizer.Force ()
  form [ _action "/web/user/log-on"; _method "post"; _class "pt-center-columns" ] [
    style [ _scoped ] [ rawText "#emailAddress { width: 20rem; }" ]
    csrfToken ctx
    input [ _type "hidden"; _name "redirectUrl"; _value (defaultArg m.redirectUrl "") ]
    div [ _class "pt-field-row" ] [
      div [ _class "pt-field" ] [
        label [ _for "emailAddress"] [ locStr s.["E-mail Address"] ]
        input [ _type "email"; _name "emailAddress"; _id "emailAddress"; _value m.emailAddress; _required; _autofocus ]
        ]
      div [ _class "pt-field" ] [
        label [ _for "password" ] [ locStr s.["Password"] ]
        input [ _type "password"; _name "password"; _id "password"; _required;
                _placeholder (sprintf "(%s)" (s.["Case-Sensitive"].Value.ToLower ())) ]
        ]
      ]
    div [ _class "pt-field-row" ] [
      div [ _class "pt-field" ] [
        label [ _for "smallGroupId" ] [ locStr s.["Group"] ]
        seq {
          "", selectDefault s.["Select Group"].Value
          yield! groups
          }
        |> selectList "smallGroupId" "" [ _required ]
               
        ]
      ]
    div [ _class "pt-checkbox-field" ] [
      input [ _type "checkbox"; _name "rememberMe"; _id "rememberMe"; _value "True" ]
      label [ _for "rememberMe" ] [ locStr s.["Remember Me"] ]
      br []
      small [] [ em [] [ rawText "("; str (s.["Requires Cookies"].Value.ToLower ()); rawText ")" ] ]
      ]
    div [ _class "pt-field-row" ] [ submit [] "account_circle" s.["Log On"] ]
    ]
  |> List.singleton
  |> Layout.Content.standard
  |> Layout.standard vi "User Log On"


/// View for the user maintenance page
let maintain (users : User list) ctx vi =
  let s      = I18N.localizer.Force ()
  let usrTbl =
    match users with
    | [] -> space
    | _ ->
        table [ _class "pt-table pt-action-table" ] [
          thead [] [
            tr [] [
              th [] [ locStr s.["Actions"] ]
              th [] [ locStr s.["Name"] ]
              th [] [ locStr s.["Admin?"] ]
              ]
            ]
          users
          |> List.map (fun user ->
              let userId    = flatGuid user.userId
              let delAction = $"/web/user/{userId}/delete"
              let delPrompt = s.["Are you sure you want to delete this {0}?  This action cannot be undone.",
                                  $"""{s.["User"].Value.ToLower ()} ({user.fullName})"""].Value
              tr [] [
                td [] [
                  a [ _href $"/web/user/{userId}/edit"; _title s.["Edit This User"].Value ] [ icon "edit" ]
                  a [ _href $"/web/user/{userId}/small-groups"; _title s.["Assign Groups to This User"].Value ]
                    [ icon "group" ]
                  a [ _href delAction
                      _title s.["Delete This User"].Value
                      _onclick $"return PT.confirmDelete('{delAction}','{delPrompt}')" ]
                    [ icon "delete_forever" ]
                  ]
                td [] [ str user.fullName ]
                td [ _class "pt-center-text" ] [
                  match user.isAdmin with
                  | true -> strong [] [ locStr s.["Yes"] ]
                  | false -> locStr s.["No"]
                  ]
                ])
          |> tbody []
          ]
  [ div [ _class "pt-center-text" ] [
      br []
      a [ _href $"/web/user/{emptyGuid}/edit"; _title s.["Add a New User"].Value ]
        [ icon "add_circle"; rawText " &nbsp;"; locStr s.["Add a New User"] ]
      br []
      br []
      ]
    tableSummary users.Length s
    usrTbl
    form [ _id "DeleteForm"; _action ""; _method "post" ] [ csrfToken ctx ]
    ]
  |> Layout.Content.standard
  |> Layout.standard vi "Maintain Users"
