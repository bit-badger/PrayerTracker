namespace PrayerTracker.ViewModels

open Microsoft.AspNetCore.Html
open Microsoft.Extensions.Localization
open PrayerTracker
open PrayerTracker.Entities


/// Helper module to return localized reference lists
module ReferenceList =

    /// A localized list of the AsOfDateDisplay DU cases
    let asOfDateList (s : IStringLocalizer) =
        [ NoDisplay.code, s["Do not display the “as of” date"]
          ShortDate.code, s["Display a short “as of” date"]
          LongDate.code,  s["Display a full “as of” date"]
        ]

    /// A list of e-mail type options
    let emailTypeList def (s : IStringLocalizer) =
        // Localize the default type
        let defaultType =
            s[match def with HtmlFormat -> "HTML Format" | PlainTextFormat -> "Plain-Text Format"].Value
        seq {
          "", LocalizedString ("", $"""{s["Group Default"].Value} ({defaultType})""")
          HtmlFormat.code,      s["HTML Format"]
          PlainTextFormat.code, s["Plain-Text Format"]
          }

    /// A list of expiration options
    let expirationList (s : IStringLocalizer) includeExpireNow =
        [ Automatic.code, s["Expire Normally"]
          Manual.code,    s["Request Never Expires"]
          if includeExpireNow then Forced.code, s["Expire Immediately"]
        ]

    /// A list of request types
    let requestTypeList (s : IStringLocalizer) =
        [ CurrentRequest,  s["Current Requests"]
          LongTermRequest, s["Long-Term Requests"]
          PraiseReport,    s["Praise Reports"]
          Expecting,       s["Expecting"]
          Announcement,    s["Announcements"]
        ]


/// A user message level
type MessageLevel =
    /// An informational message to the user
    | Info
    /// A message with information the user should consider
    | Warning
    /// A message indicating that something went wrong
    | Error

/// Support for the MessageLevel type
module MessageLevel =
    
    /// Convert a message level to its string representation
    let toString =
        function
        | Info -> "Info"
        | Warning -> "WARNING"
        | Error -> "ERROR"
    
    let toCssClass level = (toString level).ToLowerInvariant ()


/// This is used to create a message that is displayed to the user
[<NoComparison; NoEquality>]
type UserMessage =
    {   /// The type
        Level : MessageLevel
        
        /// The actual message
        Text : HtmlString
        
        /// The description (further information)
        Description : HtmlString option
    }

/// Support for the UserMessage type
module UserMessage =
  
    /// Error message template
    let error =
        { Level       = Error
          Text        = HtmlString.Empty
          Description = None
        }
    
    /// Warning message template
    let warning =
        { Level       = Warning
          Text        = HtmlString.Empty
          Description = None
        }
    
    /// Info message template
    let info =
        { Level       = Info
          Text        = HtmlString.Empty
          Description = None
        }


open System

/// View model required by the layout template, given as first parameter for all pages in PrayerTracker
[<NoComparison; NoEquality>]
type AppViewInfo =
    {   /// CSS files for the page
        Style : string list
        
        /// JavaScript files for the page
        Script : string list
        
        /// The link for help on this page
        HelpLink : string option
        
        /// Messages to be displayed to the user
        Messages : UserMessage list
        
        /// The current version of PrayerTracker
        Version : string
        
        /// The ticks when the request started
        RequestStart : int64
        
        /// The currently logged on user, if there is one
        User : User option
        
        /// The currently logged on small group, if there is one
        Group : SmallGroup option
    }

/// Support for the AppViewInfo type
module AppViewInfo =
    
    /// A fresh version that can be populated to process the current request
    let fresh =
        { Style        = []
          Script       = []
          HelpLink     = None
          Messages     = []
          Version      = ""
          RequestStart = DateTime.Now.Ticks
          User         = None
          Group        = None
        }


