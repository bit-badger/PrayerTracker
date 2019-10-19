/// Layout items for PrayerTracker
module PrayerTracker.Views.Layout

open Giraffe.GiraffeViewEngine
open PrayerTracker
open PrayerTracker.ViewModels
open System
open System.Globalization
  

/// Get the two-character language code for the current request
let langCode () = match CultureInfo.CurrentCulture.Name.StartsWith "es" with true -> "es" | _ -> "en"


/// Navigation items
module Navigation =
  
  /// Top navigation bar
  let top m =
    let s = PrayerTracker.Views.I18N.localizer.Force ()
    let menuSpacer = rawText "&nbsp; "
    let leftLinks = [
      match m.user with
      | Some u ->
          li [ _class "dropdown" ] [
            a [ _class "dropbtn"; _role "button"; _aria "label" s.["Requests"].Value; _title s.["Requests"].Value ]
              [ icon "question_answer"; space; locStr s.["Requests"]; space; icon "keyboard_arrow_down" ]
            div [ _class "dropdown-content"; _role "menu" ] [
              a [ _href "/web/prayer-requests" ]      [ icon "compare_arrows"; menuSpacer; locStr s.["Maintain"] ]
              a [ _href "/web/prayer-requests/view" ] [ icon "list";           menuSpacer; locStr s.["View List"] ]
              ]
            ]
          li [ _class "dropdown" ] [
            a [ _class "dropbtn"; _role "button"; _aria "label" s.["Group"].Value; _title s.["Group"].Value ]
              [ icon "group"; space; locStr s.["Group"]; space; icon "keyboard_arrow_down" ]
            div [ _class "dropdown-content"; _role "menu" ] [
              a [ _href "/web/small-group/members" ]      [ icon "email"; menuSpacer; locStr s.["Maintain Group Members"] ]
              a [ _href "/web/small-group/announcement" ] [ icon "send";  menuSpacer; locStr s.["Send Announcement"] ]
              a [ _href "/web/small-group/preferences" ]  [ icon "build"; menuSpacer; locStr s.["Change Preferences"] ]
              ]
            ]
          match u.isAdmin with
          | true ->
              li [ _class "dropdown" ] [
                a [ _class "dropbtn"; _role "button"; _aria "label" s.["Administration"].Value; _title s.["Administration"].Value ]
                  [ icon "settings"; space; locStr s.["Administration"]; space; icon "keyboard_arrow_down" ]
                div [ _class "dropdown-content"; _role "menu" ] [
                  a [ _href "/web/churches" ]     [ icon "home";  menuSpacer; locStr s.["Churches"] ]
                  a [ _href "/web/small-groups" ] [ icon "send";  menuSpacer; locStr s.["Groups"] ]
                  a [ _href "/web/users" ]        [ icon "build"; menuSpacer; locStr s.["Users"] ]
                  ]
                ]
          | false -> ()
      | None ->
          match m.group with
          | Some _ ->
              li [] [
                a [ _href "/web/prayer-requests/view"
                    _aria "label" s.["View Request List"].Value
                    _title s.["View Request List"].Value ]
                  [ icon "list"; space; locStr s.["View Request List"] ]
                ]
          | None ->
              li [ _class "dropdown" ] [
                a [ _class "dropbtn"; _role "button"; _aria "label" s.["Log On"].Value; _title s.["Log On"].Value ]
                  [ icon "security"; space; locStr s.["Log On"]; space; icon "keyboard_arrow_down" ]
                div [ _class "dropdown-content"; _role "menu" ] [
                  a [ _href "/web/user/log-on" ]        [ icon "person"; menuSpacer; locStr s.["User"] ]
                  a [ _href "/web/small-group/log-on" ] [ icon "group";  menuSpacer; locStr s.["Group"] ]
                  ]
                ]
              li [] [
                a [ _href "/web/prayer-requests/lists"
                    _aria "label" s.["View Request List"].Value
                    _title s.["View Request List"].Value ]
                  [ icon "list"; space; locStr s.["View Request List"] ]
                ]
      li [] [
        a [ _href   (sprintf "https://docs.prayer.bitbadger.solutions/%s" <| langCode ())
            _aria   "label" s.["Help"].Value;
            _title  s.["View Help"].Value
            _target "_blank"
            ]
          [ icon "help"; space; locStr s.["Help"] ]
        ]
      ]
    let rightLinks =
      match m.group with
      | Some _ ->
          [ match m.user with
            | Some _ ->
                li [] [
                  a [ _href "/web/user/password"
                      _aria "label" s.["Change Your Password"].Value
                      _title s.["Change Your Password"].Value ]
                    [ icon "lock"; space; locStr s.["Change Your Password"] ]
                  ]
            | None -> ()
            li [] [
              a [ _href "/web/log-off"; _aria "label" s.["Log Off"].Value; _title s.["Log Off"].Value ]
                [ icon "power_settings_new"; space; locStr s.["Log Off"] ]
              ]
            ]
      | None -> List.empty
    header [ _class "pt-title-bar" ] [
      section [ _class "pt-title-bar-left" ] [
        span [ _class "pt-title-bar-home" ] [
          a [ _href "/web/"; _title s.["Home"].Value ] [ locStr s.["PrayerTracker"] ]
          ]
        ul [] leftLinks
        ]
      section [ _class "pt-title-bar-center" ] []
      section [ _class "pt-title-bar-right"; _role "toolbar" ] [
        ul [] rightLinks
        ]
      ]
  
  /// Identity bar (below top nav)
  let identity m =
    let s = I18N.localizer.Force ()
    header [ _id "pt-language" ] [
      div [] [
        span [ _class "u" ] [ locStr s.["Language"]; rawText ": " ]
        match langCode () with
        | "es" -> 
            locStr s.["Spanish"]
            rawText " &nbsp; &bull; &nbsp; "
            a [ _href "/web/language/en" ] [ locStr s.["Change to English"] ]
        | _ ->
            locStr s.["English"]
            rawText " &nbsp; &bull; &nbsp; "
            a [ _href "/web/language/es" ] [ locStr s.["Cambie a Español"] ]
        ]
      match m.group with
      | Some g ->
          [ match m.user with
            | Some u ->
                span [ _class "u" ] [ locStr s.["Currently Logged On"] ]
                rawText "&nbsp; &nbsp;"
                icon "person"
                strong [] [ str u.fullName ]
                rawText "&nbsp; &nbsp; "
            | None ->
                locStr s.["Logged On as a Member of"]
                rawText "&nbsp; "
            icon "group"
            space
            match m.user with
            | Some _ -> a [ _href "/web/small-group" ] [ strong [] [ str g.name ] ]
            | None -> strong [] [ str g.name ]
            rawText " &nbsp;"
            ]
      | None -> []
      |> div []
      ]


