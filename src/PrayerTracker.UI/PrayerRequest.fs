﻿module PrayerTracker.Views.PrayerRequest

open Giraffe
open Giraffe.GiraffeViewEngine
open Microsoft.AspNetCore.Http
open NodaTime
open PrayerTracker
open PrayerTracker.Entities
open PrayerTracker.ViewModels
open System
open System.IO
open System.Text

/// View for the prayer request edit page
let edit (m : EditRequest) today ctx vi =
  let s         = I18N.localizer.Force ()
  let pageTitle = match m.isNew () with true -> "Add a New Request" | false -> "Edit Request"
  [ form [ _action "/prayer-request/save"; _method "post"; _class "pt-center-columns" ] [
      csrfToken ctx
      input [ _type "hidden"; _name "requestId"; _value (m.requestId.ToString "N") ]
      div [ _class "pt-field-row" ] [
        yield div [ _class "pt-field" ] [
          label [ _for "requestType" ] [ encLocText s.["Request Type"] ]
          ReferenceList.requestTypeList s
          |> Seq.ofList
          |> Seq.map (fun item -> fst item, (snd item).Value)
          |> selectList "requestType" m.requestType [ _required; _autofocus ]
          ]
        yield div [ _class "pt-field" ] [
          label [ _for "requestor" ] [ encLocText s.["Requestor / Subject"] ]
          input [ _type "text"
                  _name "requestor"
                  _id "requestor"
                  _value (match m.requestor with Some x -> x | None -> "") ]
          ]
        match m.isNew () with
        | true ->
            yield div [ _class "pt-field" ] [
              label [ _for "enteredDate" ] [ encLocText s.["Date"] ]
              input [ _type "date"; _name "enteredDate"; _id "enteredDate"; _placeholder today ]
              ]
        | false ->
            yield div [ _class "pt-field" ] [
              div [ _class "pt-checkbox-field" ] [
                br []
                input [ _type "checkbox"; _name "skipDateUpdate"; _id "skipDateUpdate"; _value "True" ]
                label [ _for "skipDateUpdate" ] [ encLocText s.["Check to not update the date"] ]
                br []
                small [] [ em [] [ encodedText (s.["Typo Corrections"].Value.ToLower ()); rawText ", etc." ] ]
                ]
              ]
        ]
      div [ _class "pt-field-row" ] [
        div [ _class "pt-field" ] [
          label [] [ encLocText s.["Expiration"] ]
          ReferenceList.expirationList s ((m.isNew >> not) ())
          |> List.map (fun exp ->
              let radioId = sprintf "expiration_%s" (fst exp)
              span [ _class "text-nowrap" ] [
                radio "expiration" radioId (fst exp) m.expiration
                label [ _for radioId ] [ encLocText (snd exp) ]
                rawText " &nbsp; &nbsp; "
                ])
          |> div [ _class "pt-center-text" ]
          ]
        ]
      div [ _class "pt-field-row" ] [
        div [ _class "pt-field pt-editor" ] [
          label [ _for "text" ] [ encLocText s.["Request"] ]
          textarea [ _name "text"; _id "text" ] [ encodedText m.text ]
          ]
        ]
      div [ _class "pt-field-row" ] [ submit [] "save" s.["Save Request"] ]
      ]
    script [] [ rawText "PT.onLoad(PT.initCKEditor)" ]
    ]
  |> Layout.Content.standard
  |> Layout.standard vi pageTitle

/// View for the request e-mail results page
let email m vi =
  let s         = I18N.localizer.Force ()
  let pageTitle = sprintf "%s • %s" s.["Prayer Requests"].Value m.listGroup.name
  let prefs     = m.listGroup.preferences
  let addresses =
    m.recipients
    |> List.fold (fun (acc : StringBuilder) mbr -> acc.AppendFormat(", {0} <{1}>", mbr.memberName, mbr.email))
                 (StringBuilder ())
  [ p [ _style (sprintf "font-family:%s;font-size:%ipt;" prefs.listFonts prefs.textFontSize) ] [
      encLocText s.["The request list was sent to the following people, via individual e-mails"]
      rawText ":"
      br []
      small [] [ encodedText (addresses.Remove(0, 2).ToString ()) ]
      ]
    span [ _class "pt-email-heading" ] [ encLocText s.["HTML Format"]; rawText ":" ]
    div [ _class "pt-email-canvas" ] [ rawText (m.asHtml s) ]
    br []
    br []
    span [ _class "pt-email-heading" ] [ encLocText s.["Plain-Text Format"]; rawText ":" ]
    div[ _class "pt-email-canvas" ] [ pre [] [ encodedText (m.asText s) ] ]
    ]
  |> Layout.Content.standard
  |> Layout.standard vi pageTitle


