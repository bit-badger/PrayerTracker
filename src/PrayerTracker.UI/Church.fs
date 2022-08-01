module PrayerTracker.Views.Church

open Giraffe.ViewEngine
open PrayerTracker.Entities
open PrayerTracker.ViewModels

/// View for the church edit page
let edit (model : EditChurch) ctx viewInfo =
    let pageTitle = if model.IsNew then "Add a New Church" else "Edit Church"
    let s         = I18N.localizer.Force ()
    let vi        =
        viewInfo
        |> AppViewInfo.withScopedStyles [
            "#name { width: 20rem; }"
            "#city { width: 10rem; }"
            "#st { width: 3rem; }"
            "#interfaceAddress { width: 30rem; }"
        ]
        |> AppViewInfo.withOnLoadScript "PT.church.edit.onPageLoad"
    form [ _action "/church/save"; _method "post"; _class "pt-center-columns"; Target.content ] [
        csrfToken ctx
        input [ _type "hidden"; _name (nameof model.ChurchId); _value (flatGuid model.ChurchId) ]
        div [ _fieldRow ] [
            div [ _inputField ] [
                label [ _for "name" ] [ locStr s["Church Name"] ]
                input [ _type "text"; _name (nameof model.Name); _id "name"; _required; _autofocus; _value model.Name ]
            ]
            div [ _inputField ] [
                label [ _for "City"] [ locStr s["City"] ]
                input [ _type "text"; _name (nameof model.City); _id "city"; _required; _value model.City ]
            ]
            div [ _inputField ] [
                label [ _for "state" ] [ locStr s["State or Province"] ]
                input [ _type      "text"
                        _name      (nameof model.State)
                        _id        "state"
                        _minlength "2"; _maxlength "2"
                        _value     model.State
                        _required ]
            ]
        ]
        div [ _fieldRow ] [
            div [ _checkboxField ] [
                input [ _type  "checkbox"
                        _name  (nameof model.HasInterface)
                        _id    "hasInterface"
                        _value "True"
                        if defaultArg model.HasInterface false then _checked ]
                label [ _for "hasInterface" ] [ locStr s["Has an interface with Virtual Prayer Room"] ]
            ]
        ]
        div [ _fieldRowWith [ "pt-fadeable" ]; _id "divInterfaceAddress" ] [
            div [ _inputField ] [
                label [ _for "interfaceAddress" ] [ locStr s["VPR Interface URL"] ]
                input [ _type  "url"
                        _name  (nameof model.InterfaceAddress)
                        _id    "interfaceAddress";
                        _value (defaultArg model.InterfaceAddress "") ]
            ]
        ]
        div [ _fieldRow ] [ submit [] "save" s["Save Church"] ]
    ]
    |> List.singleton
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
                            a [ _href    delAction
                                _title   s["Delete This Church"].Value
                                _onclick $"return PT.confirmDelete('{delAction}','{delPrompt}')" ] [
                                icon "delete_forever"
                            ]
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
    [   div [ _class "pt-center-text" ] [
            br []
            a [ _href $"/church/{emptyGuid}/edit"; _title s["Add a New Church"].Value ] [
                icon "add_circle"; rawText " &nbsp;"; locStr s["Add a New Church"]
            ]
            br []
            br []
        ]
        tableSummary churches.Length s
        chTbl
        form [ _id "DeleteForm"; _action ""; _method "post" ] [ csrfToken ctx ]
    ]
    |> Layout.Content.wide
    |> Layout.standard vi "Maintain Churches"
