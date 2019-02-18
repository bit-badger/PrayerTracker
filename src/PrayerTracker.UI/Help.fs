module PrayerTracker.Views.Help

open Giraffe.GiraffeViewEngine
open Microsoft.AspNetCore.Html
open PrayerTracker
open System.IO


/// View for the add/edit request help page
let editRequest () =
  let s   = I18N.localizer.Force ()
  let l   = I18N.forView "Help/Requests/Edit"
  use sw  = new StringWriter ()
  let raw = rawLocText sw
  [ p [] [
      raw l.["This page allows you to enter or update a new prayer request."]
      ]
    p [] [
      strong [] [ encLocText s.["Request Type"] ]
      br []
      raw l.["There are 5 request types in {0}.", s.["PrayerTracker"]]
      space
      raw l.["“{0}” are your regular requests that people may have regarding things happening over the next week or so.",
              s.["Current Requests"]]
      space
      raw l.["“{0}” are requests that may occur repeatedly or continue indefinitely.", s.["Long-Term Requests"]]
      space
      raw l.["“{0}” are like “{1}”, but they are answers to prayer to share with your group.",
              s.["Praise Reports"], s.["Current Requests"]]
      space
      raw l.["“{0}” is for those who are pregnant.", s.["Expecting"]]
      space
      raw l.["“{0}” are like “{1}”, but instead of a request, they are simply passing information along about something coming up.",
              s.["Announcements"], s.["Current Requests"]]
      ]
    p [] [
      raw l.["The order above is the order in which the request types appear on the list."]
      space
      raw l.["“{0}” and “{1}” are not subject to the automatic expiration (set on the “{2}” page) that the other requests are.",
              s.["Long-Term Requests"], s.["Expecting"], s.["Change Preferences"]]
      ]
    p [] [
      strong [] [ encLocText s.["Date"] ]
      br []
      raw l.["For new requests, this is a box with a calendar date picker."]
      space
      raw l.["Click or tab into the box to display the calendar, which will be preselected to today's date."]
      space
      raw l.["For existing requests, there will be a check box labeled “{0}”.", s.["Check to not update the date"]]
      space
      raw l.["This can be used if you are correcting spelling or punctuation, and do not have an actual update to make to the request."]
      ]
    p [] [
      strong [] [ encLocText s.["Requestor / Subject"] ]
      br []
      raw l.["For requests or praises, this field is for the name of the person who made the request or offered the praise report."]
      space
      raw l.["For announcements, this should contain the subject of the announcement."]
      space
      raw l.["For all types, it is optional; I used to have an announcement with no subject that ran every week, telling where to send requests and updates."]
      ]
    p [] [
      strong [] [ encLocText s.["Expiration"] ]
      br []
      raw l.["“{0}” means that the request is subject to the expiration days in the group preferences.",
              s.["Expire Normally"]]
      space
      raw l.["“{0}” can be used to make a request never expire (note that this is redundant for “{1}” and “{2}”).",
              s.["Request Never Expires"], s.["Long-Term Requests"], s.["Expecting"]]
      space
      raw l.["If you are editing an existing request, a third option appears."]
      space
      raw l.["“{0}” will make the request expire when it is saved.", s.["Expire Immediately"]]
      space
      raw l.["Apart from the icons on the request maintenance page, this is the only way to expire “{0}” and “{1}” requests, but it can be used for any request type.",
              s.["Long-Term Requests"], s.["Expecting"]]
      ]
    p [] [
      strong [] [ encLocText s.["Request"] ]
      br []
      raw l.["This is the text of the request."]
      space
      raw l.["The editor provides many formatting capabilities, including “Spell Check as you Type” (enabled by default), “Paste from Word”, and “Paste Plain”, as well as “Source” view, if you want to edit the HTML yourself."]
      space
      raw l.["It also supports undo and redo, and the editor supports full-screen mode."]
      space
      raw l.["Hover over each icon to see what each button does."]
      ]
    ]
  |> Layout.help "Add / Edit a Request"


