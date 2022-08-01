/// Views associated with the home page, or those that don't fit anywhere else
module PrayerTracker.Views.Home

open System.IO
open Giraffe.ViewEngine
open PrayerTracker.ViewModels

/// The error page
let error code viewInfo =
    let s   = I18N.localizer.Force ()
    let l   = I18N.forView "Home/Error"
    use sw  = new StringWriter ()
    let raw = rawLocText sw
    let is404 = "404" = code
    let pageTitle = if is404 then "Page Not Found" else "Server Error"
    [   yield!
            if is404 then
                [   p [] [
                        raw l["The page you requested cannot be found."]
                        raw l["Please use your &ldquo;Back&rdquo; button to return to {0}.", s["PrayerTracker"]]
                    ]
                    p [] [
                        raw l["If you reached this page from a link within {0}, please copy the link from the browser's address bar, and send it to support, along with the group for which you were currently authenticated (if any).",
                            s["PrayerTracker"]]
                    ]
                ]
            else
                [   p [] [
                        raw l["An error ({0}) has occurred.", code]
                        raw l["Please use your &ldquo;Back&rdquo; button to return to {0}.", s["PrayerTracker"]]
                    ]
                ]
        br []
        hr []
        div [ _style "font-size:70%;font-family:-apple-system,BlinkMacSystemFont,'Segoe UI',Roboto,Oxygen-Sans,Ubuntu,Cantarell,'Helvetica Neue',sans-serif" ] [
            img [ _src   $"""/img/%A{s["footer_en"]}.png"""
                  _alt   $"""%A{s["PrayerTracker"]} %A{s["from Bit Badger Solutions"]}"""
                  _title $"""%A{s["PrayerTracker"]} %A{s["from Bit Badger Solutions"]}"""
                  _style "vertical-align:text-bottom;" ]
            str viewInfo.Version
        ]
    ]
    |> div []
    |> Layout.bare pageTitle


/// The home page
let index viewInfo =
    let s   = I18N.localizer.Force ()
    let l   = I18N.forView "Home/Index"
    use sw  = new StringWriter ()
    let raw = rawLocText sw
    [   p [] [
            raw l["Welcome to <strong>{0}</strong>!", s["PrayerTracker"]]
            space
            raw l["{0} is an interactive website that provides churches, Sunday School classes, and other organizations an easy way to keep up with their prayer requests.",
                s["PrayerTracker"]]
            space
            raw l["It is provided at no charge, as a ministry and a community service."]
        ]
        h4 [] [ raw l["What Does It Do?"] ]
        p [] [
            raw l["{0} has what you need to make maintaining a prayer request list a breeze.", s["PrayerTracker"]]
            space
            raw l["Some of the things it can do..."]
        ]
        ul [] [
            li [] [
                raw l["It drops old requests off the list automatically."]
                space
                raw l["Requests other than “{0}” requests will expire at 14 days, though this can be changed by the organization.",
                    s["Long-Term Requests"]]
                space
                raw l["This expiration is based on the last update, not the initial request."]
                space
                raw l["(And, once requests do “drop off”, they are not gone - they may be recovered if needed.)"]
            ]
            li [] [
                raw l["Requests can be viewed any time."]
                space
                raw l["Lists can be made public, or they can be secured with a password, if desired."]
            ]
            li [] [
                raw l["Lists can be e-mailed to a pre-defined list of members."]
                space
                raw l["This can be useful for folks who may not be able to write down all the requests during class, but want a list so that they can pray for them the rest of week."]
                space
                raw l["E-mails are sent individually to each person, which keeps the e-mail list private and keeps the messages from being flagged as spam."]
            ]
            li [] [
                raw l["The look and feel of the list can be configured for each group."]
                space
                raw l["All fonts, colors, and sizes can be customized."]
                space
                raw l["This allows for configuration of large-print lists, among other things."]
            ]
        ]
        h4 [] [ raw l["How Can Your Organization Use {0}?", s["PrayerTracker"]] ]
        p [] [
            raw l["Like God’s gift of salvation, {0} is free for the asking for any church, Sunday School class, or other organization who wishes to use it.",
                s["PrayerTracker"]]
            space
            raw l["If your organization would like to get set up, just <a href=\"mailto:daniel@djs-consulting.com?subject=New%20{0}%20Class\">e-mail</a> Daniel and let him know.",
                s["PrayerTracker"]]
        ]
        h4 [] [ raw l["Do I Have to Register to See the Requests?"] ]
        p [] [
            raw l["This depends on the group."]
            space
            raw l["Lists can be configured to be password-protected, but they do not have to be."]
            space
            raw l["If you click on the “{0}” link above, you will see a list of groups - those that do not indicate that they require logging in are publicly viewable.",
                s["View Request List"]]
        ]
        h4 [] [ raw l["How Does It Work?"] ]
        p [] [
            raw l["Check out the “{0}” link above - it details each of the processes and how they work.", s["Help"]]
        ]
    ]
    |> Layout.Content.standard
    |> Layout.standard viewInfo "Welcome!"