/// Form for sending a small group or system-wide announcement
[<CLIMutable; NoComparison; NoEquality>]
type Announcement =
    {   /// Whether the announcement should be sent to the class or to PrayerTracker users
        SendToClass  : string
        
        /// The text of the announcement
        Text : string
        
        /// Whether this announcement should be added to the "Announcements" of the prayer list
        AddToRequestList : bool option
        
        /// The ID of the request type to which this announcement should be added
        RequestType : string option
    }
with
    
    /// The text of the announcement, in plain text
    member this.PlainText
      with get () = (htmlToPlainText >> wordWrap 74) this.Text


/// Form for assigning small groups to a user
[<CLIMutable; NoComparison; NoEquality>]
type AssignGroups =
    {   /// The Id of the user being assigned
        UserId : UserId
        
        /// The full name of the user being assigned
        UserName : string
        
        /// The Ids of the small groups to which the user is authorized
        SmallGroups : string
    }

/// Support for the AssignGroups type
module AssignGroups =
    
    /// Create an instance of this form from an existing user
    let fromUser (u : User) =
        { UserId      = u.userId
          UserName    = u.fullName
          SmallGroups = ""
        }


/// Form to allow users to change their password
[<CLIMutable; NoComparison; NoEquality>]
type ChangePassword =
    {   /// The user's current password
        OldPassword : string
        
        /// The user's new password
        NewPassword : string
        
        /// The user's new password, confirmed
        NewPasswordConfirm : string
    }


/// Form for adding or editing a church
[<CLIMutable; NoComparison; NoEquality>]
type EditChurch =
    {   /// The Id of the church
        ChurchId : ChurchId
        
        /// The name of the church
        Name : string
        
        /// The city for the church
        City : string
        
        /// The state or province for the church
        State : string
        
        /// Whether the church has an active Virtual Prayer Room interface
        HasInterface : bool option
        
        /// The address for the interface
        InterfaceAddress : string option
    }
with
  
    /// Is this a new church?
    member this.IsNew
      with get () = Guid.Empty = this.ChurchId
    
    /// Populate a church from this form
    member this.PopulateChurch (church : Church) =
        { church with
            name             = this.Name
            city             = this.City
            st               = this.State
            hasInterface     = match this.HasInterface with Some x -> x | None -> false
            interfaceAddress = match this.HasInterface with Some x when x -> this.InterfaceAddress | _ -> None
        }

/// Support for the EditChurch type
module EditChurch =
    
    /// Create an instance from an existing church
    let fromChurch (ch : Church) =
        { ChurchId         = ch.churchId
          Name             = ch.name
          City             = ch.city
          State            = ch.st
          HasInterface     = match ch.hasInterface with true -> Some true | false -> None
          InterfaceAddress = ch.interfaceAddress
        }
    
    /// An instance to use for adding churches
    let empty =
        { ChurchId         = Guid.Empty
          Name             = ""
          City             = ""
          State            = ""
          HasInterface     = None
          InterfaceAddress = None
        }

  
/// Form for adding/editing small group members
[<CLIMutable; NoComparison; NoEquality>]
type EditMember =
    {   /// The Id for this small group member (not user-entered)
        MemberId : MemberId
        
        /// The name of the member
        Name : string
        
        /// The e-mail address
        Email : string
        
        /// The e-mail format
        Format : string
    }
with
  
    /// Is this a new member?
    member this.IsNew
      with get () = Guid.Empty = this.MemberId

/// Support for the EditMember type
module EditMember =
    
    /// Create an instance from an existing member
    let fromMember (m : Member) =
        { MemberId = m.memberId
          Name     = m.memberName
          Email    = m.email
          Format   = match m.format with Some f -> f | None -> ""
        }
    
    /// An empty instance
    let empty =
        { MemberId = Guid.Empty
          Name     = ""
          Email    = ""
          Format   = ""
        }


