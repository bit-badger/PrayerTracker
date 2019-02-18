module PrayerTracker.Views.User

open Giraffe.GiraffeViewEngine
open PrayerTracker.Entities
open PrayerTracker.ViewModels

/// View for the group assignment page
let assignGroups m (groups : Map<string, string>) (curGroups : string list) ctx vi =
  let s         = I18N.localizer.Force ()
  let pageTitle = sprintf "%s • %A" m.userName s.["Assign Groups"]
  form [ _action "/user/small-groups/save"; _method "post"; _class "pt-center-columns" ] [
    csrfToken ctx
    input [ _type "hidden"; _name "userId"; _value (m.userId.ToString "N") ]
    input [ _type "hidden"; _name "userName"; _value m.userName ]
    table [ _class "pt-table" ] [
      thead [] [
        tr [] [
          th [] [ rawText "&nbsp;" ]
          th [] [ encLocText s.["Group"] ]
          ]
        ]
      groups
      |> Seq.map (fun grp ->
          let inputId = sprintf "id-%s" grp.Key
          tr [] [
            td [] [
              input [ yield _type "checkbox"
                      yield _name "smallGroups"
                      yield _id inputId
                      yield _value grp.Key
                      match curGroups |> List.contains grp.Key with true -> yield _checked | false -> () ]
              ]
            td [] [ label [ _for inputId ] [ encodedText grp.Value ] ]
            ])
      |> List.ofSeq
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
      encLocText s.["To change your password, enter your current password in the specified box below, then enter your new password twice."]
      ]
    form [ _action "/user/password/change"
           _method "post"
           _onsubmit (sprintf "return PT.compareValidation('newPassword','newPasswordConfirm','%A')" s.["The passwords do not match"]) ] [
      style [ _scoped ] [ rawText "#oldPassword, #newPassword, #newPasswordConfirm { width: 10rem; } "]
      csrfToken ctx
      div [ _class "pt-field-row" ] [
        div [ _class "pt-field" ] [
          label [ _for "oldPassword" ] [ encLocText s.["Current Password"] ]
          input [ _type "password"; _name "oldPassword"; _id "oldPassword"; _required; _autofocus ]
          ]
        ]
      div [ _class "pt-field-row" ] [
        div [ _class "pt-field" ] [
          label [ _for "newPassword" ] [ encLocText s.["New Password Twice"] ]
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
  [ form [ _action "/user/edit/save"; _method "post"; _class "pt-center-columns"
           _onsubmit (sprintf "return PT.compareValidation('password','passwordConfirm','%A')" s.["The passwords do not match"]) ] [
      style [ _scoped ]
        [ rawText "#firstName, #lastName, #password, #passwordConfirm { width: 10rem; } #emailAddress { width: 20rem; } " ]
      csrfToken ctx
      input [ _type "hidden"; _name "userId"; _value (m.userId.ToString "N") ]
      div [ _class "pt-field-row" ] [
        div [ _class "pt-field" ] [
          label [ _for "firstName" ] [ encLocText s.["First Name"] ]
          input [ _type "text"; _name "firstName"; _id "firstName"; _value m.firstName; _required; _autofocus ]
          ]
        div [ _class "pt-field" ] [
          label [ _for "lastName" ] [ encLocText s.["Last Name"] ]
          input [ _type "text"; _name "lastName"; _id "lastName"; _value m.lastName; _required ]
          ]
        div [ _class "pt-field" ] [
          label [ _for "emailAddress" ] [ encLocText s.["E-mail Address"] ]
          input [ _type "email"; _name "emailAddress"; _id "emailAddress"; _value m.emailAddress; _required ]
          ]
        ]
      div [ _class "pt-field-row" ] [
        div [ _class "pt-field" ] [
          label [ _for "password" ] [ encLocText s.["Password"] ]
          input [ _type "password"; _name "password"; _id "password"; _placeholder pwPlaceholder ]
          ]
        div [ _class "pt-field" ] [
          label [ _for "passwordConfirm" ] [ encLocText s.["Password Again"] ]
          input [ _type "password"; _name "passwordConfirm"; _id "passwordConfirm"; _placeholder pwPlaceholder ]
          ]
        ]
      div [ _class "pt-checkbox-field" ] [
        input [ yield _type "checkbox"
                yield _name "isAdmin"
                yield _id "isAdmin"
                yield _value "True"
                match m.isAdmin with Some x when x -> yield _checked | _ -> () ]
        label [ _for "isAdmin" ] [ encLocText s.["This user is a PrayerTracker administrator"] ]
        ]
      div [ _class "pt-field-row" ] [ submit [] "save" s.["Save User"] ]
      ]
    script [] [ rawText (sprintf "PT.onLoad(PT.user.edit.onPageLoad(%s))" ((string (m.isNew ())).ToLower ())) ]
    ]
  |> Layout.Content.standard
  |> Layout.standard vi pageTitle


/// View for the user log on page
let logOn (m : UserLogOn) (groups : Map<string, string>) ctx vi =
  let s = I18N.localizer.Force ()
  form [ _action "/user/log-on"; _method "post"; _class "pt-center-columns" ] [
    style [ _scoped ] [ rawText "#emailAddress { width: 20rem; }" ]
    csrfToken ctx
    input [ _type "hidden"; _name "redirectUrl"; _value (defaultArg m.redirectUrl "") ]
    div [ _class "pt-field-row" ] [
      div [ _class "pt-field" ] [
        label [ _for "emailAddress"] [ encLocText s.["E-mail Address"] ]
        input [ _type "email"; _name "emailAddress"; _id "emailAddress"; _value m.emailAddress; _required; _autofocus ]
        ]
      div [ _class "pt-field" ] [
        label [ _for "password" ] [ encLocText s.["Password"] ]
        input [ _type "password"; _name "password"; _id "password"; _required;
                _placeholder (sprintf "(%s)" (s.["Case-Sensitive"].Value.ToLower ())) ]
        ]
      ]
    div [ _class "pt-field-row" ] [
      div [ _class "pt-field" ] [
        label [ _for "smallGroupId" ] [ encLocText s.["Group"] ]
        seq {
          yield "", selectDefault s.["Select Group"].Value
          yield! groups |> Seq.sortBy (fun x -> x.Value) |> Seq.map (fun x -> x.Key, x.Value)
          }
        |> selectList "smallGroupId" "" [ _required ]
               
        ]
      ]
    div [ _class "pt-checkbox-field" ] [
      input [ _type "checkbox"; _name "rememberMe"; _id "rememberMe"; _value "True" ]
      label [ _for "rememberMe" ] [ encLocText s.["Remember Me"] ]
      br []
      small [] [ em [] [ rawText "("; encodedText (s.["Requires Cookies"].Value.ToLower ()); rawText ")" ] ]
      ]
    div [ _class "pt-field-row" ] [ submit [] "account_circle" s.["Log On"] ]
    ]
  |> List.singleton
  |> Layout.Content.standard
  |> Layout.standard vi "User Log On"


/// View for the user maintenance page
let maintain (users : User list) ctx vi =
  let s = I18N.localizer.Force ()
  [ div [ _class "pt-center-text" ] [
      br []
      a [ _href (sprintf "/user/%s/edit" emptyGuid); _title s.["Add a New User"].Value ]
        [ icon "add_circle"; rawText " &nbsp;"; encLocText s.["Add a New User"] ]
      br []
      br []
      ]
    tableSummary users.Length s
    table [ _class "pt-table pt-action-table" ] [
      thead [] [
        tr [] [
          th [] [ encLocText s.["Actions"] ]
          th [] [ encLocText s.["Name"] ]
          th [] [ encLocText s.["Admin?"] ]
          ]
        ]
      users
      |> List.map (fun user ->
          let userId    = user.userId.ToString "N"
          let delAction = sprintf "/user/%s/delete" userId
          let delPrompt = s.["Are you want to delete this {0}?  This action cannot be undone.",
                              (sprintf "%s (%s)" (s.["User"].Value.ToLower()) user.fullName)].Value
          tr [] [
            td [] [
              a [ _href (sprintf "/user/%s/edit" userId); _title s.["Edit This User"].Value ] [ icon "edit" ]
              a [ _href (sprintf "/user/%s/small-groups" userId); _title s.["Assign Groups to This User"].Value ]
                [ icon "group" ]
              a [ _href delAction
                  _title s.["Delete This User"].Value
                  _onclick (sprintf "return PT.confirmDelete('%s','%s')" delAction delPrompt) ]
                [ icon "delete_forever" ]
              ]
            td [] [ encodedText user.fullName ]
            td [ _class "pt-center-text" ] [
              match user.isAdmin with
              | true -> yield strong [] [ encLocText s.["Yes"] ]
              | false -> yield encLocText s.["No"]
              ]
            ])
      |> tbody []
      ]
    form [ _id "DeleteForm"; _action ""; _method "post" ] [ csrfToken ctx ]
    ]
  |> Layout.Content.standard
  |> Layout.standard vi "Maintain Users"
