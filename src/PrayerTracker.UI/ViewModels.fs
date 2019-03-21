namespace PrayerTracker.ViewModels

open Microsoft.AspNetCore.Html
open Microsoft.Extensions.Localization
open PrayerTracker
open PrayerTracker.Entities
open System


/// Helper module to return localized reference lists
module ReferenceList =

  /// A localized list of the AsOfDateDisplay DU cases
  let asOfDateList (s : IStringLocalizer) =
    [ NoDisplay.code, s.["Do not display the “as of” date"]
      ShortDate.code, s.["Display a short “as of” date"]
      LongDate.code,  s.["Display a full “as of” date"]
      ]

  /// A list of e-mail type options
  let emailTypeList def (s : IStringLocalizer) =
    // Localize the default type
    let defaultType =
      match def with
      | HtmlFormat -> s.["HTML Format"].Value
      | PlainTextFormat -> s.["Plain-Text Format"].Value
    seq {
      yield "", LocalizedString ("", sprintf "%s (%s)" s.["Group Default"].Value defaultType)
      yield HtmlFormat.code, s.["HTML Format"]
      yield PlainTextFormat.code, s.["Plain-Text Format"]
      }

  /// A list of expiration options
  let expirationList (s : IStringLocalizer) includeExpireNow =
    [ yield Automatic.code, s.["Expire Normally"]
      yield Manual.code, s.["Request Never Expires"]
      match includeExpireNow with true -> yield Forced.code, s.["Expire Immediately"] | false -> ()
      ]

  /// A list of request types
  let requestTypeList (s : IStringLocalizer) =
    [ CurrentRequest,  s.["Current Requests"]
      LongTermRequest, s.["Long-Term Requests"]
      PraiseReport,    s.["Praise Reports"]
      Expecting,       s.["Expecting"]
      Announcement,    s.["Announcements"]
      ]
    

/// This is used to create a message that is displayed to the user
[<NoComparison; NoEquality>]
type UserMessage =
  { /// The type
    level       : string
    /// The actual message
    text        : HtmlString
    /// The description (further information)
    description : HtmlString option
    }
  with
    /// Error message template
    static member Error =
      { level       = "ERROR"
        text        = HtmlString.Empty
        description = None
        }
    /// Warning message template
    static member Warning =
      { level       = "WARNING"
        text        = HtmlString.Empty
        description = None
        }
    /// Info message template
    static member Info =
      { level       = "Info"
        text        = HtmlString.Empty
        description = None
        }


/// View model required by the layout template, given as first parameter for all pages in PrayerTracker
[<NoComparison; NoEquality>]
type AppViewInfo =
  { /// CSS files for the page
    style        : string list
    /// JavaScript files for the page
    script       : string list
    /// The link for help on this page
    helpLink     : string option
    /// Messages to be displayed to the user
    messages     : UserMessage list
    /// The current version of PrayerTracker
    version      : string
    /// The ticks when the request started
    requestStart : int64
    /// The currently logged on user, if there is one
    user         : User option
    /// The currently logged on small group, if there is one
    group        : SmallGroup option
    }
  with
    /// A fresh version that can be populated to process the current request
    static member fresh =
      { style        = []
        script       = []
        helpLink     = None
        messages     = []
        version      = ""
        requestStart = DateTime.Now.Ticks
        user         = None
        group        = None
        }


/// Form for sending a small group or system-wide announcement
[<CLIMutable; NoComparison; NoEquality>]
type Announcement =
  { /// Whether the announcement should be sent to the class or to PrayerTracker users
    sendToClass      : string
    /// The text of the announcement
    text             : string
    /// Whether this announcement should be added to the "Announcements" of the prayer list
    addToRequestList : bool option
    /// The ID of the request type to which this announcement should be added
    requestType      : string option
    }
with
  /// The text of the announcement, in plain text
  member this.plainText () = (htmlToPlainText >> wordWrap 74) this.text


/// Form for assigning small groups to a user
[<CLIMutable; NoComparison; NoEquality>]
type AssignGroups =
  { /// The Id of the user being assigned
    userId      : UserId
    /// The full name of the user being assigned
    userName    : string
    /// The Ids of the small groups to which the user is authorized
    smallGroups : string
    }
with
  /// Create an instance of this form from an existing user
  static member fromUser (u : User) =
    { userId      = u.userId
      userName    = u.fullName
      smallGroups = ""
      }