/// This form allows the user to set class preferences
[<CLIMutable; NoComparison; NoEquality>]
type EditPreferences =
    {   /// The number of days after which requests are automatically expired
        ExpireDays : int
        
        /// The number of days requests are considered "new"
        DaysToKeepNew : int
        
        /// The number of weeks after which a long-term requests is flagged as requiring an update
        LongTermUpdateWeeks : int
        
        /// Whether to sort by updated date or requestor/subject
        RequestSort : string
        
        /// The name from which e-mail will be sent
        EmailFromName : string
        
        /// The e-mail address from which e-mail will be sent
        EmailFromAddress : string
        
        /// The default e-mail type for this group
        DefaultEmailType : string
        
        /// Whether the heading line color uses named colors or R/G/B
        LineColorType : string
        
        /// The named color for the heading lines
        LineColor : string
        
        /// Whether the heading text color uses named colors or R/G/B
        HeadingColorType : string
        
        /// The named color for the heading text
        HeadingColor : string
        
        /// The fonts to use for the list
        Fonts : string
        
        /// The font size for the heading text
        HeadingFontSize : int
        
        /// The font size for the list text
        ListFontSize : int
        
        /// The time zone for the class
        TimeZone : string
        
        /// The list visibility
        Visibility : int
        
        /// The small group password
        GroupPassword : string option
        
        /// The page size for search / inactive requests
        PageSize : int
        
        /// How the as-of date should be displayed
        AsOfDate : string
    }
with
  
    /// Set the properties of a small group based on the form's properties
    member this.PopulatePreferences (prefs : ListPreferences) =
        let isPublic, grpPw =
            match this.Visibility with
            | RequestVisibility.``public`` -> true, ""
            | RequestVisibility.passwordProtected -> false, (defaultArg this.GroupPassword "")
            | RequestVisibility.``private``
            | _ -> false, ""
        { prefs with
            daysToExpire        = this.ExpireDays
            daysToKeepNew       = this.DaysToKeepNew
            longTermUpdateWeeks = this.LongTermUpdateWeeks
            requestSort         = RequestSort.fromCode this.RequestSort
            emailFromName       = this.EmailFromName
            emailFromAddress    = this.EmailFromAddress
            defaultEmailType    = EmailFormat.fromCode this.DefaultEmailType
            lineColor           = this.LineColor
            headingColor        = this.HeadingColor
            listFonts           = this.Fonts
            headingFontSize     = this.HeadingFontSize
            textFontSize        = this.ListFontSize
            timeZoneId          = this.TimeZone
            isPublic            = isPublic
            groupPassword       = grpPw
            pageSize            = this.PageSize
            asOfDateDisplay     = AsOfDateDisplay.fromCode this.AsOfDate
        }

/// Support for the EditPreferences type
module EditPreferences =
    /// Populate an edit form from existing preferences
    let fromPreferences (prefs : ListPreferences) =
        let setType (x : string) = match x.StartsWith "#" with true -> "RGB" | false -> "Name"
        { ExpireDays          = prefs.daysToExpire
          DaysToKeepNew       = prefs.daysToKeepNew
          LongTermUpdateWeeks = prefs.longTermUpdateWeeks
          RequestSort         = prefs.requestSort.code
          EmailFromName       = prefs.emailFromName
          EmailFromAddress    = prefs.emailFromAddress
          DefaultEmailType    = prefs.defaultEmailType.code
          LineColorType       = setType prefs.lineColor
          LineColor           = prefs.lineColor
          HeadingColorType    = setType prefs.headingColor
          HeadingColor        = prefs.headingColor
          Fonts               = prefs.listFonts
          HeadingFontSize     = prefs.headingFontSize
          ListFontSize        = prefs.textFontSize
          TimeZone            = prefs.timeZoneId
          GroupPassword       = Some prefs.groupPassword
          PageSize            = prefs.pageSize
          AsOfDate            = prefs.asOfDateDisplay.code
          Visibility          =
              match true with 
              | _ when prefs.isPublic -> RequestVisibility.``public``
              | _ when prefs.groupPassword = "" -> RequestVisibility.``private``
              | _ -> RequestVisibility.passwordProtected
        }


