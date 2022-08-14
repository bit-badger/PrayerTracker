module PrayerTracker.Views.PrayerRequest

open System
open System.IO
open Giraffe
open Giraffe.ViewEngine
open Giraffe.ViewEngine.Accessibility
open Giraffe.ViewEngine.Htmx
open Microsoft.AspNetCore.Http
open NodaTime
open PrayerTracker
open PrayerTracker.Entities
open PrayerTracker.ViewModels

/// View for the prayer request edit page
let edit (model : EditRequest) today ctx viewInfo =
    let s         = I18N.localizer.Force ()
    let pageTitle = if model.IsNew then "Add a New Request" else "Edit Request"
    let vi        = AppViewInfo.withOnLoadScript "PT.initCKEditor" viewInfo
    form [ _action "/prayer-request/save"; _method "post"; _class "pt-center-columns"; Target.content ] [
        csrfToken ctx
        inputField "hidden" (nameof model.RequestId) model.RequestId []
        div [ _fieldRow ] [
            div [ _inputField ] [
                label [ _for (nameof model.RequestType) ] [ locStr s["Request Type"] ]
                ReferenceList.requestTypeList s
                |> Seq.ofList
                |> Seq.map (fun (typ, desc) -> PrayerRequestType.toCode typ, desc.Value)
                |> selectList (nameof model.RequestType) model.RequestType [ _required; _autofocus ]
            ]
            div [ _inputField ] [
                label [ _for (nameof model.Requestor) ] [ locStr s["Requestor / Subject"] ]
                inputField "text" (nameof model.Requestor) (defaultArg model.Requestor "") []
            ]
            if model.IsNew then
                div [ _inputField ] [
                    label [ _for (nameof model.EnteredDate) ] [ locStr s["Date"] ]
                    inputField "date" (nameof model.EnteredDate) "" [ _placeholder today ]
                ]
            else
                div [ _inputField ] [
                    br []
                    div [ _checkboxField ] [
                        inputField "checkbox" (nameof model.SkipDateUpdate) "True" []
                        label [ _for (nameof model.SkipDateUpdate) ] [ locStr s["Check to not update the date"] ]
                    ]
                    small [] [ em [] [ str (s["Typo Corrections"].Value.ToLower ()); rawText ", etc." ] ]
                ]
        ]
        div [ _fieldRow ] [
            div [ _inputField ] [
                label [] [ locStr s["Expiration"] ]
                ReferenceList.expirationList s (not model.IsNew)
                |> List.map (fun exp ->
                    let radioId = String.concat "_" [ nameof model.Expiration; fst exp ] 
                    span [ _class "text-nowrap" ] [
                        radio (nameof model.Expiration) radioId (fst exp) model.Expiration
                        label [ _for radioId ] [ space; locStr (snd exp) ]
                        rawText " &nbsp; &nbsp; "
                    ])
                |> div [ _class "pt-center-text" ]
            ]
        ]
        div [ _fieldRow ] [
            div [ _inputFieldWith [ "pt-editor" ] ] [
                label [ _for (nameof model.Text) ] [ locStr s["Request"] ]
                textarea [ _name (nameof model.Text); _id (nameof model.Text) ] [ str model.Text ]
            ]
        ]
        div [ _fieldRow ] [ submit [] "save" s["Save Request"] ]
    ]
    |> List.singleton
    |> Layout.Content.standard
    |> Layout.standard vi pageTitle

/// View for the request e-mail results page
let email model viewInfo =
    let s         = I18N.localizer.Force ()
    let pageTitle = $"""{s["Prayer Requests"].Value} • {model.SmallGroup.Name}"""
    let prefs     = model.SmallGroup.Preferences
    let addresses = model.Recipients |> List.map (fun mbr -> $"{mbr.Name} <{mbr.Email}>") |> String.concat ", "
    [   p [ _style $"font-family:{prefs.Fonts};font-size:%i{prefs.TextFontSize}pt;" ] [
            locStr s["The request list was sent to the following people, via individual e-mails"]
            rawText ":"
            br []
            small [] [ str addresses ]
        ]
        span [ _class "pt-email-heading" ] [ locStr s["HTML Format"]; rawText ":" ]
        div [ _class "pt-email-canvas" ] [ rawText (model.AsHtml s) ]
        br []
        br []
        span [ _class "pt-email-heading" ] [ locStr s["Plain-Text Format"]; rawText ":" ]
        div [ _class "pt-email-canvas" ] [ pre [] [ str (model.AsText s) ] ]
    ]
    |> Layout.Content.standard
    |> Layout.standard viewInfo pageTitle