/// View for a small group's public prayer request list
let list (m : RequestList) vi =
  [ br []
    I18N.localizer.Force () |> (m.asHtml >> rawText) 
    ]
  |> Layout.Content.standard
  |> Layout.standard vi "View Request List"


/// View for the prayer request lists page
let lists (grps : SmallGroup list) vi =
  let s   = I18N.localizer.Force ()
  let l   = I18N.forView "Requests/Lists"
  use sw  = new StringWriter ()
  let raw = rawLocText sw
  [ yield p [] [
      raw l.["The groups listed below have either public or password-protected request lists."]
      space
      raw l.["Those with list icons are public, and those with log on icons are password-protected."]
      space
      raw l.["Click the appropriate icon to log on or view the request list."]
      ]
    match grps.Length with
    | 0 -> yield p [] [ raw l.["There are no groups with public or password-protected request lists."] ]
    | count ->
        yield tableSummary count s
        yield table [ _class "pt-table pt-action-table" ] [
          thead [] [
            tr [] [
              th [] [ encLocText s.["Actions"] ]
              th [] [ encLocText s.["Church"] ]
              th [] [ encLocText s.["Group"] ]
              ]
            ]
          grps
          |> List.map (fun grp ->
              let grpId = grp.smallGroupId.ToString "N"
              tr [] [
                match grp.preferences.isPublic with
                | true ->
                    a [ _href (sprintf "/prayer-requests/%s/list" grpId); _title s.["View"].Value ] [ icon "list" ]
                | false ->
                    a [ _href (sprintf "/small-group/log-on/%s" grpId); _title s.["Log On"].Value ]
                      [ icon "verified_user" ]
                |> List.singleton
                |> td []
                td [] [ encodedText grp.church.name ]
                td [] [ encodedText grp.name ]
                ])
          |> tbody []
          ]
    ]
  |> Layout.Content.standard
  |> Layout.standard vi "Request Lists"


/// View for the prayer request maintenance page
let maintain (reqs : PrayerRequest seq) (grp : SmallGroup) onlyActive (ctx : HttpContext) vi =
  let s    = I18N.localizer.Force ()
  let now  = grp.localDateNow (ctx.GetService<IClock> ())
  let typs = ReferenceList.requestTypeList s |> Map.ofList
  let updReq (req : PrayerRequest) =
    match req.updateRequired now grp.preferences.daysToExpire grp.preferences.longTermUpdateWeeks with
    | true -> "pt-request-update"
    | false -> ""
    |> _class 
  let reqExp (req : PrayerRequest) =
    _class (match req.isExpired now grp.preferences.daysToExpire with true -> "pt-request-expired" | false -> "")
  /// Iterate the sequence once, before we render, so we can get the count of it at the top of the table
  let requests =
    reqs
    |> Seq.map (fun req ->
        let reqId     = req.prayerRequestId.ToString "N"
        let reqText   = Utils.htmlToPlainText req.text
        let delAction = sprintf "/prayer-request/%s/delete" reqId
        let delPrompt = s.["Are you want to delete this prayer request?  This action cannot be undone.\\n(If the prayer request has been answered, or an event has passed, consider inactivating it instead.)"].Value
        tr [] [
          td [] [
            yield a [ _href (sprintf "/prayer-request/%s/edit" reqId); _title s.["Edit This Prayer Request"].Value ]
              [ icon "edit" ]
            match req.isExpired now grp.preferences.daysToExpire with
            | true ->
                yield a [ _href (sprintf "/prayer-request/%s/restore" reqId)
                          _title s.["Restore This Inactive Request"].Value ]
                  [ icon "visibility" ]
            | false ->
                yield a [ _href (sprintf "/prayer-request/%s/expire" reqId)
                          _title s.["Expire This Request Immediately"].Value ]
                  [ icon "visibility_off" ]
            yield a [ _href delAction; _title s.["Delete This Request"].Value;
                      _onclick (sprintf "return PT.confirmDelete('%s','%s')" delAction delPrompt) ]
              [ icon "delete_forever" ]
            ]
          td [ updReq req ] [
            encodedText (req.updatedDate.ToString(s.["MMMM d, yyyy"].Value,
                            System.Globalization.CultureInfo.CurrentUICulture))
            ]
          td [] [ encLocText typs.[req.requestType] ]
          td [ reqExp req ] [ encodedText (match req.requestor with Some r -> r | None -> " ") ]
          td [] [
            yield
              match 60 > reqText.Length with
              | true -> rawText reqText
              | false -> rawText (sprintf "%s&hellip;" (reqText.Substring (0, 60)))
            ]
          ])
    |> List.ofSeq
  [ div [ _class "pt-center-text" ] [
      br []
      a [ _href (sprintf "/prayer-request/%s/edit" emptyGuid); _title s.["Add a New Request"].Value ]
        [ icon "add_circle"; rawText " &nbsp;"; encLocText s.["Add a New Request"] ]
      rawText " &nbsp; &nbsp; &nbsp; "
      a [ _href "/prayer-requests/view"; _title s.["View Prayer Request List"].Value ]
        [ icon "list"; rawText " &nbsp;"; encLocText s.["View Prayer Request List"] ]
      br []
      br []
      ]
    tableSummary requests.Length s
    table [ _class "pt-table pt-action-table" ] [
      thead [] [
        tr [] [
          th [] [ encLocText s.["Actions"] ]
          th [] [ encLocText s.["Updated Date"] ]
          th [] [ encLocText s.["Type"] ]
          th [] [ encLocText s.["Requestor"] ]
          th [] [ encLocText s.["Request"] ]
          ]
        ]
      tbody [] requests
      ]
    div [ _class "pt-center-text" ] [
      yield br []
      match onlyActive with
      | true ->
          yield encLocText s.["Inactive requests are currently not shown"]
          yield br []
          yield a [ _href "/prayer-requests/inactive" ] [ encLocText s.["Show Inactive Requests"] ]
      | false ->
          yield encLocText s.["Inactive requests are currently shown"]
          yield br []
          yield a [ _href "/prayer-requests" ] [ encLocText s.["Do Not Show Inactive Requests"] ]
      ]
    form [ _id "DeleteForm"; _action ""; _method "post" ] [ csrfToken ctx ]
    ]
  |> Layout.Content.wide
  |> Layout.standard vi "Maintain Requests"