/// Form for adding or editing prayer requests
[<CLIMutable; NoComparison; NoEquality>]
type EditRequest =
    {   /// The Id of the request
        RequestId : PrayerRequestId
        
        /// The type of the request
        RequestType : string
        
        /// The date of the request
        EnteredDate : DateTime option
        
        /// Whether to update the date or not
        SkipDateUpdate : bool option
        
        /// The requestor or subject
        Requestor : string option
        
        /// How this request is expired
        Expiration : string
        
        /// The text of the request
        Text : string
    }
with
  
    /// Is this a new request?
    member this.IsNew
      with get () = Guid.Empty = this.RequestId

/// Support for the EditRequest type
module EditRequest =
    
    /// An empty instance to use for new requests
    let empty =
        { RequestId      = Guid.Empty
          RequestType    = CurrentRequest.code
          EnteredDate    = None
          SkipDateUpdate = None
          Requestor      = None
          Expiration     = Automatic.code
          Text           = ""
        }
    
    /// Create an instance from an existing request
    let fromRequest req =
        { empty with
            RequestId   = req.prayerRequestId
            RequestType = req.requestType.code
            Requestor   = req.requestor
            Expiration  = req.expiration.code
            Text        = req.text
        }


/// Form for the admin-level editing of small groups
[<CLIMutable; NoComparison; NoEquality>]
type EditSmallGroup =
    {   /// The Id of the small group
        SmallGroupId : SmallGroupId
        
        /// The name of the small group
        Name : string
        
        /// The Id of the church to which this small group belongs
        ChurchId : ChurchId
    }
with
    
    /// Is this a new small group?
    member this.IsNew
      with get () = Guid.Empty = this.SmallGroupId
    
    /// Populate a small group from this form
    member this.populateGroup (grp : SmallGroup) =
        { grp with
            name     = this.Name
            churchId = this.ChurchId
        }

/// Support for the EditSmallGroup type
module EditSmallGroup =
    
    /// Create an instance from an existing small group
    let fromGroup (g : SmallGroup) =
        { SmallGroupId = g.smallGroupId
          Name         = g.name
          ChurchId     = g.churchId
        }
    
    /// An empty instance (used when adding a new group)
    let empty =
        { SmallGroupId = Guid.Empty
          Name         = ""
          ChurchId     = Guid.Empty
        }


/// Form for the user edit page
[<CLIMutable; NoComparison; NoEquality>]
type EditUser =
    {   /// The Id of the user
        UserId : UserId
        
        /// The first name of the user
        FirstName : string
        
        /// The last name of the user
        LastName : string
        
        /// The e-mail address for the user
        Email : string
        
        /// The password for the user
        Password : string
        
        /// The password hash for the user a second time
        PasswordConfirm : string
        
        /// Is this user a PrayerTracker administrator?
        IsAdmin : bool option
    }
with
  
    /// Is this a new user?
    member this.IsNew
      with get () = Guid.Empty = this.UserId
  
    /// Populate a user from the form
    member this.PopulateUser (user : User) hasher =
        { user with
            firstName    = this.FirstName
            lastName     = this.LastName
            emailAddress = this.Email
            isAdmin      = defaultArg this.IsAdmin false
          }
        |> function
        | u when isNull this.Password || this.Password = "" -> u
        | u -> { u with passwordHash = hasher this.Password }

/// Support for the EditUser type
module EditUser =
  
    /// An empty instance
    let empty =
        { UserId          = Guid.Empty
          FirstName       = ""
          LastName        = ""
          Email           = ""
          Password        = ""
          PasswordConfirm = ""
          IsAdmin         = None
        }
    
    /// Create an instance from an existing user
    let fromUser (user : User) =
        { empty with
            UserId    = user.userId
            FirstName = user.firstName
            LastName  = user.lastName
            Email     = user.emailAddress
            IsAdmin   = if user.isAdmin then Some true else None
        }


