module PrayerTracker.Views.Church

open Giraffe.GiraffeViewEngine
open PrayerTracker.Entities
open PrayerTracker.ViewModels

/// View for the church edit page
let edit (m : EditChurch) ctx vi =
  let pageTitle = match m.isNew () with true -> "Add a New Church" | false -> "Edit Church"
  let s         = I18N.localizer.Force ()
  [ form [ _action "/church/save"; _method "post"; _class "pt-center-columns" ] [
      style [ _scoped ]
        [ rawText "#name { width: 20rem; } #city { width: 10rem; } #st { width: 3rem; } #interfaceAddress { width: 30rem; }" ]
      csrfToken ctx
      input [ _type "hidden"; _name "churchId"; _value (flatGuid m.churchId) ]
      div [ _class "pt-field-row" ] [
        div [ _class "pt-field" ] [
          label [ _for "name" ] [ encLocText s.["Church Name"] ]
          input [ _type "text"; _name "name"; _id "name"; _required; _autofocus; _value m.name ]
          ]
        div [ _class "pt-field" ] [
          label [ _for "City"] [ encLocText s.["City"] ]
          input [ _type "text"; _name "city"; _id "city"; _required; _value m.city ]
          ]
        div [ _class "pt-field" ] [
          label [ _for "ST" ] [ encLocText s.["State"] ]
          input [ _type "text"; _name "st"; _id "st"; _required; _minlength "2"; _maxlength "2"; _value m.st ]
          ]
        ]
      div [ _class "pt-field-row" ] [
        div [ _class "pt-checkbox-field" ] [
          input [ yield _type "checkbox"
                  yield _name "hasInterface"
                  yield _id "hasInterface"
                  yield _value "True"
                  match m.hasInterface with Some x when x -> yield _checked | _ -> () ]
          label [ _for "hasInterface" ] [ encLocText s.["Has an interface with Virtual Prayer Room"] ]
          ]
        ]
      div [ _class "pt-field-row pt-fadeable"; _id "divInterfaceAddress" ] [
        div [ _class "pt-field" ] [
          label [ _for "interfaceAddress" ] [ encLocText s.["VPR Interface URL"] ]
          input [ _type "url"; _name "interfaceAddress"; _id "interfaceAddress";
                  _value (match m.interfaceAddress with Some ia -> ia | None -> "") ]
          ]
        ]
      div [ _class "pt-field-row" ] [ submit [] "save" s.["Save Church"] ]
      ]
    script [] [ rawText "PT.onLoad(PT.church.edit.onPageLoad)" ]
    ]
  |> Layout.Content.standard
  |> Layout.standard vi pageTitle


/// View for church maintenance page
let maintain (churches : Church list) (stats : Map<string, ChurchStats>) ctx vi =
  let s     = I18N.localizer.Force ()
  let chTbl =
    match churches with
    | [] -> space
    | _ ->
        table [ _class "pt-table pt-action-table" ] [
          thead [] [
            tr [] [
              th [] [ encLocText s.["Actions"] ]
              th [] [ encLocText s.["Name"] ]
              th [] [ encLocText s.["Location"] ]
              th [] [ encLocText s.["Groups"] ]
              th [] [ encLocText s.["Requests"] ]
              th [] [ encLocText s.["Users"] ]
              th [] [ encLocText s.["Interface?"] ]
              ]
            ]
          churches
          |> List.map (fun ch ->
              let chId      = flatGuid ch.churchId
              let delAction = sprintf "/church/%s/delete" chId
              let delPrompt = s.["Are you want to delete this {0}?  This action cannot be undone.",
                                  sprintf "%s (%s)" (s.["Church"].Value.ToLower ()) ch.name]
              tr [] [
                td [] [
                  a [ _href (sprintf "/church/%s/edit" chId); _title s.["Edit This Church"].Value ] [ icon "edit" ]
                  a [ _href delAction
                      _title s.["Delete This Church"].Value
                      _onclick (sprintf "return PT.confirmDelete('%s','%A')" delAction delPrompt) ]
                    [ icon "delete_forever" ]
                  ]
                td [] [ encodedText ch.name ]
                td [] [ encodedText ch.city; rawText ", "; encodedText ch.st ]
                td [ _class "pt-right-text" ] [ rawText (stats.[chId].smallGroups.ToString "N0") ]
                td [ _class "pt-right-text" ] [ rawText (stats.[chId].prayerRequests.ToString "N0") ]
                td [ _class "pt-right-text" ] [ rawText (stats.[chId].users.ToString "N0") ]
                td [ _class "pt-center-text" ] [ encLocText s.[match ch.hasInterface with true -> "Yes" | false -> "No"] ]
                ])
          |> tbody []
          ]
  [ div [ _class "pt-center-text" ] [
      br []
      a [ _href (sprintf "/church/%s/edit" emptyGuid); _title s.["Add a New Church"].Value ]
        [ icon "add_circle"; rawText " &nbsp;"; encLocText s.["Add a New Church"] ]
      br []
      br []
      ]
    tableSummary churches.Length s
    chTbl
    form [ _id "DeleteForm"; _action ""; _method "post" ] [ csrfToken ctx ]
    ]
  |> Layout.Content.wide
  |> Layout.standard vi "Maintain Churches"