/// View for the small group member maintenance help
let groupMembers () =
  let s   = I18N.localizer.Force ()
  let l   = I18N.forView "Help/Group/Members"
  use sw  = new StringWriter ()
  let raw = rawLocText sw
  [ p [] [
      raw l.["From this page, you can add, edit, and delete the e-mail addresses for your group."]
      ]
    p [] [
      strong [] [ encLocText s.["Add a New Group Member"] ]
      br []
      raw l.["To add an e-mail address, click the icon or text in the center of the page, below the title and above the list of addresses for your group."]
      ]
    p [] [
      strong [] [ encLocText s.["Edit Group Member"] ]
      br []
      raw l.["To edit an e-mail address, click the blue pencil icon; it's the first icon under the “{0}” column heading.",
              s.["Actions"]]
      space
      raw l.["This will allow you to update the name and/or the e-mail address for that member."]
      ]
    p [] [
      strong [] [ encLocText s.["Delete a Group Member"] ]
      br []
      raw l.["To delete an e-mail address, click the blue trash can icon in the “{0}” column.", s.["Actions"]]
      space
      raw l.["Note that once an e-mail address has been deleted, it is gone."]
      space
      raw l.["(Of course, if you delete it in error, you can enter it again using the “Add” instructions above.)"]
      ]
    ]
  |> Layout.help "Maintain Group Members"


/// View for the log on help page
let logOn () =
  let s = I18N.localizer.Force ()
  let l = I18N.forView "Help/User/LogOn"
  use sw = new StringWriter ()
  let raw = rawLocText sw
  [ p [] [
      raw l.["This page allows you to log on to {0}.", s.["PrayerTracker"]]
      space
      raw l.["There are two different levels of access for {0} - user and group.", s.["PrayerTracker"]]
      ]
    p [] [
      strong [] [ encLocText s.["User Log On"] ]
      br []
      raw l.["Select your group, then enter your e-mail address and password into the appropriate boxes."]
      space
      raw l.["If you want {0} to remember you on your computer, click the “{1}” box before clicking the “{2}” button.",
              s.["PrayerTracker"], s.["Remember Me"], s.["Log On"]]
      ]
    p [] [
      strong [] [ encLocText s.["Group Log On"] ]
      br []
      raw l.["If your group has defined a password to use to allow you to view their request list online, select your group from the drop down list, then enter the group password into the appropriate box."]
      space
      raw l.["If you want {0} to remember your group, click the “{1}” box before clicking the “{2}” button.",
              s.["PrayerTracker"], s.["Remember Me"], s.["Log On"]]
      ]
    ]
  |> Layout.help "Log On"

/// Help index page
let index vi =
  let s = I18N.localizer.Force ()
  let l = I18N.forView "Help/Index"
  use sw = new StringWriter ()
  let raw = rawLocText sw

  let helpRows =
    Help.all
    |> List.map (fun h ->
        tr [] [
          td [] [
            a [ _href (sprintf "/help/%s" h.Url)
                _onclick (sprintf "return PT.showHelp('%s')" h.Url) ]
              [ encLocText s.[h.linkedText] ]
            ]
          ])
    
  [ p [] [
      raw l.["Throughout {0}, you'll see this icon {1} next to the title on each page.",
              s.["PrayerTracker"], icon "help_outline" |> (renderHtmlNode >> HtmlString)]
      space
      raw l.["Clicking this will open a new, small window with directions on using that page."]
      space
      raw l.["If you are looking for a quick overview of {0}, start with the “{1}” and “{2}” entries.",
              s.["PrayerTracker"], s.["Edit Request"], s.["Change Preferences"]]
      ]
    hr []
    p [ _class "pt-center-text" ] [ strong [] [ encLocText s.["Help Topics"] ] ]
    table [ _class "pt-table" ] [ tbody [] helpRows ]
    ]
  |> Layout.Content.standard
  |> Layout.standard vi "Help"