/// Form for the small group log on page
[<CLIMutable; NoComparison; NoEquality>]
type GroupLogOn =
    {   /// The ID of the small group to which the user is logging on
        SmallGroupId : SmallGroupId
        
        /// The password entered
        Password : string
        
        /// Whether to remember the login
        RememberMe : bool option
    }

/// Support for the GroupLogOn type
module GroupLogOn =
  
    /// An empty instance
    let empty =
        { SmallGroupId = Guid.Empty
          Password     = ""
          RememberMe   = None
        }


/// Items needed to display the request maintenance page
[<NoComparison; NoEquality>]
type MaintainRequests =
    {   /// The requests to be displayed
        Requests : PrayerRequest list
        
        /// The small group to which the requests belong
        SmallGroup : SmallGroup
        
        /// Whether only active requests are included
        OnlyActive : bool option
        
        /// The search term for the requests
        SearchTerm : string option
        
        /// The page number of the results
        PageNbr : int option
    }

/// Support for the MaintainRequests type
module MaintainRequests =
    
    /// An empty instance
    let empty =
        { Requests   = []
          SmallGroup = SmallGroup.empty 
          OnlyActive = None
          SearchTerm = None
          PageNbr    = None
        }


/// Items needed to display the small group overview page
[<NoComparison; NoEquality>]
type Overview =
    {   /// The total number of active requests
        TotalActiveReqs : int
        
        /// The numbers of active requests by request type
        ActiveReqsByType : Map<PrayerRequestType, int>
        
        /// A count of all requests
        AllReqs : int
        
        /// A count of all members
        TotalMembers : int
    }


/// Form for the user log on page
[<CLIMutable; NoComparison; NoEquality>]
type UserLogOn =
    {   /// The e-mail address of the user
        Email : string
        
        /// The password entered
        Password : string
        
        /// The ID of the small group to which the user is logging on
        SmallGroupId : SmallGroupId
        
        /// Whether to remember the login
        RememberMe : bool option
        
        /// The URL to which the user should be redirected once login is successful
        RedirectUrl : string option
    }

/// Support for the UserLogOn type
module UserLogOn =
    
    /// An empty instance
    let empty =
        { Email        = ""
          Password     = ""
          SmallGroupId = Guid.Empty
          RememberMe   = None
          RedirectUrl  = None
        }


open Giraffe.ViewEngine

/// This represents a list of requests
type RequestList =
    {   /// The prayer request list
        Requests : PrayerRequest list
        
        /// The date for which this list is being generated
        Date : DateTime
        
        /// The small group to which this list belongs
        SmallGroup : SmallGroup
        
        /// Whether to show the class header
        ShowHeader : bool
        
        /// The list of recipients (populated if requests are e-mailed)
        Recipients : Member list
        
        /// Whether the user can e-mail this list
        CanEmail : bool
    }
