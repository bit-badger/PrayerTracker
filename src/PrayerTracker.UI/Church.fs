module PrayerTracker.Views.Church

open Giraffe.ViewEngine
open PrayerTracker.Entities
open PrayerTracker.ViewModels

/// View for the church edit page
let edit (m : EditChurch) ctx vi =
    let pageTitle = if m.IsNew then "Add a New Church" else "Edit Church"
    let s         = I18N.localizer.Force ()
    [ form [ _action "/church/save"; _method "post"; _class "pt-center-columns" ] [
        style [ _scoped ] [
            rawText "#name { width: 20rem; } #city { width: 10rem; } #st { width: 3rem; } #interfaceAddress { width: 30rem; }"
        ]
        csrfToken ctx
        input [ _type "hidden"; _name (nameof m.ChurchId); _value (flatGuid m.ChurchId) ]
        div [ _class "pt-field-row" ] [
            div [ _class "pt-field" ] [
                label [ _for "name" ] [ locStr s["Church Name"] ]
                input [ _type "text"; _name (nameof m.Name); _id "name"; _required; _autofocus; _value m.Name ]
            ]
            div [ _class "pt-field" ] [
                label [ _for "City"] [ locStr s["City"] ]
                input [ _type "text"; _name (nameof m.City); _id "city"; _required; _value m.City ]
            ]
            div [ _class "pt-field" ] [
                label [ _for "state" ] [ locStr s["State or Province"] ]
                input [ _type "text"
                        _name (nameof m.State)
                        _id "state"
                        _required
                        _minlength "2"; _maxlength "2"
                        _value m.State ]
            ]
        ]
        div [ _class "pt-field-row" ] [
            div [ _class "pt-checkbox-field" ] [
                input [ _type "checkbox"
                        _name (nameof m.HasInterface)
                        _id "hasInterface"
                        _value "True"
                        if defaultArg m.HasInterface false then _checked ]
                label [ _for "hasInterface" ] [ locStr s["Has an interface with Virtual Prayer Room"] ]
            ]
        ]
        div [ _class "pt-field-row pt-fadeable"; _id "divInterfaceAddress" ] [
            div [ _class "pt-field" ] [
                label [ _for "interfaceAddress" ] [ locStr s["VPR Interface URL"] ]
                input [ _type "url"
                        _name (nameof m.InterfaceAddress)
                        _id "interfaceAddress";
                        _value (defaultArg m.InterfaceAddress "") ]
            ]
        ]
        div [ _class "pt-field-row" ] [ submit [] "save" s["Save Church"] ]
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
                        th [] [ locStr s["Actions"] ]
                        th [] [ locStr s["Name"] ]
                        th [] [ locStr s["Location"] ]
                        th [] [ locStr s["Groups"] ]
                        th [] [ locStr s["Requests"] ]
                        th [] [ locStr s["Users"] ]
                        th [] [ locStr s["Interface?"] ]
                    ]
                ]
                churches
                |> List.map (fun ch ->
                    let chId      = flatGuid ch.churchId
                    let delAction = $"/church/{chId}/delete"
                    let delPrompt = s["Are you sure you want to delete this {0}?  This action cannot be undone.",
                                      $"""{s["Church"].Value.ToLower ()} ({ch.name})"""]
                    tr [] [
                        td [] [
                          a [ _href $"/church/{chId}/edit"; _title s["Edit This Church"].Value ] [ icon "edit" ]
                          a [ _href delAction
                              _title s["Delete This Church"].Value
                              _onclick $"return PT.confirmDelete('{delAction}','{delPrompt}')" ]
                            [ icon "delete_forever" ]
                          ]
                        td [] [ str ch.name ]
                        td [] [ str ch.city; rawText ", "; str ch.st ]
                        td [ _class "pt-right-text" ] [ rawText (stats[chId].smallGroups.ToString "N0") ]
                        td [ _class "pt-right-text" ] [ rawText (stats[chId].prayerRequests.ToString "N0") ]
                        td [ _class "pt-right-text" ] [ rawText (stats[chId].users.ToString "N0") ]
                        td [ _class "pt-center-text" ] [ locStr s[if ch.hasInterface then "Yes" else "No"] ]
                    ])
                |> tbody []
            ]
    [ div [ _class "pt-center-text" ] [
        br []
        a [ _href $"/church/{emptyGuid}/edit"; _title s["Add a New Church"].Value ]
          [ icon "add_circle"; rawText " &nbsp;"; locStr s["Add a New Church"] ]
        br []
        br []
      ]
      tableSummary churches.Length s
      chTbl
      form [ _id "DeleteForm"; _action ""; _method "post" ] [ csrfToken ctx ]
    ]
    |> Layout.Content.wide
    |> Layout.standard vi "Maintain Churches"