/// Form to allow users to change their password
[<CLIMutable; NoComparison; NoEquality>]
type ChangePassword =
  { /// The user's current password
    oldPassword        : string
    /// The user's new password
    newPassword        : string
    /// The user's new password, confirmed
    newPasswordConfirm : string
    }


/// Form for adding or editing a church
[<CLIMutable; NoComparison; NoEquality>]
type EditChurch =
  { /// The Id of the church
    churchId         : ChurchId
    /// The name of the church
    name             : string
    /// The city for the church
    city             : string
    /// The state for the church
    st               : string
    /// Whether the church has an active VPR interface
    hasInterface     : bool option
    /// The address for the interface
    interfaceAddress : string option
    }
with
  /// Create an instance from an existing church
  static member fromChurch (ch : Church) =
    { churchId = ch.churchId
      name     = ch.name
      city     = ch.city
      st       = ch.st
      hasInterface = match ch.hasInterface with true -> Some true | false -> None
      interfaceAddress = ch.interfaceAddress
      }
  /// An instance to use for adding churches
  static member empty =
    { churchId         = Guid.Empty
      name             = ""
      city             = ""
      st               = ""
      hasInterface     = None
      interfaceAddress = None
      }
  /// Is this a new church?
  member this.isNew () = Guid.Empty = this.churchId
  /// Populate a church from this form
  member this.populateChurch (church : Church) =
    { church with
        name             = this.name
        city             = this.city
        st               = this.st
        hasInterface     = match this.hasInterface with Some x -> x | None -> false
        interfaceAddress = match this.hasInterface with Some x when x -> this.interfaceAddress | _ -> None
      }
  
  
/// Form for adding/editing small group members
[<CLIMutable; NoComparison; NoEquality>]
type EditMember =
  { /// The Id for this small group member (not user-entered)
    memberId     : MemberId
    /// The name of the member
    memberName   : string
    /// The e-mail address
    emailAddress : string
    /// The e-mail format
    emailType    : string
    }
with
  /// Create an instance from an existing member
  static member fromMember (m : Member) =
    { memberId     = m.memberId
      memberName   = m.memberName
      emailAddress = m.email
      emailType    = match m.format with Some f -> f | None -> ""
    }
  /// An empty instance
  static member empty =
    { memberId     = Guid.Empty
      memberName   = ""
      emailAddress = ""
      emailType    = ""
    }
  /// Is this a new member?
  member this.isNew () = Guid.Empty = this.memberId


/// This form allows the user to set class preferences
[<CLIMutable; NoComparison; NoEquality>]
type EditPreferences =
  { /// The number of days after which requests are automatically expired
    expireDays          : int
    /// The number of days requests are considered "new"
    daysToKeepNew       : int
    /// The number of weeks after which a long-term requests is flagged as requiring an update
    longTermUpdateWeeks : int
    /// Whether to sort by updated date or requestor/subject
    requestSort         : string
    /// The name from which e-mail will be sent
    emailFromName       : string
    /// The e-mail address from which e-mail will be sent
    emailFromAddress    : string
    /// The default e-mail type for this group
    defaultEmailType    : string
    /// Whether the heading line color uses named colors or R/G/B
    headingLineType     : string
    /// The named color for the heading lines
    headingLineColor    : string
    /// Whether the heading text color uses named colors or R/G/B
    headingTextType     : string
    /// The named color for the heading text
    headingTextColor    : string
    /// The fonts to use for the list
    listFonts           : string
    /// The font size for the heading text
    headingFontSize     : int
    /// The font size for the list text
    listFontSize        : int
    /// The time zone for the class
    timeZone            : string
    /// The list visibility
    listVisibility      : int
    /// The small group password
    groupPassword       : string option
    /// The page size for search / inactive requests
    pageSize            : int
    /// How the as-of date should be displayed
    asOfDate            : string
    }