/// View for a small group's public prayer request list
let list (model : RequestList) viewInfo =
    [   br []
        I18N.localizer.Force () |> (model.AsHtml >> rawText) 
    ]
    |> Layout.Content.standard
    |> Layout.standard viewInfo "View Request List"


/// View for the prayer request lists page
let lists (groups : SmallGroupInfo list) viewInfo =
    let s   = I18N.localizer.Force ()
    let l   = I18N.forView "Requests/Lists"
    use sw  = new StringWriter ()
    let raw = rawLocText sw
    let vi  = AppViewInfo.withScopedStyles [ "#groupList { grid-template-columns: repeat(3, auto); }" ] viewInfo
    [   p [] [
            raw l["The groups listed below have either public or password-protected request lists."]
            space
            raw l["Those with list icons are public, and those with log on icons are password-protected."]
            space
            raw l["Click the appropriate icon to log on or view the request list."]
        ]
        match groups.Length with
        | 0 -> p [] [ raw l["There are no groups with public or password-protected request lists."] ]
        | count ->
            tableSummary count s
            section [ _id "groupList"; _class "pt-table"; _ariaLabel "Small group list" ] [
                div [ _class "row head" ] [
                    header [ _class "cell" ] [ locStr s["Actions"] ]
                    header [ _class "cell" ] [ locStr s["Church"] ]
                    header [ _class "cell" ] [ locStr s["Group"] ]
                ]
                for group in groups do
                    div [ _class "row" ] [
                        div [ _class "cell actions" ] [
                            if group.IsPublic then
                                a [ _href $"/prayer-requests/{group.Id}/list"; _title s["View"].Value ] [
                                    iconSized 18 "list"
                                ]
                            else
                                a [ _href $"/small-group/log-on/{group.Id}"; _title s["Log On"].Value ] [
                                    iconSized 18 "verified_user"
                                ]
                        ]
                        div [ _class "cell" ] [ str group.ChurchName ]
                        div [ _class "cell" ] [ str group.Name ]
                    ]
            ]
    ]
    |> Layout.Content.standard
    |> Layout.standard vi "Request Lists"