/// Content layouts
module Content =
  /// Content layout that tops at 60rem
  let standard = div [ _class "pt-content" ]

  /// Content layout that uses the full width of the browser window
  let wide = div [ _class "pt-content pt-full-width" ]

  
/// Separator for parts of the title
let private titleSep = rawText " &#xab; "

let private commonHead =
  [ meta [ _name "viewport"; _content "width=device-width, initial-scale=1" ]
    meta [ _name "generator"; _content "Giraffe" ]
    link [ _rel "stylesheet"; _href "https://fonts.googleapis.com/icon?family=Material+Icons" ]
    link [ _rel "stylesheet"; _href "/css/app.css" ]
    script [ _src "/js/app.js" ] []
    ]

/// Render the <head> portion of the page
let private htmlHead m pageTitle =
  let s = I18N.localizer.Force ()
  head [] [
    meta [ _charset "UTF-8" ]
    title [] [ locStr pageTitle; titleSep; locStr s.["PrayerTracker"] ]
    yield! commonHead
    for cssFile in m.style do
      link [ _rel "stylesheet"; _href (sprintf "/css/%s.css" cssFile); _type "text/css" ]
    for jsFile in m.script do
      script [ _src (sprintf "/js/%s.js" jsFile) ] []
    ]
  
/// Render a link to the help page for the current page
let private helpLink link =
  let s = I18N.localizer.Force ()
  sup [] [
    a [ _href link
        _title s.["Click for Help on This Page"].Value
        _onclick (sprintf "return PT.showHelp('%s')" link) ] [
      icon "help_outline"
      ]
    ]

/// Render the page title, and optionally a help link
let private renderPageTitle m pageTitle =
  h2 [ _id "pt-page-title" ] [
    match m.helpLink with Some link -> Help.fullLink (langCode ()) link |> helpLink | None -> ()
    locStr pageTitle
    ]

/// Render the messages that may need to be displayed to the user
let private messages m =
  let s = I18N.localizer.Force ()
  m.messages
  |> List.map (fun msg ->
      table [ _class (sprintf "pt-msg %s" (msg.level.ToLower ())) ] [
        tr [] [
          td [] [
            match msg.level with
            | "Info" -> ()
            | lvl ->
                strong [] [ locStr s.[lvl] ]
                rawText " &#xbb; "
            rawText msg.text.Value
            match msg.description with
            | Some desc ->
                br []
                div [ _class "description" ] [ rawText desc.Value ]
            | None -> ()
            ]
          ]
        ])

/// Render the <footer> at the bottom of the page
let private htmlFooter m =
  let s = I18N.localizer.Force ()
  let imgText = sprintf "%O %O" s.["PrayerTracker"] s.["from Bit Badger Solutions"]
  let resultTime = TimeSpan(DateTime.Now.Ticks - m.requestStart).TotalSeconds
  footer [] [
    div [ _id "pt-legal" ] [
      a [ _href "/web/legal/privacy-policy" ] [ locStr s.["Privacy Policy"] ]
      rawText " &bull; "
      a [ _href "/web/legal/terms-of-service" ] [ locStr s.["Terms of Service"] ]
      rawText " &bull; "
      a [ _href "https://github.com/bit-badger/PrayerTracker"
          _title s.["View source code and get technical support"].Value
          _target "_blank"
          _rel "noopener" ] [
        locStr s.["Source & Support"]
        ]
      ]
    div [ _id "pt-footer" ] [
      a [ _href "/web/"; _style "line-height:28px;" ] [
        img [ _src (sprintf "/img/%O.png" s.["footer_en"]); _alt imgText; _title imgText ]
        ]
      str m.version
      space
      i [ _title s.["This page loaded in {0:N3} seconds", resultTime].Value; _class "material-icons md-18" ] [
        str "schedule"
        ]
      ]
    ]

/// The standard layout for PrayerTracker
let standard m pageTitle (content : XmlNode) =
  let s   = I18N.localizer.Force ()
  let ttl = s.[pageTitle]
  html [ _lang "" ] [
    htmlHead m ttl
    body [] [
      Navigation.top m
      div [ _id "pt-body" ] [
        Navigation.identity m
        renderPageTitle m ttl
        yield! messages m
        content
        htmlFooter m
        ]
      ]
    ]
  
/// A layout with nothing but a title and content
let bare pageTitle content =
  let s   = I18N.localizer.Force ()
  let ttl = s.[pageTitle]
  html [ _lang "" ] [
    head [] [
      meta [ _charset "UTF-8" ]
      title [] [ locStr ttl; titleSep; locStr s.["PrayerTracker"] ]
      ]
    body [] [ content ]
    ]
