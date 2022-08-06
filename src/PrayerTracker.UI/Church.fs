module PrayerTracker.Views.Church

open Giraffe.ViewEngine
open Giraffe.ViewEngine.Accessibility
open Giraffe.ViewEngine.Htmx
open PrayerTracker
open PrayerTracker.Entities
open PrayerTracker.ViewModels

/// View for the church edit page
let edit (model : EditChurch) ctx viewInfo =
    let pageTitle = if model.IsNew then "Add a New Church" else "Edit Church"
    let s         = I18N.localizer.Force ()
    let vi        =
        viewInfo
        |> AppViewInfo.withScopedStyles [
            $"#{nameof model.Name} {{ width: 20rem; }}"
            $"#{nameof model.City} {{ width: 10rem; }}"
            $"#{nameof model.State} {{ width: 3rem; }}"
            $"#{nameof model.InterfaceAddress} {{ width: 30rem; }}"
        ]
        |> AppViewInfo.withOnLoadScript "PT.church.edit.onPageLoad"
    form [ _action "/church/save"; _method "post"; _class "pt-center-columns"; Target.content ] [
        csrfToken ctx
        input [ _type "hidden"; _name (nameof model.ChurchId); _value model.ChurchId ]
        div [ _fieldRow ] [
            div [ _inputField ] [
                label [ _for (nameof model.Name) ] [ locStr s["Church Name"] ]
                inputField "text" (nameof model.Name) model.Name [ _required; _autofocus ]
            ]
            div [ _inputField ] [
                label [ _for (nameof model.City) ] [ locStr s["City"] ]
                inputField "text" (nameof model.City) model.City [ _required ]
            ]
            div [ _inputField ] [
                label [ _for (nameof model.State) ] [ locStr s["State or Province"] ]
                inputField "text" (nameof model.State) model.State [ _minlength "2"; _maxlength "2"; _required ]
            ]
        ]
        div [ _fieldRow ] [
            div [ _checkboxField ] [
                inputField "checkbox" (nameof model.HasInterface) "True"
                           [ if defaultArg model.HasInterface false then _checked ]
                label [ _for (nameof model.HasInterface) ] [ locStr s["Has an interface with Virtual Prayer Room"] ]
            ]
        ]
        div [ _fieldRowWith [ "pt-fadeable" ]; _id "divInterfaceAddress" ] [
            div [ _inputField ] [
                label [ _for (nameof model.InterfaceAddress) ] [ locStr s["VPR Interface URL"] ]
                inputField "url" (nameof model.InterfaceAddress) (defaultArg model.InterfaceAddress "") []
            ]
        ]
        div [ _fieldRow ] [ submit [] "save" s["Save Church"] ]
    ]
    |> List.singleton
    |> Layout.Content.standard
    |> Layout.standard vi pageTitle


/// View for church maintenance page
let maintain (churches : Church list) (stats : Map<string, ChurchStats>) ctx viewInfo =
    let s  = I18N.localizer.Force ()
    let vi = AppViewInfo.withScopedStyles [ "#churchList { grid-template-columns: repeat(7, auto); }" ] viewInfo
    let churchTable =
        match churches with
        | [] -> space
        | _ ->
            section [ _id "churchList"; _class "pt-table"; _ariaLabel "Church list" ] [
                div [ _class "row head" ] [
                    header [ _class "cell" ] [ locStr s["Actions"] ]
                    header [ _class "cell" ] [ locStr s["Name"] ]
                    header [ _class "cell" ] [ locStr s["Location"] ]
                    header [ _class "cell" ] [ locStr s["Groups"] ]
                    header [ _class "cell" ] [ locStr s["Requests"] ]
                    header [ _class "cell" ] [ locStr s["Users"] ]
                    header [ _class "cell" ] [ locStr s["Interface?"] ]
                ]
                for church in churches do
                    let churchId  = shortGuid church.Id.Value
                    let delAction = $"/church/{churchId}/delete"
                    let delPrompt = s["Are you sure you want to delete this {0}?  This action cannot be undone.",
                                      $"""{s["Church"].Value.ToLower ()} ({church.Name})"""]
                    div [ _class "row" ] [
                        div [ _class "cell actions" ] [
                            a [ _href $"/church/{churchId}/edit"; _title s["Edit This Church"].Value ] [
                                iconSized 18 "edit"
                            ]
                            a [ _href      delAction
                                _title     s["Delete This Church"].Value
                                _hxPost    delAction
                                _hxConfirm delPrompt.Value ] [
                                iconSized 18 "delete_forever"
                            ]
                        ]
                        div [ _class "cell" ] [ str church.Name ]
                        div [ _class "cell" ] [ str church.City; rawText ", "; str church.State ]
                        div [ _class "cell pt-right-text" ] [ rawText (stats[churchId].SmallGroups.ToString "N0") ]
                        div [ _class "cell pt-right-text" ] [ rawText (stats[churchId].PrayerRequests.ToString "N0") ]
                        div [ _class "cell pt-right-text" ] [ rawText (stats[churchId].Users.ToString "N0") ]
                        div [ _class "cell pt-center-text" ] [
                            locStr s[if church.HasVpsInterface then "Yes" else "No"]
                        ]
                    ]
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
        form [ _method "post" ] [
            csrfToken ctx
            churchTable
        ]
    ]
    |> Layout.Content.wide
    |> Layout.standard vi "Maintain Churches"