with
  static member fromPreferences (prefs : ListPreferences) =
    let setType (x : string) = match x.StartsWith "#" with true -> "RGB" | false -> "Name"
    { expireDays          = prefs.daysToExpire
      daysToKeepNew       = prefs.daysToKeepNew
      longTermUpdateWeeks = prefs.longTermUpdateWeeks
      requestSort         = prefs.requestSort.code
      emailFromName       = prefs.emailFromName
      emailFromAddress    = prefs.emailFromAddress
      defaultEmailType    = prefs.defaultEmailType.code
      headingLineType     = setType prefs.lineColor
      headingLineColor    = prefs.lineColor
      headingTextType     = setType prefs.headingColor
      headingTextColor    = prefs.headingColor
      listFonts           = prefs.listFonts
      headingFontSize     = prefs.headingFontSize
      listFontSize        = prefs.textFontSize
      timeZone            = prefs.timeZoneId
      groupPassword       = Some prefs.groupPassword
      pageSize            = prefs.pageSize
      asOfDate            = prefs.asOfDateDisplay.code
      listVisibility      =
        match true with 
        | _ when prefs.isPublic -> RequestVisibility.``public``
        | _ when prefs.groupPassword = "" -> RequestVisibility.``private``
        | _ -> RequestVisibility.passwordProtected
      }
  /// Set the properties of a small group based on the form's properties
  member this.populatePreferences (prefs : ListPreferences) =
    let isPublic, grpPw =
      match this.listVisibility with
      | RequestVisibility.``public`` -> true, ""
      | RequestVisibility.passwordProtected -> false, (defaultArg this.groupPassword "")
      | RequestVisibility.``private``
      | _ -> false, ""
    { prefs with
        daysToExpire        = this.expireDays
        daysToKeepNew       = this.daysToKeepNew
        longTermUpdateWeeks = this.longTermUpdateWeeks
        requestSort         = RequestSort.fromCode this.requestSort
        emailFromName       = this.emailFromName
        emailFromAddress    = this.emailFromAddress
        defaultEmailType    = EmailFormat.fromCode this.defaultEmailType
        lineColor           = this.headingLineColor
        headingColor        = this.headingTextColor
        listFonts           = this.listFonts
        headingFontSize     = this.headingFontSize
        textFontSize        = this.listFontSize
        timeZoneId          = this.timeZone
        isPublic            = isPublic
        groupPassword       = grpPw
        pageSize            = this.pageSize
        asOfDateDisplay     = AsOfDateDisplay.fromCode this.asOfDate
      }


/// Form for adding or editing prayer requests
[<CLIMutable; NoComparison; NoEquality>]
type EditRequest =
  { /// The Id of the request
    requestId      : PrayerRequestId
    /// The type of the request
    requestType    : string
    /// The date of the request
    //[<Display (Name = "Date")>]
    enteredDate    : DateTime option
    /// Whether to update the date or not
    skipDateUpdate : bool option
    /// The requestor or subject
    requestor      : string option
    /// How this request is expired
    expiration     : string
    /// The text of the request
    text           : string
    }
with
  /// An empty instance to use for new requests
  static member empty =
    { requestId      = Guid.Empty
      requestType    = CurrentRequest.code
      enteredDate    = None
      skipDateUpdate = None
      requestor      = None
      expiration     = Automatic.code
      text           = ""
      }
  /// Create an instance from an existing request
  static member fromRequest req =
    { EditRequest.empty with
        requestId   = req.prayerRequestId
        requestType = req.requestType.code
        requestor   = req.requestor
        expiration  = req.expiration.code
        text        = req.text
      }
  /// Is this a new request?
  member this.isNew () = Guid.Empty = this.requestId


/// Form for the admin-level editing of small groups
[<CLIMutable; NoComparison; NoEquality>]
type EditSmallGroup =
  { /// The Id of the small group
    smallGroupId : SmallGroupId
    /// The name of the small group
    name         : string
    /// The Id of the church to which this small group belongs
    churchId     : ChurchId
    }
with
  /// Create an instance from an existing small group
  static member fromGroup (g : SmallGroup) =
    { smallGroupId = g.smallGroupId
      name         = g.name
      churchId     = g.churchId
    }
  /// An empty instance (used when adding a new group)
  static member empty =
    { smallGroupId = Guid.Empty
      name         = ""
      churchId     = Guid.Empty
      }
  /// Is this a new small group?
  member this.isNew () = Guid.Empty = this.smallGroupId
  /// Populate a small group from this form
  member this.populateGroup (grp : SmallGroup) =
    { grp with
        name     = this.name
        churchId = this.churchId
      }


/// Form for the user edit page
[<CLIMutable; NoComparison; NoEquality>]
type EditUser =
  { /// The Id of the user
    userId          : UserId
    /// The first name of the user
    firstName       : string
    /// The last name of the user
    lastName        : string
    /// The e-mail address for the user
    emailAddress    : string
    /// The password for the user
    password        : string
    /// The password hash for the user a second time
    passwordConfirm : string
    /// Is this user a PrayerTracker administrator?
    isAdmin         : bool option
    }