with

    /// Group requests by their type, along with the type and its localized string
    member this.RequestsByType (s : IStringLocalizer) =
        ReferenceList.requestTypeList s
        |> List.map (fun (typ, name) ->
            let sort =
                match this.SmallGroup.preferences.requestSort with
                | SortByDate -> Seq.sortByDescending (fun req -> req.updatedDate)
                | SortByRequestor -> Seq.sortBy (fun req -> req.requestor)
            let reqs =
                this.Requests
                |> Seq.ofList
                |> Seq.filter (fun req -> req.requestType = typ)
                |> sort
                |> List.ofSeq
            typ, name, reqs)
        |> List.filter (fun (_, _, reqs) -> not (List.isEmpty reqs))
    
    /// Is this request new?
    member this.IsNew (req : PrayerRequest) =
        (this.Date - req.updatedDate).Days <= this.SmallGroup.preferences.daysToKeepNew
    
    /// Generate this list as HTML
    member this.AsHtml (s : IStringLocalizer) =
        let prefs    = this.SmallGroup.preferences
        let asOfSize = Math.Round (float prefs.textFontSize * 0.8, 2)
        [   if this.ShowHeader then
                div [ _style $"text-align:center;font-family:{prefs.listFonts}" ] [
                    span [ _style $"font-size:%i{prefs.headingFontSize}pt;" ] [
                        strong [] [ str s["Prayer Requests"].Value ]
                    ]
                    br []
                    span [ _style $"font-size:%i{prefs.textFontSize}pt;" ] [
                        strong [] [ str this.SmallGroup.name ]
                        br []
                        str (this.Date.ToString s["MMMM d, yyyy"].Value)
                    ]
                ]
                br []
            for _, name, reqs in this.RequestsByType s do
                div [ _style "padding-left:10px;padding-bottom:.5em;" ] [
                    table [ _style $"font-family:{prefs.listFonts};page-break-inside:avoid;" ] [
                        tr [] [
                            td [ _style $"font-size:%i{prefs.headingFontSize}pt;color:{prefs.headingColor};padding:3px 0;border-top:solid 3px {prefs.lineColor};border-bottom:solid 3px {prefs.lineColor};font-weight:bold;" ] [
                                rawText "&nbsp; &nbsp; "; str name.Value; rawText "&nbsp; &nbsp; "
                            ]
                        ]
                    ]
                ]
                reqs
                |> List.map (fun req ->
                    let bullet = if this.IsNew req then "circle" else "disc"
                    li [ _style $"list-style-type:{bullet};font-family:{prefs.listFonts};font-size:%i{prefs.textFontSize}pt;padding-bottom:.25em;" ] [
                        match req.requestor with
                        | Some r when r <> "" ->
                            strong [] [ str r ]
                            rawText " &mdash; "
                        | Some _ -> ()
                        | None -> ()
                        rawText req.text
                        match prefs.asOfDateDisplay with
                        | NoDisplay -> ()
                        | ShortDate
                        | LongDate ->
                            let dt =
                                match prefs.asOfDateDisplay with
                                | ShortDate -> req.updatedDate.ToShortDateString ()
                                | LongDate -> req.updatedDate.ToLongDateString ()
                                | _ -> ""
                            i [ _style $"font-size:%.2f{asOfSize}pt" ] [
                                rawText "&nbsp; ("; str s["as of"].Value; str " "; str dt; rawText ")"
                            ]
                    ])
                  |> ul []
                br []
          ]
        |> RenderView.AsString.htmlNodes

    /// Generate this list as plain text
    member this.AsText (s : IStringLocalizer) =
        seq {
            this.SmallGroup.name
            s["Prayer Requests"].Value
            this.Date.ToString s["MMMM d, yyyy"].Value
            " "
            for _, name, reqs in this.RequestsByType s do
                let dashes = String.replicate (name.Value.Length + 4) "-"
                dashes
                $"  {name.Value.ToUpper ()}"
                dashes
                for req in reqs do
                    let bullet    = if this.IsNew req then "+" else "-"
                    let requestor = match req.requestor with Some r -> $"{r} - " | None -> ""
                    match this.SmallGroup.preferences.asOfDateDisplay with
                    | NoDisplay -> ""
                    | _ ->
                        let dt =
                            match this.SmallGroup.preferences.asOfDateDisplay with
                            | ShortDate -> req.updatedDate.ToShortDateString ()
                            | LongDate -> req.updatedDate.ToLongDateString ()
                            | _ -> ""
                        $"""  ({s["as of"].Value} {dt})"""
                    |> sprintf "  %s %s%s%s" bullet requestor (htmlToPlainText req.text)
                " "
        }
        |> String.concat "\n"
        |> wordWrap 74
