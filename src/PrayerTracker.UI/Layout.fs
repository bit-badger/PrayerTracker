/// Layout items for PrayerTracker
module PrayerTracker.Views.Layout

open Giraffe.ViewEngine
open PrayerTracker
open PrayerTracker.ViewModels
open System
open System.Globalization


/// Get the two-character language code for the current request
let langCode () = if CultureInfo.CurrentCulture.Name.StartsWith "es" then "es" else "en"


/// Navigation items
module Navigation =
  
    /// Top navigation bar
    let top m =
        let s = I18N.localizer.Force ()
        let menuSpacer = rawText "&nbsp; "
        let leftLinks = [
            match m.User with
            | Some u ->
                li [ _class "dropdown" ] [
                    a [ _class "dropbtn"
                        _role "button"
                        _aria "label" s["Requests"].Value
                        _title s["Requests"].Value ]
                      [ icon "question_answer"; space; locStr s["Requests"]; space; icon "keyboard_arrow_down" ]
                    div [ _class "dropdown-content"; _role "menu" ] [
                        a [ _href "/prayer-requests" ] [ icon "compare_arrows"; menuSpacer; locStr s["Maintain"] ]
                        a [ _href "/prayer-requests/view" ] [ icon "list"; menuSpacer; locStr s["View List"] ]
                    ]
                ]
                li [ _class "dropdown" ] [
                    a [ _class "dropbtn"; _role "button"; _aria "label" s["Group"].Value; _title s["Group"].Value ]
                      [ icon "group"; space; locStr s["Group"]; space; icon "keyboard_arrow_down" ]
                    div [ _class "dropdown-content"; _role "menu" ] [
                        a [ _href "/small-group/members" ]
                          [ icon "email"; menuSpacer; locStr s["Maintain Group Members"] ]
                        a [ _href "/small-group/announcement" ]
                          [ icon "send";  menuSpacer; locStr s["Send Announcement"] ]
                        a [ _href "/small-group/preferences" ]
                          [ icon "build"; menuSpacer; locStr s["Change Preferences"] ]
                    ]
                ]
                if u.isAdmin then
                    li [ _class "dropdown" ] [
                        a [ _class "dropbtn"
                            _role "button"
                            _aria "label" s["Administration"].Value
                            _title s["Administration"].Value
                          ] [ icon "settings"; space; locStr s["Administration"]; space; icon "keyboard_arrow_down" ]
                        div [ _class "dropdown-content"; _role "menu" ] [
                            a [ _href "/churches" ]     [ icon "home";  menuSpacer; locStr s["Churches"] ]
                            a [ _href "/small-groups" ] [ icon "send";  menuSpacer; locStr s["Groups"] ]
                            a [ _href "/users" ]        [ icon "build"; menuSpacer; locStr s["Users"] ]
                        ]
                    ]
            | None ->
                match m.Group with
                | Some _ ->
                    li [] [
                        a [ _href "/prayer-requests/view"
                            _aria "label" s["View Request List"].Value
                            _title s["View Request List"].Value
                          ] [ icon "list"; space; locStr s["View Request List"] ]
                    ]
                | None ->
                    li [ _class "dropdown" ] [
                        a [ _class "dropbtn"
                            _role "button"
                            _aria "label" s["Log On"].Value
                            _title s["Log On"].Value
                          ] [ icon "security"; space; locStr s["Log On"]; space; icon "keyboard_arrow_down" ]
                        div [ _class "dropdown-content"; _role "menu" ] [
                            a [ _href "/user/log-on" ]        [ icon "person"; menuSpacer; locStr s["User"] ]
                            a [ _href "/small-group/log-on" ] [ icon "group";  menuSpacer; locStr s["Group"] ]
                        ]
                    ]
                    li [] [
                        a [ _href "/prayer-requests/lists"
                            _aria "label" s["View Request List"].Value
                            _title s["View Request List"].Value
                          ] [ icon "list"; space; locStr s["View Request List"] ]
                    ]
            li [] [
                a [ _href   $"https://docs.prayer.bitbadger.solutions/{langCode ()}"
                    _aria   "label" s["Help"].Value;
                    _title  s["View Help"].Value
                    _target "_blank"
                  ] [ icon "help"; space; locStr s["Help"] ]
            ]
        ]
        let rightLinks =
            match m.Group with
            | Some _ -> [
                match m.User with
                | Some _ ->
                    li [] [
                        a [ _href "/user/password"
                            _aria "label" s["Change Your Password"].Value
                            _title s["Change Your Password"].Value
                          ] [ icon "lock"; space; locStr s["Change Your Password"] ]
                    ]
                | None -> ()
                li [] [
                    a [ _href "/log-off"; _aria "label" s["Log Off"].Value; _title s["Log Off"].Value ]
                      [ icon "power_settings_new"; space; locStr s["Log Off"] ]
                ]
              ]
            | None -> []
        header [ _class "pt-title-bar" ] [
            section [ _class "pt-title-bar-left" ] [
                span [ _class "pt-title-bar-home" ] [
                    a [ _href "/"; _title s["Home"].Value ] [ locStr s["PrayerTracker"] ]
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
                span [ _class "u" ] [ locStr s["Language"]; rawText ": " ]
                match langCode () with
                | "es" -> 
                    locStr s["Spanish"]
                    rawText " &nbsp; &bull; &nbsp; "
                    a [ _href "/language/en" ] [ locStr s["Change to English"] ]
                | _ ->
                    locStr s["English"]
                    rawText " &nbsp; &bull; &nbsp; "
                    a [ _href "/language/es" ] [ locStr s["Cambie a Español"] ]
            ]
            match m.Group with
            | Some g ->[
                match m.User with
                | Some u ->
                    span [ _class "u" ] [ locStr s["Currently Logged On"] ]
                    rawText "&nbsp; &nbsp;"
                    icon "person"
                    strong [] [ str u.fullName ]
                    rawText "&nbsp; &nbsp; "
                | None ->
                    locStr s["Logged On as a Member of"]
                    rawText "&nbsp; "
                icon "group"
                space
                match m.User with
                | Some _ -> a [ _href "/small-group" ] [ strong [] [ str g.name ] ]
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

/// Common HTML head tag items
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
        title [] [ locStr pageTitle; titleSep; locStr s["PrayerTracker"] ]
        yield! commonHead
        for cssFile in m.Style do
            link [ _rel "stylesheet"; _href $"/css/{cssFile}.css"; _type "text/css" ]
        for jsFile in m.Script do
            script [ _src $"/js/{jsFile}.js" ] []
    ]
  
/// Render a link to the help page for the current page
let private helpLink link =
    let s = I18N.localizer.Force ()
    sup [] [
        a [ _href link; _title s["Click for Help on This Page"].Value; _onclick $"return PT.showHelp('{link}')" ]
          [ icon "help_outline" ]
    ]

/// Render the page title, and optionally a help link
let private renderPageTitle m pageTitle =
    h2 [ _id "pt-page-title" ] [
        match m.HelpLink with Some link -> Help.fullLink (langCode ()) link |> helpLink | None -> ()
        locStr pageTitle
    ]

/// Render the messages that may need to be displayed to the user
let private messages m =
    let s = I18N.localizer.Force ()
    m.Messages
    |> List.map (fun msg ->
        table [ _class $"pt-msg {MessageLevel.toCssClass msg.Level}" ] [
            tr [] [
                td [] [
                    match msg.Level with
                    | Info -> ()
                    | lvl ->
                        strong [] [ locStr s[MessageLevel.toString lvl] ]
                        rawText " &#xbb; "
                    rawText msg.Text.Value
                    match msg.Description with
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
    let imgText = sprintf "%O %O" s["PrayerTracker"] s["from Bit Badger Solutions"]
    let resultTime = TimeSpan(DateTime.Now.Ticks - m.RequestStart).TotalSeconds
    footer [] [
        div [ _id "pt-legal" ] [
            a [ _href "/legal/privacy-policy" ] [ locStr s["Privacy Policy"] ]
            rawText " &bull; "
            a [ _href "/legal/terms-of-service" ] [ locStr s["Terms of Service"] ]
            rawText " &bull; "
            a [ _href "https://github.com/bit-badger/PrayerTracker"
                _title s["View source code and get technical support"].Value
                _target "_blank"
                _rel "noopener"
              ] [ locStr s["Source & Support"]
            ]
        ]
        div [ _id "pt-footer" ] [
            a [ _href "/"; _style "line-height:28px;" ]
              [ img [ _src $"""/img/%O{s["footer_en"]}.png"""; _alt imgText; _title imgText ] ]
            str m.Version
            space
            i [ _title s["This page loaded in {0:N3} seconds", resultTime].Value; _class "material-icons md-18" ]
              [ str "schedule" ]
        ]
    ]

/// The standard layout for PrayerTracker
let standard m pageTitle (content : XmlNode) =
    let s   = I18N.localizer.Force ()
    let ttl = s[pageTitle]
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
    let ttl = s[pageTitle]
    html [ _lang "" ] [
        head [] [
            meta [ _charset "UTF-8" ]
            title [] [ locStr ttl; titleSep; locStr s["PrayerTracker"] ]
        ]
        body [] [ content ]
    ]