with
  /// An empty instance
  static member empty =
    { userId          = Guid.Empty
      firstName       = ""
      lastName        = ""
      emailAddress    = ""
      password        = ""
      passwordConfirm = ""
      isAdmin         = None
      }
  /// Create an instance from an existing user
  static member fromUser (user : User) =
    { EditUser.empty with
        userId       = user.userId
        firstName    = user.firstName
        lastName     = user.lastName
        emailAddress = user.emailAddress
        isAdmin      = match user.isAdmin with true -> Some true | false -> None
      }
  /// Is this a new user?
  member this.isNew () = Guid.Empty = this.userId
  /// Populate a user from the form
  member this.populateUser (user : User) hasher =
    { user with
        firstName    = this.firstName
        lastName     = this.lastName
        emailAddress = this.emailAddress
        isAdmin      = match this.isAdmin with Some x -> x | None -> false
      }
    |> function
    | u when this.password = null || this.password = "" -> u
    | u -> { u with passwordHash = hasher this.password }


/// Form for the small group log on page
[<CLIMutable; NoComparison; NoEquality>]
type GroupLogOn =
  { /// The ID of the small group to which the user is logging on
    smallGroupId : SmallGroupId
    /// The password entered
    password     : string
    /// Whether to remember the login
    rememberMe   : bool option
    }
with
  static member empty =
    { smallGroupId   = Guid.Empty
      password       = ""
      rememberMe     = None
      }


/// Items needed to display the request maintenance page
[<NoComparison; NoEquality>]
type MaintainRequests =
  { /// The requests to be displayed
    requests   : PrayerRequest seq
    /// The small group to which the requests belong
    smallGroup : SmallGroup
    /// Whether only active requests are included
    onlyActive : bool option
    /// The search term for the requests
    searchTerm : string option
    /// The page number of the results
    pageNbr    : int option
    }
with
  static member empty =
    { requests   = Seq.empty
      smallGroup = SmallGroup.empty 
      onlyActive = None
      searchTerm = None
      pageNbr    = None
      }


/// Items needed to display the small group overview page
[<NoComparison; NoEquality>]
type Overview =
  { /// The total number of active requests
    totalActiveReqs : int
    /// The numbers of active requests by category
    activeReqsByCat : Map<PrayerRequestType, int>
    /// A count of all requests
    allReqs         : int
    /// A count of all members
    totalMbrs       : int
    }


/// Form for the user log on page
[<CLIMutable; NoComparison; NoEquality>]
type UserLogOn =
  { /// The e-mail address of the user
    emailAddress : string
    /// The password entered
    password     : string
    /// The ID of the small group to which the user is logging on
    smallGroupId : SmallGroupId
    /// Whether to remember the login
    rememberMe   : bool option
    /// The URL to which the user should be redirected once login is successful
    redirectUrl  : string option
    }
with
  static member empty =
    { emailAddress   = ""
      password       = ""
      smallGroupId   = Guid.Empty
      rememberMe     = None
      redirectUrl    = None
      }


open Giraffe.GiraffeViewEngine

/// This represents a list of requests
type RequestList =
  { /// The prayer request list
    requests   : PrayerRequest list
    /// The date for which this list is being generated
    date       : DateTime
    /// The small group to which this list belongs
    listGroup  : SmallGroup
    /// Whether to show the class header
    showHeader : bool
    /// The list of recipients (populated if requests are e-mailed)
    recipients : Member list
    /// Whether the user can e-mail this list
    canEmail   : bool
    }