/// Privacy Policy page
let privacyPolicy viewInfo =
    let s   = I18N.localizer.Force ()
    let l   = I18N.forView "Home/PrivacyPolicy"
    use sw  = new StringWriter ()
    let raw = rawLocText sw
    [   p [ _class "pt-right-text" ] [ small [] [ em [] [ raw l["(as of July 31, 2018)"] ] ] ]
        p [] [
            raw l["The nature of the service is one where privacy is a must."]
            space
            raw l["The items below will help you understand the data we collect, access, and store on your behalf as you use this service."]
        ]
        h3 [] [ raw l["What We Collect"] ]
        ul [] [
            li [] [
                strong [] [ raw l["Identifying Data"] ]
                rawText " &ndash; "
                raw l["{0} stores the first and last names, e-mail addresses, and hashed passwords of all authorized users.",
                    s["PrayerTracker"]]
                space
                raw l["Users are also associated with one or more small groups."]
            ]
            li [] [
                strong [] [ raw l["User Provided Data"] ]
                rawText " &ndash; "
                raw l["{0} stores the text of prayer requests.", s["PrayerTracker"]]
                space
                raw l["It also stores names and e-mail addresses of small group members, and plain-text passwords for small groups with password-protected lists."]
            ]
        ]
        h3 [] [ raw l["How Your Data Is Accessed / Secured"] ]
        ul [] [
            li [] [
                raw l["While you are signed in, {0} utilizes a session cookie, and transmits that cookie to the server to establish your identity.",
                    s["PrayerTracker"]]
                space
                raw l["If you utilize the “{0}” box on sign in, a second cookie is stored, and transmitted to establish a session; this cookie is removed by clicking the “{1}” link.",
                    s["Remember Me"], s["Log Off"]]
                space
                raw l["Both of these cookies are encrypted, both in your browser and in transit."]
                space
                raw l["Finally, a third cookie is used to maintain your currently selected language, so that this selection is maintained across browser sessions."]
            ]
            li [] [
                raw l["Data for your small group is returned to you, as required, to display and edit."]
                space
                raw l["{0} also sends e-mails on behalf of the configured owner of a small group; these e-mails are sent from prayer@djs-consulting.com, with the “Reply To” header set to the configured owner of the small group.",
                    s["PrayerTracker"]]
                space
                raw l["Distinct e-mails are sent to each user, as to not disclose the other recipients."]
                space
                raw l["On the server, all data is stored in a controlled-access database."]
            ]
            li [] [
                raw l["Your data is backed up, along with other Bit Badger Solutions hosted systems, in a rolling manner; backups are preserved for the prior 7 days, and backups from the 1st and 15th are preserved for 3 months."]
                space
                raw l["These backups are stored in a private cloud data repository."]
            ]
            li [] [
                raw l["Access to servers and backups is strictly controlled and monitored for unauthorized access attempts."]
            ]
        ]
        h3 [] [ raw l["Removing Your Data"] ]
        p [] [
            raw l["At any time, you may choose to discontinue using {0}; just e-mail Daniel, as you did to register, and request deletion of your small group.",
                s["PrayerTracker"]]
        ]
    ]
    |> Layout.Content.standard
    |> Layout.standard viewInfo "Privacy Policy"


/// Terms of Service page
let termsOfService viewInfo =
    let s      = I18N.localizer.Force ()
    let l      = I18N.forView "Home/TermsOfService"
    use sw     = new StringWriter ()
    let raw    = rawLocText sw
    let ppLink =
        a [ _href "/legal/privacy-policy" ] [ str (s["Privacy Policy"].Value.ToLower ()) ]
        |> renderHtmlString

    [   p [ _class "pt-right-text" ] [ small [] [ em [] [ raw l["(as of May 24, 2018)"] ] ] ]
        h3 [] [ str "1. "; raw l["Acceptance of Terms"] ]
        p [] [
            raw l["By accessing this web site, you are agreeing to be bound by these Terms and Conditions, and that you are responsible to ensure that your use of this site complies with all applicable laws."]
            space
            raw l["Your continued use of this site implies your acceptance of these terms."]
        ]
        h3 [] [ str "2. "; raw l["Description of Service and Registration"] ]
        p [] [
            raw l["{0} is a service that allows individuals to enter and amend prayer requests on behalf of organizations.",
                s["PrayerTracker"]]
            space
            raw l["Registration is accomplished via e-mail to Daniel Summers (daniel at bitbadger dot solutions, substituting punctuation)."]
            space
            raw l["See our {0} for details on the personal (user) information we maintain.", ppLink]
        ]
        h3 [] [ str "3. "; raw l["Liability"] ]
        p [] [
            raw l["This service is provided “as is”, and no warranty (express or implied) exists."]
            space
            raw l["The service and its developers may not be held liable for any damages that may arise through the use of this service."]
        ]
        h3 [] [ str "4. "; raw l["Updates to Terms"] ]
        p [] [
            raw l["These terms and conditions may be updated at any time."]
            space
            raw l["When these terms are updated, users will be notified by a system-generated announcement."]
            space
            raw l["Additionally, the date at the top of this page will be updated."]
        ]
        hr []
        p [] [ raw l["You may also wish to review our {0} to learn how we handle your data.", ppLink] ]
    ]
    |> Layout.Content.standard
    |> Layout.standard viewInfo "Terms of Service"


/// View for unauthorized page
let unauthorized viewInfo =
    let s   = I18N.localizer.Force ()
    let l   = I18N.forView "Home/Unauthorized"
    use sw  = new StringWriter ()
    let raw = rawLocText sw
    [   p [] [
            raw l["If you feel you have reached this page in error, please <a href=\"mailto:daniel@djs-consulting.com?Subject={0}%20Unauthorized%20Access\">contact Daniel</a> and provide the details as to what you were doing (i.e., what link did you click, where had you been, etc.).",
                s["PrayerTracker"]]
        ]
        p [] [
            raw l["Otherwise, you may select one of the links above to get back into an authorized portion of {0}.",
                s["PrayerTracker"]]
        ]
    ]
    |> Layout.Content.standard
    |> Layout.standard viewInfo "Unauthorized Access"