let password () =
  let s = I18N.localizer.Force ()
  let l = I18N.forView "Help/User/Password"
  use sw = new StringWriter ()
  let raw = rawLocText sw
  [ p [] [
      raw l.["This page will let you change your password."]
      space
      raw l.["Enter your existing password in the top box, then enter your new password in the bottom two boxes."]
      space
      raw l.["Entering your existing password is a security measure; with the “{0}” box on the log in page, this will prevent someone else who may be using your computer from being able to simply go to the site and change your password.",
              s.["Remember Me"]]
      ]
    p [] [
      raw l.["If you cannot remember your existing password, we cannot retrieve it, but we can set it to something known so that you can then change it to your password."]
      space
      raw l.["<a href=\"mailto:daniel@djs-consulting.com?subject={0}%20Password%20Help\">Click here to request help resetting your password</a>.",
              s.["PrayerTracker"]]
      ]
    ]
  |> Layout.help "Change Your Password"


/// View for the small group preferences help page
let preferences () =
  let s   = I18N.localizer.Force ()
  let l   = I18N.forView "Help/Group/Preferences"
  use sw  = new StringWriter ()
  let raw = rawLocText sw
  [ p [] [
      raw l.["This page allows you to change how your prayer request list looks and behaves."]
      space
      raw l.["Each section is addressed below."]
      ]
    p [] [
      strong [] [ encLocText s.["Requests Expire After"] ]
      br []
      raw l.["When a regular request goes this many days without being updated, it expires and no longer appears on the request list."]
      space
      raw l.["Note that the categories “{0}” and “{1}” never expire automatically.", s.["Long-Term Requests"], s.["Expecting"]]
      ]
    p [] [
      strong [] [ encLocText s.["Requests “New” For"] ]
      br []
      raw l.["Requests that have been updated within this many days are identified by a hollow circle for their bullet, as opposed to a filled circle for other requests."]
      space
      raw l.["All categories respect this setting."]
      space
      raw l.["If you do a typo correction on a request, if you do not check the box to update the date, this setting will change the bullet."]
      space
      raw l.["(NOTE: In the plain-text e-mail, new requests are bulleted with a “+” symbol, and old are bulleted with a “-” symbol.)"]
      ]
    p [] [
      strong [] [ encLocText s.["Long-Term Requests Alerted for Update"] ]
      br []
      raw l.["Requests that have not been updated in this many weeks are identified by an italic font on the “{0}” page, to remind you to seek updates on these requests so that your prayers can stay relevant and current.",
              s.["Maintain Requests"]]
      ]
    p [] [
      strong [] [ encLocText s.["Request Sorting"] ]
      br []
      raw l.["By default, requests are sorted within each group by the last updated date, with the most recent on top."]
      space
      raw l.["If you would prefer to have the list sorted by requestor or subject rather than by date, select “{0}” instead.",
              s.["Sort by Requestor Name"]]
      ]
    p [] [
      strong [] [ encLocText s.["E-mail “From” Name and Address"] ]
      br []
      raw l.["{0} must put an name and e-mail address in the “from” position of each e-mail it sends.", s.["PrayerTracker"]]
      space
      raw l.["The default name is “PrayerTracker”, and the default e-mail address is “prayer@djs-consulting.com”."]
      space
      raw l.["This will work, but any bounced e-mails and out-of-office replies will be sent to that address (which is not even a real address)."]
      space
      raw l.["Changing at least the e-mail address to your address will ensure that you receive these e-mails, and can prune your e-mail list accordingly."]
      ]
    p [] [
      strong [] [ encLocText s.["E-mail Format"] ]
      br []
      raw l.["This is the default e-mail format for your group."]
      space
      raw l.["The {0} default is HTML, which sends the list just as you see it online.", s.["PrayerTracker"]]
      space
      raw l.["However, some e-mail clients may not display this properly, so you can choose to default the email to a plain-text format, which does not have colors, italics, or other formatting."]
      space
      raw l.["The setting on this page is the group default; you can select a format for each recipient on the “{0}” page.",
              s.["Maintain Group Members"]]
      ]
    p [] [
      strong [] [ encLocText s.["Colors"] ]
      br []
      raw l.["You can customize the colors that are used for the headings and lines in your request list."]
      space
      raw l.["You can select one of the 16 named colors in the drop down lists, or you can “mix your own” using red, green, and blue (RGB) values between 0 and 255."]
      space
      raw l.["There is a link on the bottom of the page to a color list with more names and their RGB values, if you're really feeling artistic."]
      space
      raw l.["The background color cannot be changed."]
      ]
    p [] [
      strong [] [ encLocText s.["Fonts{0} for List", ""] ]
      br []
      raw l.["This is a comma-separated list of fonts that will be used for your request list."]
      space
      raw l.["A warning is good here; just because you have an obscure font and like the way that it looks does not mean that others have that same font."]
      space
      raw l.["It is generally best to stick with the fonts that come with Windows - fonts like “Arial”, “Times New Roman”, “Tahoma”, and “Comic Sans MS”."]
      space
      raw l.["You should also end the font list with either “serif” or “sans-serif”, which will use the browser's default serif (like “Times New Roman”) or sans-serif (like “Arial”) font."]
      ]
    p [] [
      strong [] [ encLocText s.["Heading / List Text Size"] ]
      br []
      raw l.["This is the point size to use for each."]
      space
      raw l.["The default for the heading is 16pt, and the default for the text is 12pt."]
      ]
    p [] [
      strong [] [ encLocText s.["Making a “Large Print” List"] ]
      br []
      raw l.["If your group is comprised mostly of people who prefer large print, the following settings will make your list look like the typical large-print publication:"]
      br []
      blockquote [] [
        em [] [ encLocText s.["Fonts"] ]
        rawText " &#8212; 'Times New Roman',serif"
        br []
        em [] [ encLocText s.["Heading Text Size"] ]
        rawText " &#8212; 18pt"
        br []
        em [] [ encLocText s.["List Text Size"] ]
        rawText " &#8212; 16pt"
        ]
      ]
    p [] [
      strong [] [ encLocText s.["Time Zone"] ]
      br []
      raw l.["This is the time zone that you would like to use for your group."]
      space
      raw l.["If you do not see your time zone listed, just <a href=\"mailto:daniel@djs-consulting.com?subject={0}%20{1}\">contact Daniel</a> and tell him what time zone you need.",
              s.["PrayerTracker"], s.["Time Zone"].Value.Replace(" ", "%20")]
      ]
    p [] [
      strong [] [ encLocText s.["Request List Visibility"] ]
      br []
      raw l.["The group's request list can be either public, private, or password-protected."]
      space
      raw l.["Public lists are available without logging in, and private lists are only available online to administrators (though the list can still be sent via e-mail by an administrator)."]
      space
      raw l.["Password-protected lists allow group members to log in and view the current request list online, using the “{0}” link and providing this password.",
              s.["Group Log On"]]
      raw l.["As this is a shared password, it is stored in plain text, so you can easily see what it is."]
      space
      raw l.["If you select “{0}” but do not enter a password, the list remains private, which is also the default value.",
              s.["Password Protected"]]
      space
      raw l.["(Changing this password will force all members of the group who logged in with the “{0}” box checked to provide the new password.)",
        s.["Remember Me"]]
      ]
    ]
  |> Layout.help "Change Preferences"