/// View for the printable prayer request list
let print m version =
  let s         = I18N.localizer.Force ()
  let pageTitle = sprintf "%s • %s" s.["Prayer Requests"].Value m.listGroup.name
  let imgAlt    = sprintf "%s %s" s.["PrayerTracker"].Value s.["from Bit Badger Solutions"].Value
  article [] [
    rawText (m.asHtml s)
    br []
    hr []
    div [ _style "font-size:70%;font-family:@Model.ListGroup.preferences.listFonts;" ] [
      img [ _src (sprintf "/img/%s.png" s.["footer_en"].Value)
            _style "vertical-align:text-bottom;"
            _alt imgAlt
            _title imgAlt ]
      space
      encodedText version
      ]
    ]
  |> Layout.bare pageTitle


/// View for the prayer request list
let view m vi =
  let s         = I18N.localizer.Force ()
  let pageTitle = sprintf "%s • %s" s.["Prayer Requests"].Value m.listGroup.name
  let spacer    = rawText " &nbsp; &nbsp; &nbsp; "
  let dtString  = m.date.ToString "yyyy-MM-dd"
  [ div [ _class "pt-center-text" ] [
      yield br []
      yield a [ _class "pt-icon-link"
                _href (sprintf "/prayer-requests/print/%s" dtString)
                _title s.["View Printable"].Value ] [
        icon "print"; rawText " &nbsp;"; encLocText s.["View Printable"]
        ]
      match m.canEmail with
      | true ->
          yield spacer
          match m.date.DayOfWeek = DayOfWeek.Sunday with
          | true -> ()
          | false ->
              let rec findSunday (date : DateTime) =
                match date.DayOfWeek = DayOfWeek.Sunday with
                | true -> date
                | false -> findSunday (date.AddDays 1.)
              let sunday = findSunday m.date
              yield a [ _class "pt-icon-link"
                        _href (sprintf "/prayer-requests/view/%s" (sunday.ToString "yyyy-MM-dd"))
                        _title s.["List for Next Sunday"].Value ] [
                icon "update"; rawText " &nbsp;"; encLocText s.["List for Next Sunday"]
                ]
              yield spacer
          let emailPrompt = s.["This will e-mail the current list to every member of your class, without further prompting.  Are you sure this is what you are ready to do?"].Value
          yield a [ _class "pt-icon-link"
                    _href (sprintf "/prayer-requests/email/%s" dtString)
                    _title s.["Send via E-mail"].Value
                    _onclick (sprintf "return PT.requests.view.promptBeforeEmail('%s')" emailPrompt) ] [
            icon "mail_outline"; rawText " &nbsp;"; encLocText s.["Send via E-mail"]
            ]
          yield spacer
          yield a [ _class "pt-icon-link"; _href "/prayer-requests"; _title s.["Maintain Prayer Requests"].Value ] [
            icon "compare_arrows"; rawText " &nbsp;"; encLocText s.["Maintain Prayer Requests"]
            ]
      | false -> ()
      ]
    br []
    rawText (m.asHtml s)
    ]
  |> Layout.Content.standard
  |> Layout.standard vi pageTitle
