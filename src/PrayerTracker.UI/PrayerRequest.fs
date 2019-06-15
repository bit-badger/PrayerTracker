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
  [ form [ _action "/web/prayer-request/save"; _method "post"; _class "pt-center-columns" ] [
      csrfToken ctx
      input [ _type "hidden"; _name "requestId"; _value (flatGuid m.requestId) ]
      div [ _class "pt-field-row" ] [
        yield div [ _class "pt-field" ] [
          label [ _for "requestType" ] [ locStr s.["Request Type"] ]
          ReferenceList.requestTypeList s
          |> Seq.ofList
          |> Seq.map (fun (typ, desc) -> typ.code, desc.Value)
          |> selectList "requestType" m.requestType [ _required; _autofocus ]
          ]
        yield div [ _class "pt-field" ] [
          label [ _for "requestor" ] [ locStr s.["Requestor / Subject"] ]
          input [ _type "text"
                  _name "requestor"
                  _id "requestor"
                  _value (match m.requestor with Some x -> x | None -> "") ]
          ]
        match m.isNew () with
        | true ->
            yield div [ _class "pt-field" ] [
              label [ _for "enteredDate" ] [ locStr s.["Date"] ]
              input [ _type "date"; _name "enteredDate"; _id "enteredDate"; _placeholder today ]
              ]
        | false ->
            yield div [ _class "pt-field" ] [
              div [ _class "pt-checkbox-field" ] [
                br []
                input [ _type "checkbox"; _name "skipDateUpdate"; _id "skipDateUpdate"; _value "True" ]
                label [ _for "skipDateUpdate" ] [ locStr s.["Check to not update the date"] ]
                br []
                small [] [ em [] [ str (s.["Typo Corrections"].Value.ToLower ()); rawText ", etc." ] ]
                ]
              ]
        ]
      div [ _class "pt-field-row" ] [
        div [ _class "pt-field" ] [
          label [] [ locStr s.["Expiration"] ]
          ReferenceList.expirationList s ((m.isNew >> not) ())
          |> List.map (fun exp ->
              let radioId = sprintf "expiration_%s" (fst exp)
              span [ _class "text-nowrap" ] [
                radio "expiration" radioId (fst exp) m.expiration
                label [ _for radioId ] [ locStr (snd exp) ]
                rawText " &nbsp; &nbsp; "
                ])
          |> div [ _class "pt-center-text" ]
          ]
        ]
      div [ _class "pt-field-row" ] [
        div [ _class "pt-field pt-editor" ] [
          label [ _for "text" ] [ locStr s.["Request"] ]
          textarea [ _name "text"; _id "text" ] [ str m.text ]
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
      locStr s.["The request list was sent to the following people, via individual e-mails"]
      rawText ":"
      br []
      small [] [ str (addresses.Remove(0, 2).ToString ()) ]
      ]
    span [ _class "pt-email-heading" ] [ locStr s.["HTML Format"]; rawText ":" ]
    div [ _class "pt-email-canvas" ] [ rawText (m.asHtml s) ]
    br []
    br []
    span [ _class "pt-email-heading" ] [ locStr s.["Plain-Text Format"]; rawText ":" ]
    div[ _class "pt-email-canvas" ] [ pre [] [ str (m.asText s) ] ]
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
              th [] [ locStr s.["Actions"] ]
              th [] [ locStr s.["Church"] ]
              th [] [ locStr s.["Group"] ]
              ]
            ]
          grps
          |> List.map (fun grp ->
              let grpId = flatGuid grp.smallGroupId
              tr [] [
                match grp.preferences.isPublic with
                | true ->
                    a [ _href (sprintf "/web/prayer-requests/%s/list" grpId); _title s.["View"].Value ] [ icon "list" ]
                | false ->
                    a [ _href (sprintf "/web/small-group/log-on/%s" grpId); _title s.["Log On"].Value ]
                      [ icon "verified_user" ]
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
let maintain m (ctx : HttpContext) vi =
  let s    = I18N.localizer.Force ()
  let l    = I18N.forView "Requests/Maintain"
  use sw   = new StringWriter ()
  let raw  = rawLocText sw
  let now  = m.smallGroup.localDateNow (ctx.GetService<IClock> ())
  let typs = ReferenceList.requestTypeList s |> Map.ofList
  let updReq (req : PrayerRequest) =
    match req.updateRequired now m.smallGroup.preferences.daysToExpire m.smallGroup.preferences.longTermUpdateWeeks with
    | true -> "pt-request-update"
    | false -> ""
    |> _class 
  let reqExp (req : PrayerRequest) =
    _class (match req.isExpired now m.smallGroup.preferences.daysToExpire with true -> "pt-request-expired" | false -> "")
  /// Iterate the sequence once, before we render, so we can get the count of it at the top of the table
  let requests =
    m.requests
    |> Seq.map (fun req ->
        let reqId     = flatGuid req.prayerRequestId
        let reqText   = Utils.htmlToPlainText req.text
        let delAction = sprintf "/web/prayer-request/%s/delete" reqId
        let delPrompt =
          [ s.["Are you sure you want to delete this {0}?  This action cannot be undone.",
                s.["Prayer Request"].Value.ToLower() ]
              .Value
            "\\n"
            l.["(If the prayer request has been answered, or an event has passed, consider inactivating it instead.)"]
              .Value
            ]
          |> String.concat ""
        tr [] [
          td [] [
            yield a [ _href (sprintf "/web/prayer-request/%s/edit" reqId); _title l.["Edit This Prayer Request"].Value ]
              [ icon "edit" ]
            match req.isExpired now m.smallGroup.preferences.daysToExpire with
            | true ->
                yield a [ _href (sprintf "/web/prayer-request/%s/restore" reqId)
                          _title l.["Restore This Inactive Request"].Value ]
                  [ icon "visibility" ]
            | false ->
                yield a [ _href (sprintf "/web/prayer-request/%s/expire" reqId)
                          _title l.["Expire This Request Immediately"].Value ]
                  [ icon "visibility_off" ]
            yield a [ _href delAction; _title l.["Delete This Request"].Value;
                      _onclick (sprintf "return PT.confirmDelete('%s','%s')" delAction delPrompt) ]
              [ icon "delete_forever" ]
            ]
          td [ updReq req ] [
            str (req.updatedDate.ToString(s.["MMMM d, yyyy"].Value, System.Globalization.CultureInfo.CurrentUICulture))
            ]
          td [] [ locStr typs.[req.requestType] ]
          td [ reqExp req ] [ str (match req.requestor with Some r -> r | None -> " ") ]
          td [] [
            yield
              match 60 > reqText.Length with
              | true -> rawText reqText
              | false -> rawText (sprintf "%s&hellip;" reqText.[0..59])
            ]
          ])
    |> List.ofSeq
  [ yield div [ _class "pt-center-text" ] [
      yield br []
      yield a [ _href (sprintf "/web/prayer-request/%s/edit" emptyGuid); _title s.["Add a New Request"].Value ]
        [ icon "add_circle"; rawText " &nbsp;"; locStr s.["Add a New Request"] ]
      yield rawText " &nbsp; &nbsp; &nbsp; "
      yield a [ _href "/web/prayer-requests/view"; _title s.["View Prayer Request List"].Value ]
        [ icon "list"; rawText " &nbsp;"; locStr s.["View Prayer Request List"] ]
      match m.searchTerm with
      | Some _ ->
          yield rawText " &nbsp; &nbsp; &nbsp; "
          yield a [ _href "/web/prayer-requests"; _title l.["Clear Search Criteria"].Value ]
            [ icon "highlight_off"; rawText " &nbsp;"; raw l.["Clear Search Criteria"] ]
      | None -> ()
      ]
    yield form [ _action "/web/prayer-requests"; _method "get"; _class "pt-center-text pt-search-form" ] [
      input [ _type "text"
              _name "search"
              _placeholder l.["Search requests..."].Value
              _value (defaultArg m.searchTerm "")
              ]
      space
      submit [] "search" s.["Search"]
      ]
    yield br []
    yield tableSummary requests.Length s
    match requests.Length with
    | 0 -> ()
    | _ ->
        yield table [ _class "pt-table pt-action-table" ] [
          thead [] [
            tr [] [
              th [] [ locStr s.["Actions"] ]
              th [] [ locStr s.["Updated Date"] ]
              th [] [ locStr s.["Type"] ]
              th [] [ locStr s.["Requestor"] ]
              th [] [ locStr s.["Request"] ]
              ]
            ]
          tbody [] requests
          ]
    yield div [ _class "pt-center-text" ] [
      yield br []
      match m.onlyActive with
      | Some true ->
          yield raw l.["Inactive requests are currently not shown"]
          yield br []
          yield a [ _href "/web/prayer-requests/inactive" ] [ raw l.["Show Inactive Requests"] ]
      | _ ->
          match Option.isSome m.onlyActive with
          | true ->
              yield raw l.["Inactive requests are currently shown"]
              yield br []
              yield a [ _href "/web/prayer-requests" ] [ raw l.["Do Not Show Inactive Requests"] ]
              yield br []
              yield br []
          | false -> ()
          let srch = [ match m.searchTerm with Some s -> yield "search", s | None -> () ]
          let url  = match m.onlyActive with Some true | None -> "" | _ -> "/inactive" |> sprintf "/web/prayer-requests%s"
          let pg   = defaultArg m.pageNbr 1
          match pg with
          | 1 -> ()
          | _ ->
              // button (_type "submit" :: attrs) [ icon ico; rawText " &nbsp;"; locStr text ]
              let withPage = match pg with 2 -> srch | _ -> ("page", string (pg - 1)) :: srch
              yield a [ _href (makeUrl url withPage) ]
                [ icon "keyboard_arrow_left"; space; raw l.["Previous Page"] ]
          yield rawText " &nbsp; &nbsp; "
          match requests.Length = m.smallGroup.preferences.pageSize with
          | true ->
              yield a [ _href (makeUrl url (("page", string (pg + 1)) :: srch)) ]
                [ raw l.["Next Page"]; space; icon "keyboard_arrow_right" ]
          | false -> ()
      ]
    yield form [ _id "DeleteForm"; _action ""; _method "post" ] [ csrfToken ctx ]
    ]
  |> Layout.Content.wide
  |> Layout.standard vi (match m.searchTerm with Some _ -> "Search Results" | None -> "Maintain Requests")


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
      str version
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
                _href (sprintf "/web/prayer-requests/print/%s" dtString)
                _title s.["View Printable"].Value ] [
        icon "print"; rawText " &nbsp;"; locStr s.["View Printable"]
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
                        _href (sprintf "/web/prayer-requests/view/%s" (sunday.ToString "yyyy-MM-dd"))
                        _title s.["List for Next Sunday"].Value ] [
                icon "update"; rawText " &nbsp;"; locStr s.["List for Next Sunday"]
                ]
              yield spacer
          let emailPrompt = s.["This will e-mail the current list to every member of your group, without further prompting.  Are you sure this is what you are ready to do?"].Value
          yield a [ _class "pt-icon-link"
                    _href (sprintf "/web/prayer-requests/email/%s" dtString)
                    _title s.["Send via E-mail"].Value
                    _onclick (sprintf "return PT.requests.view.promptBeforeEmail('%s')" emailPrompt) ] [
            icon "mail_outline"; rawText " &nbsp;"; locStr s.["Send via E-mail"]
            ]
          yield spacer
          yield a [ _class "pt-icon-link"; _href "/web/prayer-requests"; _title s.["Maintain Prayer Requests"].Value ] [
            icon "compare_arrows"; rawText " &nbsp;"; locStr s.["Maintain Prayer Requests"]
            ]
      | false -> ()
      ]
    br []
    rawText (m.asHtml s)
    ]
  |> Layout.Content.standard
  |> Layout.standard vi pageTitle