with
  /// Get the requests for a specified type
  member this.requestsInCategory cat =
    let reqs =
      this.requests
      |> Seq.ofList
      |> Seq.filter (fun req -> req.requestType = cat)
    match this.listGroup.preferences.requestSort with
    | SortByDate -> reqs |> Seq.sortByDescending (fun req -> req.updatedDate)
    | SortByRequestor -> reqs |> Seq.sortBy (fun req -> req.requestor)
    |> List.ofSeq
  /// Is this request new?
  member this.isNew (req : PrayerRequest) =
    (this.date - req.updatedDate).Days <= this.listGroup.preferences.daysToKeepNew
  /// Generate this list as HTML
  member this.asHtml (s : IStringLocalizer) =
    let prefs    = this.listGroup.preferences
    let asOfSize = Math.Round (float prefs.textFontSize * 0.8, 2)
    [ match this.showHeader with
      | true ->
          yield div [ _style (sprintf "text-align:center;font-family:%s" prefs.listFonts) ] [
            span [ _style (sprintf "font-size:%ipt;" prefs.headingFontSize) ] [
              strong [] [ str s.["Prayer Requests"].Value ]
              ]
            br []
            span [ _style (sprintf "font-size:%ipt;" prefs.textFontSize) ] [
              strong [] [ str this.listGroup.name ]
              br []
              str (this.date.ToString s.["MMMM d, yyyy"].Value)
              ]
            ]
          yield br []
      | false -> ()
      let typs = ReferenceList.requestTypeList s
      for cat in
          typs
          |> Seq.ofList
          |> Seq.map fst
          |> Seq.filter (fun c -> 0 < (this.requests |> List.filter (fun req -> req.requestType = c) |> List.length)) do
        let reqs    = this.requestsInCategory cat
        let catName = typs |> List.filter (fun t -> fst t = cat) |> List.head |> snd
        yield div [ _style "padding-left:10px;padding-bottom:.5em;" ] [
          table [ _style (sprintf "font-family:%s;page-break-inside:avoid;" prefs.listFonts) ] [
            tr [] [
              td [ _style (sprintf "font-size:%ipt;color:%s;padding:3px 0;border-top:solid 3px %s;border-bottom:solid 3px %s;font-weight:bold;"
                                      prefs.headingFontSize prefs.headingColor prefs.lineColor prefs.lineColor) ] [
                rawText "&nbsp; &nbsp; "; str catName.Value; rawText "&nbsp; &nbsp; "
                ]
              ]
            ]
          ]
        yield
          reqs
          |> List.map (fun req ->
              let bullet = match this.isNew req with true -> "circle" | false -> "disc"
              li [ _style (sprintf "list-style-type:%s;font-family:%s;font-size:%ipt;padding-bottom:.25em;"
                                      bullet prefs.listFonts prefs.textFontSize) ] [
                match req.requestor with
                | Some rqstr when rqstr <> "" ->
                    yield strong [] [ str rqstr ]
                    yield rawText " &mdash; "
                | Some _ -> ()
                | None -> ()
                yield rawText req.text
                match prefs.asOfDateDisplay with
                | NoDisplay -> ()
                | ShortDate
                | LongDate ->
                    let dt =
                      match prefs.asOfDateDisplay with
                      | ShortDate -> req.updatedDate.ToShortDateString ()
                      | LongDate -> req.updatedDate.ToLongDateString ()
                      | _ -> ""
                    yield i [ _style (sprintf "font-size:%.2fpt" asOfSize) ] [
                      rawText "&nbsp; ("; str s.["as of"].Value; str " "; str dt; rawText ")"
                      ]
                ])
          |> ul []
        yield br []
      ]
    |> renderHtmlNodes

  /// Generate this list as plain text
  member this.asText (s : IStringLocalizer) =
    seq {
      yield this.listGroup.name
      yield s.["Prayer Requests"].Value
      yield this.date.ToString s.["MMMM d, yyyy"].Value
      yield " "
      let typs = ReferenceList.requestTypeList s
      for cat in
          typs
          |> Seq.ofList
          |> Seq.map fst
          |> Seq.filter (fun c -> 0 < (this.requests |> List.filter (fun req -> req.requestType = c) |> List.length)) do
        let reqs   = this.requestsInCategory cat
        let typ    = (typs |> List.filter (fun t -> fst t = cat) |> List.head |> snd).Value
        let dashes = String.replicate (typ.Length + 4) "-"
        yield dashes
        yield sprintf @"  %s" (typ.ToUpper ())
        yield dashes
        for req in reqs do
          let bullet = match this.isNew req with true -> "+" | false -> "-"
          let requestor = match req.requestor with Some r -> sprintf "%s - " r | None -> ""
          yield
            match this.listGroup.preferences.asOfDateDisplay with
            | NoDisplay -> ""
            | _ ->
                let dt =
                  match this.listGroup.preferences.asOfDateDisplay with
                  | ShortDate -> req.updatedDate.ToShortDateString ()
                  | LongDate -> req.updatedDate.ToLongDateString ()
                  | _ -> ""
                sprintf "  (%s %s)" s.["as of"].Value dt
            |> sprintf "  %s %s%s%s" bullet requestor (htmlToPlainText req.text)
        yield " "
      }
    |> String.concat "\n"
    |> wordWrap 74
