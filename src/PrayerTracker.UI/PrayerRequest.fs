module PrayerTracker.Views.PrayerRequest

open System
open System.IO
open Giraffe
open Giraffe.ViewEngine
open Microsoft.AspNetCore.Http
open NodaTime
open PrayerTracker
open PrayerTracker.Entities
open PrayerTracker.ViewModels

/// View for the prayer request edit page
let edit (m : EditRequest) today ctx vi =
    let s         = I18N.localizer.Force ()
    let pageTitle = if m.IsNew then "Add a New Request" else "Edit Request"
    [ form [ _action "/prayer-request/save"; _method "post"; _class "pt-center-columns" ] [
        csrfToken ctx
        input [ _type "hidden"; _name (nameof m.RequestId); _value (flatGuid m.RequestId) ]
        div [ _class "pt-field-row" ] [
            div [ _class "pt-field" ] [
                label [ _for (nameof m.RequestType) ] [ locStr s["Request Type"] ]
                ReferenceList.requestTypeList s
                |> Seq.ofList
                |> Seq.map (fun (typ, desc) -> typ.code, desc.Value)
                |> selectList (nameof m.RequestType) m.RequestType [ _required; _autofocus ]
            ]
            div [ _class "pt-field" ] [
                label [ _for "requestor" ] [ locStr s["Requestor / Subject"] ]
                input [ _type "text"
                        _name (nameof m.Requestor)
                        _id "requestor"
                        _value (defaultArg m.Requestor "") ]
            ]
            if m.IsNew then
                div [ _class "pt-field" ] [
                    label [ _for "enteredDate" ] [ locStr s["Date"] ]
                    input [ _type "date"; _name (nameof m.EnteredDate); _id "enteredDate"; _placeholder today ]
                ]
            else
                div [ _class "pt-field" ] [
                    div [ _class "pt-checkbox-field" ] [
                        br []
                        input [ _type "checkbox"; _name (nameof m.SkipDateUpdate); _id "skipDateUpdate"; _value "True" ]
                        label [ _for "skipDateUpdate" ] [ locStr s["Check to not update the date"] ]
                        br []
                        small [] [ em [] [ str (s["Typo Corrections"].Value.ToLower ()); rawText ", etc." ] ]
                    ]
                ]
        ]
        div [ _class "pt-field-row" ] [
            div [ _class "pt-field" ] [
                label [] [ locStr s["Expiration"] ]
                ReferenceList.expirationList s (not m.IsNew)
                |> List.map (fun exp ->
                    let radioId = $"expiration_{fst exp}"
                    span [ _class "text-nowrap" ] [
                        radio (nameof m.Expiration) radioId (fst exp) m.Expiration
                        label [ _for radioId ] [ locStr (snd exp) ]
                        rawText " &nbsp; &nbsp; "
                    ])
                |> div [ _class "pt-center-text" ]
            ]
        ]
        div [ _class "pt-field-row" ] [
            div [ _class "pt-field pt-editor" ] [
                label [ _for "text" ] [ locStr s["Request"] ]
                textarea [ _name (nameof m.Text); _id "text" ] [ str m.Text ]
            ]
        ]
        div [ _class "pt-field-row" ] [ submit [] "save" s["Save Request"] ]
        ]
      script [] [ rawText "PT.onLoad(PT.initCKEditor)" ]
    ]
    |> Layout.Content.standard
    |> Layout.standard vi pageTitle

/// View for the request e-mail results page
let email m vi =
    let s         = I18N.localizer.Force ()
    let pageTitle = $"""{s["Prayer Requests"].Value} • {m.SmallGroup.name}"""
    let prefs     = m.SmallGroup.preferences
    let addresses = m.Recipients |> List.map (fun mbr -> $"{mbr.memberName} <{mbr.email}>") |> String.concat ", "
    [ p [ _style $"font-family:{prefs.listFonts};font-size:%i{prefs.textFontSize}pt;" ] [
          locStr s["The request list was sent to the following people, via individual e-mails"]
          rawText ":"
          br []
          small [] [ str addresses ]
      ]
      span [ _class "pt-email-heading" ] [ locStr s["HTML Format"]; rawText ":" ]
      div [ _class "pt-email-canvas" ] [ rawText (m.AsHtml s) ]
      br []
      br []
      span [ _class "pt-email-heading" ] [ locStr s["Plain-Text Format"]; rawText ":" ]
      div [ _class "pt-email-canvas" ] [ pre [] [ str (m.AsText s) ] ]
    ]
    |> Layout.Content.standard
    |> Layout.standard vi pageTitle


/// View for a small group's public prayer request list
let list (m : RequestList) vi =
    [ br []
      I18N.localizer.Force () |> (m.AsHtml >> rawText) 
    ]
    |> Layout.Content.standard
    |> Layout.standard vi "View Request List"


