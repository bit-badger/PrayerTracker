/// Layout items for PrayerTracker
module PrayerTracker.Views.Layout

open Giraffe.ViewEngine
open Giraffe.ViewEngine.Accessibility
open PrayerTracker.ViewModels
open System.Globalization

/// Get the two-character language code for the current request
let langCode () = if CultureInfo.CurrentCulture.Name.StartsWith "es" then "es" else "en"


/// Navigation items
module Navigation =
  
    /// Top navigation bar
    let top m =
        let s          = I18N.localizer.Force ()
        let menuSpacer = rawText "&nbsp; "
        let _dropdown  = _class "dropdown-btn"
        let leftLinks = [
            match m.User with
            | Some u ->
                li [ _class "dropdown" ] [
                    a [ _dropdown; _ariaLabel s["Requests"].Value; _title s["Requests"].Value; _roleButton ] [
                        icon "question_answer"; space; locStr s["Requests"]; space; icon "keyboard_arrow_down"
                    ]
                    div [ _class "dropdown-content"; _roleMenuBar ] [
                        a [ _href "/prayer-requests"; _roleMenuItem ] [
                            icon "compare_arrows"; menuSpacer; locStr s["Maintain"]
                        ]
                        a [ _href "/prayer-requests/view"; _roleMenuItem ] [
                            icon "list"; menuSpacer; locStr s["View List"]
                        ]
                    ]
                ]
                li [ _class "dropdown" ] [
                    a [ _dropdown; _ariaLabel s["Group"].Value; _title s["Group"].Value; _roleButton ] [
                        icon "group"; space; locStr s["Group"]; space; icon "keyboard_arrow_down"
                    ]
                    div [ _class "dropdown-content"; _roleMenuBar ] [
                        a [ _href "/small-group/members"; _roleMenuItem ] [
                            icon "email"; menuSpacer; locStr s["Maintain Group Members"]
                        ]
                        a [ _href "/small-group/announcement"; _roleMenuItem ] [
                            icon "send";  menuSpacer; locStr s["Send Announcement"]
                        ]
                        a [ _href "/small-group/preferences"; _roleMenuItem ] [
                            icon "build"; menuSpacer; locStr s["Change Preferences"]
                        ]
                    ]
                ]
                if u.IsAdmin then
                    li [ _class "dropdown" ] [
                        a [ _dropdown
                            _ariaLabel s["Administration"].Value
                            _title     s["Administration"].Value
                            _roleButton ] [
                            icon "settings"; space; locStr s["Administration"]; space; icon "keyboard_arrow_down"
                        ]
                        div [ _class "dropdown-content"; _roleMenuBar ] [
                            a [ _href "/churches"; _roleMenuItem ] [ icon "home";  menuSpacer; locStr s["Churches"] ]
                            a [ _href "/small-groups"; _roleMenuItem ] [ icon "send";  menuSpacer; locStr s["Groups"] ]
                            a [ _href "/users"; _roleMenuItem ] [ icon "build"; menuSpacer; locStr s["Users"] ]
                        ]
                    ]
            | None ->
                match m.Group with
                | Some _ ->
                    li [] [
                        a [ _href      "/prayer-requests/view"
                            _ariaLabel s["View Request List"].Value
                            _title     s["View Request List"].Value ] [
                            icon "list"; space; locStr s["View Request List"]
                        ]
                    ]
                | None ->
                    li [ _class "dropdown" ] [
                        a [ _dropdown; _ariaLabel s["Log On"].Value; _title s["Log On"].Value; _roleButton ] [
                            icon "security"; space; locStr s["Log On"]; space; icon "keyboard_arrow_down"
                        ]
                        div [ _class "dropdown-content"; _roleMenuBar ] [
                            a [ _href "/user/log-on"; _roleMenuItem ] [ icon "person"; menuSpacer; locStr s["User"] ]
                            a [ _href "/small-group/log-on"; _roleMenuItem ] [
                                icon "group";  menuSpacer; locStr s["Group"]
                            ]
                        ]
                    ]
                    li [] [
                        a [ _href      "/prayer-requests/lists"
                            _ariaLabel s["View Request List"].Value
                            _title     s["View Request List"].Value ] [
                            icon "list"; space; locStr s["View Request List"]
                        ]
                    ]
            li [] [
                a [ _href      $"https://docs.prayer.bitbadger.solutions/{langCode ()}"
                    _ariaLabel s["Help"].Value
                    _title     s["View Help"].Value
                    _target    "_blank"
                    _relNoOpener ] [
                    icon "help"; space; locStr s["Help"]
                ]
            ]
        ]
        let rightLinks =
            match m.Group with
            | Some _ ->
                [   match m.User with
                    | Some _ ->
                        li [] [
                            a [ _href      "/user/password"
                                _ariaLabel s["Change Your Password"].Value
                                _title     s["Change Your Password"].Value ] [
                                icon "lock"; space; locStr s["Change Your Password"]
                            ]
                        ]
                    | None -> ()
                    li [] [
                        a [ _href "/log-off"; _ariaLabel s["Log Off"].Value; _title s["Log Off"].Value; Target.body ] [
                            icon "power_settings_new"; space; locStr s["Log Off"]
                        ]
                    ]
                ]
            | None -> []
        header [ _class "pt-title-bar"; Target.content ] [
            section [ _class "pt-title-bar-left"; _ariaLabel "Left side of top menu" ] [
                span [ _class "pt-title-bar-home" ] [
                    a [ _href "/"; _title s["Home"].Value ] [ locStr s["PrayerTracker"] ]
                ]
                ul [] leftLinks
            ]
            section [ _class "pt-title-bar-center"; _ariaLabel "Empty center space in top menu" ] []
            section [ _class "pt-title-bar-right"; _roleToolBar; _ariaLabel "Right side of top menu" ] [
                ul [] rightLinks
            ]
        ]
    
    /// Identity bar (below top nav)
    let identity m =
        let s = I18N.localizer.Force ()
        header [ _id "pt-language"; Target.body ] [
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
            | Some g ->
                [   match m.User with
                    | Some u ->
                        span [ _class "u" ] [ locStr s["Currently Logged On"] ]
                        rawText "&nbsp; &nbsp;"
                        icon "person"
                        strong [] [ str u.Name ]
                        rawText "&nbsp; &nbsp; "
                    | None ->
                        locStr s["Logged On as a Member of"]
                        rawText "&nbsp; "
                    icon "group"
                    space
                    match m.User with
                    | Some _ -> a [ _href "/small-group"; Target.content ] [ strong [] [ str g.Name ] ]
                    | None -> strong [] [ str g.Name ]
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
let private commonHead = [
    meta [ _name "viewport"; _content "width=device-width, initial-scale=1" ]
    meta [ _name "generator"; _content "Giraffe" ]
    link [ _rel "stylesheet"; _href "https://fonts.googleapis.com/icon?family=Material+Icons" ]
    link [ _rel "stylesheet"; _href "/css/app.css" ]
]

/// Render the <head> portion of the page
let private htmlHead viewInfo pgTitle =
    let s = I18N.localizer.Force ()
    head [] [
        meta [ _charset "UTF-8" ]
        title [] [ locStr pgTitle; titleSep; locStr s["PrayerTracker"] ]
        yield! commonHead
        for cssFile in viewInfo.Style do
            link [ _rel "stylesheet"; _href $"/css/{cssFile}.css"; _type "text/css" ]
    ]


open Giraffe.ViewEngine.Htmx

/// Render a link to the help page for the current page
let private helpLink link =
    let s = I18N.localizer.Force ()
    sup [] [
        a [ _href    link
            _title   s["Click for Help on This Page"].Value
            _onclick $"return PT.showHelp('{link}')"
            _hxNoBoost ] [
            icon "help_outline"
        ]
    ]

/// Render the page title, and optionally a help link
let private renderPageTitle viewInfo pgTitle =
    h2 [ _id "pt-page-title" ] [
        match viewInfo.HelpLink with
        | Some link -> PrayerTracker.Utils.Help.fullLink (langCode ()) link |> helpLink
        | None -> ()
        locStr pgTitle
    ]

/// Render the messages that may need to be displayed to the user
let private messages viewInfo =
    let s = I18N.localizer.Force ()
    if List.isEmpty viewInfo.Messages then []
    else
        viewInfo.Messages
        |> List.map (fun msg ->
            div [ _class $"pt-msg {MessageLevel.toCssClass msg.Level}" ] [
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
            ])
        |> div [ _class "pt-messages" ]
        |> List.singleton


open System

/// Render the <footer> at the bottom of the page
let private htmlFooter viewInfo =
    let s          = I18N.localizer.Force ()
    let imgText    = $"""%O{s["PrayerTracker"]} %O{s["from Bit Badger Solutions"]}"""
    let resultTime = TimeSpan(DateTime.Now.Ticks - viewInfo.RequestStart).TotalSeconds
    footer [ _class "pt-footer" ] [
        div [ _id "pt-legal" ] [
            a [ _href "/legal/privacy-policy" ] [ locStr s["Privacy Policy"] ]
            rawText " &nbsp; "
            a [ _href "/legal/terms-of-service" ] [ locStr s["Terms of Service"] ]
            rawText " &nbsp; "
            a [ _href   "https://github.com/bit-badger/PrayerTracker"
                _title  s["View source code and get technical support"].Value
                _target "_blank"
                _relNoOpener ] [
                locStr s["Source & Support"]
            ]
        ]
        div [ _id "pt-footer" ] [
            a [ _href "/"; _style "line-height:28px;" ] [
                img [ _src   $"""/img/%O{s["footer_en"]}.png"""
                      _alt   imgText
                      _title imgText
                      _width "331"; _height "28" ]
            ]
            span [ _id "pt-version" ] [ str viewInfo.Version ]
            space
            i [ _title s["This page loaded in {0:N3} seconds", resultTime].Value; _class "material-icons md-18" ] [
                str "schedule"
            ]
        ]
    ]

/// The content portion of the PrayerTracker layout
let private contentSection viewInfo pgTitle (content : XmlNode) = [
    Navigation.identity viewInfo
    renderPageTitle viewInfo pgTitle
    yield! messages viewInfo
    match viewInfo.ScopedStyle with
    | [] -> ()
    | styles -> style [] [ rawText (styles |> String.concat " ") ]
    content
    htmlFooter viewInfo
    match viewInfo.OnLoadScript with
    | Some onLoad ->
        script [] [
            rawText $"""
                window.doOnLoad = () => {{
                    if (window.PT) {{
                        {onLoad}()
                        delete window.doOnLoad
                    }} else {{ setTimeout(window.doOnLoad, 500) }}
                }}
                window.doOnLoad()"""
        ]
    | None -> ()
]

/// The HTML head element for partial responses
let private partialHead pgTitle =
    let s = I18N.localizer.Force ()
    head [] [
        meta [ _charset "UTF-8" ]
        title [] [ locStr pgTitle; titleSep; locStr s["PrayerTracker"] ]
    ]

open Giraffe.Htmx.Common

/// The body of the PrayerTracker layout
let private pageLayout viewInfo pgTitle content =
    body [ _hxBoost ] [
        Navigation.top viewInfo
        div [ _id "pt-body"; Target.content; _hxSwap $"{HxSwap.InnerHtml} show:window:top" ]
            (contentSection viewInfo pgTitle content)
        match viewInfo.Layout with
        | FullPage ->
            Script.minified
            script [ _src "/js/ckeditor/ckeditor.js" ] []
            script [ _src "/js/app.js" ] []
        | _ -> ()
    ]
    
/// The standard layout(s) for PrayerTracker
let standard viewInfo pageTitle content =
    let s       = I18N.localizer.Force ()
    let pgTitle = s[pageTitle]
    html [ _lang (langCode ()) ] [
        match viewInfo.Layout with
        | FullPage ->
            htmlHead   viewInfo pgTitle
            pageLayout viewInfo pgTitle content
        | PartialPage ->
            partialHead pgTitle
            pageLayout viewInfo pgTitle content
        | ContentOnly ->
            partialHead pgTitle
            body [] (contentSection viewInfo pgTitle content)
    ]

/// A layout with nothing but a title and content
let bare pageTitle content =
    let s = I18N.localizer.Force ()
    html [ _lang (langCode ()) ] [
        partialHead s[pageTitle]
        body [] [ content ]
    ]