/// View for the prayer request maintenance page
let maintain (model : MaintainRequests) (ctx : HttpContext) viewInfo =
    let s     = I18N.localizer.Force ()
    let l     = I18N.forView "Requests/Maintain"
    use sw    = new StringWriter ()
    let raw   = rawLocText sw
    let group = model.SmallGroup
    let now   = SmallGroup.localDateNow (ctx.GetService<IClock> ()) group
    let types = ReferenceList.requestTypeList s |> Map.ofList
    let vi    = AppViewInfo.withScopedStyles [ "#requestList { grid-template-columns: repeat(5, auto); }" ] viewInfo
    /// Iterate the sequence once, before we render, so we can get the count of it at the top of the table
    let requests =
        model.Requests
        |> List.map (fun req ->
            let updateClass  =
                _class (if PrayerRequest.updateRequired now group req then "cell pt-request-update" else "cell")
            let isExpired    = PrayerRequest.isExpired now group req
            let expiredClass = _class (if isExpired then "cell pt-request-expired" else "cell")
            let reqId        = shortGuid req.Id.Value
            let reqText      = htmlToPlainText req.Text
            let delAction    = $"/prayer-request/{reqId}/delete"
            let delPrompt    =
                [   s["Are you sure you want to delete this {0}?  This action cannot be undone.",
                        s["Prayer Request"].Value.ToLower() ].Value
                    "\\n"
                    l["(If the prayer request has been answered, or an event has passed, consider inactivating it instead.)"]
                        .Value
                ]
                |> String.concat ""
            div [ _class "row" ] [
                div [ _class "cell actions" ] [
                    a [ _href $"/prayer-request/{reqId}/edit"; _title l["Edit This Prayer Request"].Value ] [
                        iconSized 18 "edit"
                    ]
                    if isExpired then
                        a [ _href  $"/prayer-request/{reqId}/restore"
                            _title l["Restore This Inactive Request"].Value ] [
                            iconSized 18 "visibility"
                        ]
                    else
                        a [ _href  $"/prayer-request/{reqId}/expire"
                            _title l["Expire This Request Immediately"].Value ] [
                            iconSized 18 "visibility_off"
                        ]
                    a [ _href      delAction
                        _title     l["Delete This Request"].Value
                        _hxPost    delAction
                        _hxConfirm delPrompt ] [
                        iconSized 18 "delete_forever"
                    ]
                ]
                div [ updateClass ] [
                    str (req.UpdatedDate.ToString(s["MMMM d, yyyy"].Value, Globalization.CultureInfo.CurrentUICulture))
                ]
                div [ _class "cell" ] [ locStr types[req.RequestType] ]
                div [ expiredClass ] [ str (match req.Requestor with Some r -> r | None -> " ") ]
                div [ _class "cell" ] [
                    match reqText.Length with
                    | len when len < 60 -> rawText reqText
                    | _ -> rawText $"{reqText[0..59]}&hellip;"
                ]
            ])
        |> List.ofSeq
    [   div [ _class "pt-center-text" ] [
            br []
            a [ _href $"/prayer-request/{emptyGuid}/edit"; _title s["Add a New Request"].Value ] [
                icon "add_circle"; rawText " &nbsp;"; locStr s["Add a New Request"]
            ]
            rawText " &nbsp; &nbsp; &nbsp; "
            a [ _href "/prayer-requests/view"; _title s["View Prayer Request List"].Value ] [
                icon "list"; rawText " &nbsp;"; locStr s["View Prayer Request List"]
            ]
            match model.SearchTerm with
            | Some _ ->
                rawText " &nbsp; &nbsp; &nbsp; "
                a [ _href "/prayer-requests"; _title l["Clear Search Criteria"].Value ] [
                    icon "highlight_off"; rawText " &nbsp;"; raw l["Clear Search Criteria"]
                ]
            | None -> ()
        ]
        form [ _action "/prayer-requests"; _method "get"; _class "pt-center-text pt-search-form"; Target.content ] [
            inputField "text" "search" (defaultArg model.SearchTerm "") [ _placeholder l["Search requests..."].Value ]
            space
            submit [] "search" s["Search"]
        ]
        br []
        tableSummary requests.Length s
        match requests.Length with
        | 0 -> ()
        | _ ->
            form [ _method "post" ] [
                csrfToken ctx
                section [ _id "requestList"; _class "pt-table"; _ariaLabel "Prayer request list" ] [
                    div [ _class "row head" ] [
                        header [ _class "cell" ] [ locStr s["Actions"] ]
                        header [ _class "cell" ] [ locStr s["Updated Date"] ]
                        header [ _class "cell" ] [ locStr s["Type"] ]
                        header [ _class "cell" ] [ locStr s["Requestor"] ]
                        header [ _class "cell" ] [ locStr s["Request"] ]
                    ]
                    yield! requests
                ]
            ]
        div [ _class "pt-center-text" ] [
            br []
            match model.OnlyActive with
            | Some true ->
                raw l["Inactive requests are currently not shown"]
                br []
                a [ _href "/prayer-requests/inactive" ] [ raw l["Show Inactive Requests"] ]
            | _ ->
                if Option.isSome model.OnlyActive then
                    raw l["Inactive requests are currently shown"]
                    br []
                    a [ _href "/prayer-requests" ] [ raw l["Do Not Show Inactive Requests"] ]
                    br []
                    br []
                let search = [ match model.SearchTerm with Some s -> "search", s | None -> () ]
                let pg     = defaultArg model.PageNbr 1
                let url    =
                    match model.OnlyActive with Some true | None -> "" | _ -> "/inactive"
                    |> sprintf "/prayer-requests%s"
                match pg with
                | 1 -> ()
                | _ ->
                    // button (_type "submit" :: attrs) [ icon ico; rawText " &nbsp;"; locStr text ]
                    let withPage = match pg with 2 -> search | _ -> ("page", string (pg - 1)) :: search
                    a [ _href (makeUrl url withPage) ] [ icon "keyboard_arrow_left"; space; raw l["Previous Page"] ]
                rawText " &nbsp; &nbsp; "
                match requests.Length = model.SmallGroup.Preferences.PageSize with
                | true ->
                    a [ _href (makeUrl url (("page", string (pg + 1)) :: search)) ] [
                        raw l["Next Page"]; space; icon "keyboard_arrow_right"
                    ]
                | false -> ()
        ]
    ]
    |> Layout.Content.wide
    |> Layout.standard vi (match model.SearchTerm with Some _ -> "Search Results" | None -> "Maintain Requests")