/// View for the request maintenance help page
let requests () =
  let s   = I18N.localizer.Force ()
  let l   = I18N.forView "Help/Requests/Maintain"
  use sw  = new StringWriter ()
  let raw = rawLocText sw
  [ p [] [
      raw l.["From this page, you can add, edit, and delete your current requests."]
      raw l.["You can also restore requests that may have expired, but should be made active once again."]
      ]
    p [] [
      strong [] [ encLocText s.["Add a New Request"] ]
      br []
      raw l.["To add a request, click the icon or text in the center of the page, below the title and above the list of requests for your group."]
      ]
    p [] [
      strong [] [ encLocText s.["Edit Request"] ]
      br []
      raw l.["To edit a request, click the blue pencil icon; it's the first icon under the “{0}” column heading.",
              s.["Actions"]]
      ]
    p [] [
      strong [] [ encLocText s.["Expire a Request"] ]
      br []
      raw l.["For active requests, the second icon is an eye with a slash through it; clicking this icon will expire the request immediately."]
      space
      raw l.["This is equivalent to editing the request, selecting “{0}”, and saving it.", s.["Expire Immediately"]]
      ]
    p [] [
      strong [] [ encLocText s.["Restore an Inactive Request"] ]
      br []
      raw l.["When the page is first displayed, it does not display inactive requests."]
      space
      raw l.["However, clicking the link at the bottom of the page will refresh the page with the inactive requests shown."]
      space
      raw l.["The middle icon will look like an eye; clicking it will restore the request as an active request."]
      space
      raw l.["The last updated date will be current, and the request is set to expire normally."]
      ]
    p [] [
      strong [] [ encLocText s.["Delete a Request"] ]
      br []
      raw l.["Deleting a request is contrary to the intent of {0}, as you can retrieve requests that have expired.",
              s.["PrayerTracker"]]
      space
      raw l.["However, if there is a request that needs to be deleted, clicking the blue trash can icon in the “{0}” column will allow you to do it.",
              s.["Actions"]]
      space
      raw l.["Use this option carefully, as these deletions cannot be undone; once a request is deleted, it is gone for good."]
      ]
    ]
  |> Layout.help "Maintain Requests"