/// View for the prayer request lists page
let lists (groups : SmallGroup list) vi =
    let s   = I18N.localizer.Force ()
    let l   = I18N.forView "Requests/Lists"
    use sw  = new StringWriter ()
    let raw = rawLocText sw
    [ p [] [
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
          table [ _class "pt-table pt-action-table" ] [
              thead [] [
                  tr [] [
                      th [] [ locStr s["Actions"] ]
                      th [] [ locStr s["Church"] ]
                      th [] [ locStr s["Group"] ]
                  ]
              ]
              groups
              |> List.map (fun grp ->
                  let grpId = flatGuid grp.smallGroupId
                  tr [] [
                      if grp.preferences.isPublic then
                          a [ _href $"/prayer-requests/{grpId}/list"; _title s["View"].Value ] [ icon "list" ]
                      else
                          a [ _href $"/small-group/log-on/{grpId}"; _title s["Log On"].Value ] [ icon "verified_user" ]
                      |> List.singleton
                      |> td []
                      td [] [ str grp.church.name ]
                      td [] [ str grp.name ]
                  ])
              |> tbody []
          ]
    ]
    |> Layout.Content.standard
    |> Layout.standard vi "Request Lists"


/// View for the prayer request maintenance page
let maintain (m : MaintainRequests) (ctx : HttpContext) vi =
    let s     = I18N.localizer.Force ()
    let l     = I18N.forView "Requests/Maintain"
    use sw    = new StringWriter ()
    let raw   = rawLocText sw
    let now   = m.SmallGroup.localDateNow (ctx.GetService<IClock> ())
    let types = ReferenceList.requestTypeList s |> Map.ofList
    let updReq (req : PrayerRequest) =
        if req.updateRequired now m.SmallGroup.preferences.daysToExpire m.SmallGroup.preferences.longTermUpdateWeeks then
            "pt-request-update"
        else ""
        |> _class 
    let reqExp (req : PrayerRequest) =
        _class (if req.isExpired now m.SmallGroup.preferences.daysToExpire then "pt-request-expired" else "")
    /// Iterate the sequence once, before we render, so we can get the count of it at the top of the table
    let requests =
        m.Requests
        |> List.map (fun req ->
            let reqId     = flatGuid req.prayerRequestId
            let reqText   = htmlToPlainText req.text
            let delAction = $"/prayer-request/{reqId}/delete"
            let delPrompt =
                [ s["Are you sure you want to delete this {0}?  This action cannot be undone.",
                    s["Prayer Request"].Value.ToLower() ].Value
                  "\\n"
                  l["(If the prayer request has been answered, or an event has passed, consider inactivating it instead.)"]
                      .Value
                ]
                |> String.concat ""
            tr [] [
                td [] [
                    a [ _href $"/prayer-request/{reqId}/edit"; _title l["Edit This Prayer Request"].Value ]
                      [ icon "edit" ]
                    if req.isExpired now m.SmallGroup.preferences.daysToExpire then
                        a [ _href $"/prayer-request/{reqId}/restore"
                            _title l["Restore This Inactive Request"].Value ]
                          [ icon "visibility" ]
                    else
                        a [ _href $"/prayer-request/{reqId}/expire"
                            _title l["Expire This Request Immediately"].Value ]
                          [ icon "visibility_off" ]
                    a [ _href delAction; _title l["Delete This Request"].Value;
                        _onclick $"return PT.confirmDelete('{delAction}','{delPrompt}')" ]
                      [ icon "delete_forever" ]
                ]
                td [ updReq req ] [
                    str (req.updatedDate.ToString(s["MMMM d, yyyy"].Value, Globalization.CultureInfo.CurrentUICulture))
                ]
                td [] [ locStr types[req.requestType] ]
                td [ reqExp req ] [ str (match req.requestor with Some r -> r | None -> " ") ]
                td [] [
                    match reqText.Length with
                    | len when len < 60 -> rawText reqText
                    | _ -> rawText $"{reqText[0..59]}&hellip;"
                ]
            ])
        |> List.ofSeq
    [ div [ _class "pt-center-text" ] [
        br []
        a [ _href $"/prayer-request/{emptyGuid}/edit"; _title s["Add a New Request"].Value ]
          [ icon "add_circle"; rawText " &nbsp;"; locStr s["Add a New Request"] ]
        rawText " &nbsp; &nbsp; &nbsp; "
        a [ _href "/prayer-requests/view"; _title s["View Prayer Request List"].Value ]
          [ icon "list"; rawText " &nbsp;"; locStr s["View Prayer Request List"] ]
        match m.SearchTerm with
        | Some _ ->
            rawText " &nbsp; &nbsp; &nbsp; "
            a [ _href "/prayer-requests"; _title l["Clear Search Criteria"].Value ]
              [ icon "highlight_off"; rawText " &nbsp;"; raw l["Clear Search Criteria"] ]
        | None -> ()
      ]
      form [ _action "/prayer-requests"; _method "get"; _class "pt-center-text pt-search-form" ] [
          input [ _type "text"
                  _name "search"
                  _placeholder l["Search requests..."].Value
                  _value (defaultArg m.SearchTerm "")
                ]
          space
          submit [] "search" s["Search"]
      ]
      br []
      tableSummary requests.Length s
      match requests.Length with
      | 0 -> ()
      | _ ->
          table [ _class "pt-table pt-action-table" ] [
              thead [] [
                  tr [] [
                      th [] [ locStr s["Actions"] ]
                      th [] [ locStr s["Updated Date"] ]
                      th [] [ locStr s["Type"] ]
                      th [] [ locStr s["Requestor"] ]
                      th [] [ locStr s["Request"] ]
                  ]
              ]
              tbody [] requests
          ]
      div [ _class "pt-center-text" ] [
          br []
          match m.OnlyActive with
          | Some true ->
              raw l["Inactive requests are currently not shown"]
              br []
              a [ _href "/prayer-requests/inactive" ] [ raw l["Show Inactive Requests"] ]
          | _ ->
              if defaultArg m.OnlyActive false then
                  raw l["Inactive requests are currently shown"]
                  br []
                  a [ _href "/prayer-requests" ] [ raw l["Do Not Show Inactive Requests"] ]
                  br []
                  br []
              let search = [ match m.SearchTerm with Some s -> "search", s | None -> () ]
              let pg     = defaultArg m.PageNbr 1
              let url    =
                  match m.OnlyActive with Some true | None -> "" | _ -> "/inactive" |> sprintf "/prayer-requests%s"
              match pg with
              | 1 -> ()
              | _ ->
                  // button (_type "submit" :: attrs) [ icon ico; rawText " &nbsp;"; locStr text ]
                  let withPage = match pg with 2 -> search | _ -> ("page", string (pg - 1)) :: search
                  a [ _href (makeUrl url withPage) ]
                    [ icon "keyboard_arrow_left"; space; raw l["Previous Page"] ]
              rawText " &nbsp; &nbsp; "
              match requests.Length = m.SmallGroup.preferences.pageSize with
              | true ->
                  a [ _href (makeUrl url (("page", string (pg + 1)) :: search)) ]
                    [ raw l["Next Page"]; space; icon "keyboard_arrow_right" ]
              | false -> ()
      ]
      form [ _id "DeleteForm"; _action ""; _method "post" ] [ csrfToken ctx ]
    ]
    |> Layout.Content.wide
    |> Layout.standard vi (match m.SearchTerm with Some _ -> "Search Results" | None -> "Maintain Requests")


/// View for the printable prayer request list
let print m version =
    let s         = I18N.localizer.Force ()
    let pageTitle = $"""{s["Prayer Requests"].Value} • {m.SmallGroup.name}"""
    let imgAlt    = $"""{s["PrayerTracker"].Value} {s["from Bit Badger Solutions"].Value}"""
    article [] [
        rawText (m.AsHtml s)
        br []
        hr []
        div [ _style $"font-size:70%%;font-family:{m.SmallGroup.preferences.listFonts};" ] [
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
let view m vi =
    let s         = I18N.localizer.Force ()
    let pageTitle = $"""{s["Prayer Requests"].Value} • {m.SmallGroup.name}"""
    let spacer    = rawText " &nbsp; &nbsp; &nbsp; "
    let dtString  = m.Date.ToString "yyyy-MM-dd"
    [ div [ _class "pt-center-text" ] [
        br []
        a [ _class "pt-icon-link"
            _href $"/prayer-requests/print/{dtString}"
            _title s["View Printable"].Value
          ] [ icon "print"; rawText " &nbsp;"; locStr s["View Printable"] ]
        if m.CanEmail then
            spacer
            if m.Date.DayOfWeek <> DayOfWeek.Sunday then
                let rec findSunday (date : DateTime) =
                    if date.DayOfWeek = DayOfWeek.Sunday then date else findSunday (date.AddDays 1.)
                let sunday = findSunday m.Date
                a [ _class "pt-icon-link"
                    _href $"""/prayer-requests/view/{sunday.ToString "yyyy-MM-dd"}"""
                    _title s["List for Next Sunday"].Value ] [
                    icon "update"; rawText " &nbsp;"; locStr s["List for Next Sunday"]
                ]
                spacer
            let emailPrompt = s["This will e-mail the current list to every member of your group, without further prompting.  Are you sure this is what you are ready to do?"].Value
            a [ _class "pt-icon-link"
                _href $"/prayer-requests/email/{dtString}"
                _title s["Send via E-mail"].Value
                _onclick $"return PT.requests.view.promptBeforeEmail('{emailPrompt}')" ] [
                icon "mail_outline"; rawText " &nbsp;"; locStr s["Send via E-mail"]
            ]
        spacer
        a [ _class "pt-icon-link"; _href "/prayer-requests"; _title s["Maintain Prayer Requests"].Value ] [
           icon "compare_arrows"; rawText " &nbsp;"; locStr s["Maintain Prayer Requests"]
        ]
      ]
      br []
      rawText (m.AsHtml s)
    ]
    |> Layout.Content.standard
    |> Layout.standard vi pageTitle