/// View for the printable prayer request list
let print model version =
    let s         = I18N.localizer.Force ()
    let pageTitle = $"""{s["Prayer Requests"].Value} • {model.SmallGroup.Name}"""
    let imgAlt    = $"""{s["PrayerTracker"].Value} {s["from Bit Badger Solutions"].Value}"""
    article [] [
        rawText (model.AsHtml s)
        br []
        hr []
        div [ _style $"font-size:70%%;font-family:{model.SmallGroup.Preferences.Fonts};" ] [
            img [ _src $"""/img/{s["footer_en"].Value}.png"""
                  _style "vertical-align:text-bottom;"
                  _alt imgAlt
                  _title imgAlt ]
            space
            str version
        ]
    ]
    |> Layout.bare pageTitle


/// View for the prayer request list
let view model viewInfo =
    let s         = I18N.localizer.Force ()
    let pageTitle = $"""{s["Prayer Requests"].Value} • {model.SmallGroup.Name}"""
    let spacer    = rawText " &nbsp; &nbsp; &nbsp; "
    let dtString  = model.Date.ToString ("yyyy-MM-dd", null) // TODO: this should be invariant
    [   div [ _class "pt-center-text" ] [
            br []
            a [ _class  "pt-icon-link"
                _href   $"/prayer-requests/print/{dtString}"
                _target "_blank"
                _title  s["View Printable"].Value ] [
                icon "print"; rawText " &nbsp;"; locStr s["View Printable"]
            ]
            if model.CanEmail then
                spacer
                if model.Date.DayOfWeek <> IsoDayOfWeek.Sunday then
                    let rec findSunday (date : LocalDate) =
                        if date.DayOfWeek = IsoDayOfWeek.Sunday then date else findSunday (date.PlusDays 1)
                    let sunday = findSunday model.Date
                    a [ _class "pt-icon-link"
                        _href  $"""/prayer-requests/view/{sunday.ToString ("yyyy-MM-dd", null)}""" // TODO: make invariant
                        _title s["List for Next Sunday"].Value ] [
                        icon "update"; rawText " &nbsp;"; locStr s["List for Next Sunday"]
                    ]
                    spacer
                let emailPrompt = s["This will e-mail the current list to every member of your group, without further prompting.  Are you sure this is what you are ready to do?"].Value
                a [ _class   "pt-icon-link"
                    _href    $"/prayer-requests/email/{dtString}"
                    _title   s["Send via E-mail"].Value
                    _onclick $"return PT.requests.view.promptBeforeEmail('{emailPrompt}')" ] [
                    icon "mail_outline"; rawText " &nbsp;"; locStr s["Send via E-mail"]
                ]
            spacer
            a [ _class "pt-icon-link"; _href "/prayer-requests"; _title s["Maintain Prayer Requests"].Value ] [
                icon "compare_arrows"; rawText " &nbsp;"; locStr s["Maintain Prayer Requests"]
            ]
        ]
        br []
        rawText (model.AsHtml s)
    ]
    |> Layout.Content.standard
    |> Layout.standard viewInfo pageTitle