/// View for the Send Announcement page help
let sendAnnouncement () =
  let s   = I18N.localizer.Force ()
  let l   = I18N.forView "Help/Group/Announcement"
  use sw  = new StringWriter ()
  let raw = rawLocText sw
  [ p [] [
      strong [] [ encLocText s.["Announcement Text"] ]
      br []
      raw l.["This is the text of the announcement you would like to send."]
      space
      raw l.["It functions the same way as the text box on the “<a href=\"{0}\">{1}</a>” page.",
              (sprintf "/help/%s/%s" Help.editRequest.``module`` Help.editRequest.topic), s.["Edit Request"]]
      ]
    p [] [
      strong [] [ encLocText s.["Add to Request List"] ]
      br []
      raw l.["Without this box checked, the text of the announcement will only be e-mailed to your group members."]
      space
      raw l.["If you check this box, however, the text of the announcement will be added to your prayer list under the section you have selected."]
      ]
    ]
  |> Layout.help "Send Announcement"

let viewRequests () =
  let s   = I18N.localizer.Force ()
  let l   = I18N.forView "Help/Requests/View"
  use sw  = new StringWriter ()
  let raw = rawLocText sw
  [ p [] [
      raw l.["From this page, you can view the request list (for today or for the next Sunday), view a printable version of the list, and e-mail the list to the members of your group."]
      space
      raw l.["(NOTE: If you are logged in as a group member, the only option you will see is to view a printable list.)"]
      ]
    p [] [
      strong [] [ encLocText s.["List for Next Sunday"] ]
      br []
      raw l.["This will modify the date for the list, so it will look like it is currently next Sunday."]
      space
      raw l.["This can be used, for example, to see what requests will expire, or allow you to print a list with Sunday's date on Saturday evening."]
      space
      raw l.["Note that this link does not appear if it is Sunday."]
      ]
    p [] [
      strong [] [ encLocText s.["View Printable"] ]
      br []
      raw l.["Clicking this link will display the list in a format that is suitable for printing; it does not have the normal {0} header across the top.",
              s.["PrayerTracker"]]
      space
      raw l.["Once you have clicked the link, you can print it using your browser's standard “Print” functionality."]
      ]
    p [] [
      strong [] [ encLocText s.["Send Via E-mail"] ]
      br []
      raw l.["Clicking this link will send the list you are currently viewing to your group members."]
      space
      raw l.["The page will remind you that you are about to do that, and ask for your confirmation."]
      space
      raw l.["If you proceed, you will see a page that shows to whom the list was sent, and what the list looked like."]
      space
      raw l.["You may safely use your browser's “Back” button to navigate away from the page."]
      ]
    ]
  |> Layout.help "View Request List"
