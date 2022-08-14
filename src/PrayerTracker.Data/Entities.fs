namespace PrayerTracker.Entities

(*-- SUPPORT TYPES --*)

/// How as-of dates should (or should not) be displayed with requests
type AsOfDateDisplay =
    /// No as-of date should be displayed
    | NoDisplay
    /// The as-of date should be displayed in the culture's short date format
    | ShortDate
    /// The as-of date should be displayed in the culture's long date format
    | LongDate

/// Functions to support as-of date display options
module AsOfDateDisplay =
    
    /// Convert to a DU case from a single-character string
    let fromCode code =
        match code with
        | "N" -> NoDisplay
        | "S" -> ShortDate
        | "L" -> LongDate
        | _   -> invalidArg "code" $"Unknown code {code}"
    
    /// Convert this DU case to a single-character string
    let toCode = function NoDisplay -> "N" | ShortDate -> "S" | LongDate -> "L"


/// Acceptable e-mail formats
type EmailFormat =
    /// HTML e-mail
    | HtmlFormat
    /// Plain-text e-mail
    | PlainTextFormat

/// Functions to support e-mail formats
module EmailFormat =
    
    /// Convert to a DU case from a single-character string
    let fromCode code =
        match code with
        | "H" -> HtmlFormat
        | "P" -> PlainTextFormat
        | _   -> invalidArg "code" $"Unknown code {code}"
    
    /// Convert this DU case to a single-character string
    let toCode = function HtmlFormat -> "H" | PlainTextFormat -> "P"


/// Expiration for requests
type Expiration =
    /// Follow the rules for normal expiration
    | Automatic
    /// Do not expire via rules
    | Manual
    /// Force immediate expiration
    | Forced

/// Functions to support expirations
module Expiration =
    
    /// Convert to a DU case from a single-character string
    let fromCode code =
        match code with
        | "A" -> Automatic
        | "M" -> Manual
        | "F" -> Forced
        | _   -> invalidArg "code" $"Unknown code {code}"
    
    /// Convert this DU case to a single-character string
    let toCode = function Automatic -> "A" | Manual -> "M" | Forced -> "F"


/// Types of prayer requests
type PrayerRequestType =
    /// Current requests
    | CurrentRequest
    /// Long-term/ongoing request
    | LongTermRequest
    /// Expectant couples
    | Expecting
    /// Praise reports
    | PraiseReport
    /// Announcements
    | Announcement

/// Functions to support prayer request types
module PrayerRequestType =
    
    /// Convert to a DU case from a single-character string
    let fromCode code =
        match code with
        | "C" -> CurrentRequest
        | "L" -> LongTermRequest
        | "E" -> Expecting
        | "P" -> PraiseReport
        | "A" -> Announcement
        | _   -> invalidArg "code" $"Unknown code {code}"
    
    /// Convert this DU case to a single-character string
    let toCode =
        function
        | CurrentRequest  -> "C"
        | LongTermRequest -> "L"
        | Expecting       -> "E"
        | PraiseReport    -> "P"
        | Announcement    -> "A"


/// How requests should be sorted
type RequestSort =
    /// Sort by date, then by requestor/subject
    | SortByDate
    /// Sort by requestor/subject, then by date
    | SortByRequestor

/// Functions to support request sorts
module RequestSort =
    
    /// Convert to a DU case from a single-character string
    let fromCode code =
        match code with
        | "D" -> SortByDate
        | "R" -> SortByRequestor
        | _   -> invalidArg "code" $"Unknown code {code}"
    
    /// Convert this DU case to a single-character string
    let toCode = function SortByDate -> "D" | SortByRequestor -> "R"


open System

/// PK type for the Church entity
type ChurchId =
    | ChurchId of Guid
with
    /// The GUID value of the church ID
    member this.Value = this |> function ChurchId guid -> guid


/// PK type for the Member entity
type MemberId =
    | MemberId of Guid
with
    /// The GUID value of the member ID
    member this.Value = this |> function MemberId guid -> guid


/// PK type for the PrayerRequest entity
type PrayerRequestId =
    | PrayerRequestId of Guid
with
    /// The GUID value of the prayer request ID
    member this.Value = this |> function PrayerRequestId guid -> guid


/// PK type for the SmallGroup entity
type SmallGroupId =
    | SmallGroupId of Guid
with
    /// The GUID value of the small group ID
    member this.Value = this |> function SmallGroupId guid -> guid


/// PK type for the TimeZone entity
type TimeZoneId = TimeZoneId of string

/// Functions to support time zone IDs
module TimeZoneId =
    
    /// Convert a time zone ID to its string value
    let toString = function TimeZoneId it -> it


/// PK type for the User entity
type UserId =
    | UserId of Guid
with
    /// The GUID value of the user ID
    member this.Value = this |> function UserId guid -> guid

(*-- SPECIFIC VIEW TYPES --*)

/// Statistics for churches
[<NoComparison; NoEquality>]
type ChurchStats =
    {   /// The number of small groups in the church
        SmallGroups : int
        
        /// The number of prayer requests in the church
        PrayerRequests : int
        
        /// The number of users who can access small groups in the church
        Users : int
    }


/// Information needed to display the public/protected request list and small group maintenance pages
[<NoComparison; NoEquality>]
type SmallGroupInfo =
    {   /// The ID of the small group
        Id : string
        
        /// The name of the small group
        Name : string
        
        /// The name of the church to which the small group belongs
        ChurchName : string
        
        /// The ID of the time zone for the small group
        TimeZoneId : TimeZoneId
        
        /// Whether the small group has a publicly-available request list
        IsPublic : bool
    }

(*-- ENTITIES --*)

open NodaTime

/// This represents a church
[<NoComparison; NoEquality>]
type Church =
    {   /// The ID of this church
        Id : ChurchId
        
        /// The name of the church
        Name : string
        
        /// The city where the church is
        City : string
        
        /// The 2-letter state or province code for the church's location
        State : string
        
        /// Does this church have an active interface with Virtual Prayer Space?
        HasVpsInterface : bool
        
        /// The address for the interface
        InterfaceAddress : string option
    }

/// Functions to support churches
module Church =
    
    /// An empty church
    // aww... how sad :(
    let empty =
        {   Id               = ChurchId Guid.Empty
            Name             = ""
            City             = ""
            State            = ""
            HasVpsInterface  = false
            InterfaceAddress = None
        }
    

/// Preferences for the form and format of the prayer request list
[<NoComparison; NoEquality>]
type ListPreferences =
    {   /// The Id of the small group to which these preferences belong
        SmallGroupId : SmallGroupId
        
        /// The days after which regular requests expire
        DaysToExpire : int
        
        /// The number of days a new or updated request is considered new
        DaysToKeepNew : int
        
        /// The number of weeks after which long-term requests are flagged for follow-up
        LongTermUpdateWeeks : int
        
        /// The name from which e-mails are sent
        EmailFromName : string
        
        /// The e-mail address from which e-mails are sent
        EmailFromAddress : string
        
        /// The fonts to use in generating the list of prayer requests
        Fonts : string
        
        /// The color for the prayer request list headings
        HeadingColor : string
        
        /// The color for the lines offsetting the prayer request list headings
        LineColor : string
        
        /// The font size for the headings on the prayer request list
        HeadingFontSize : int
        
        /// The font size for the text on the prayer request list
        TextFontSize : int
        
        /// The order in which the prayer requests are sorted
        RequestSort : RequestSort
        
        /// The password used for "small group login" (view-only request list)
        GroupPassword : string
        
        /// The default e-mail type for this class
        DefaultEmailType : EmailFormat
        
        /// Whether this class makes its request list public
        IsPublic : bool
        
        /// The time zone which this class uses (use tzdata names)
        TimeZoneId : TimeZoneId
        
        /// The number of requests displayed per page
        PageSize : int
        
        /// How the as-of date should be automatically displayed
        AsOfDateDisplay : AsOfDateDisplay
    }
with
    
    /// The list of fonts to use when displaying request lists (converts "native" to native font stack)
    member this.FontStack =
        if this.Fonts = "native" then
            """system-ui,-apple-system,"Segoe UI",Roboto,Ubuntu,"Liberation Sans",Cantarell,"Helvetica Neue",sans-serif"""
        else this.Fonts

/// Functions to support list preferences
module ListPreferences =
    
    /// A set of preferences with their default values
    let empty =
        {   SmallGroupId        = SmallGroupId Guid.Empty
            DaysToExpire        = 14
            DaysToKeepNew       = 7
            LongTermUpdateWeeks = 4
            EmailFromName       = "PrayerTracker"
            EmailFromAddress    = "prayer@bitbadger.solutions"
            Fonts               = "native"
            HeadingColor        = "maroon"
            LineColor           = "navy"
            HeadingFontSize     = 16
            TextFontSize        = 12
            RequestSort         = SortByDate
            GroupPassword       = ""
            DefaultEmailType    = HtmlFormat
            IsPublic            = false
            TimeZoneId          = TimeZoneId "America/Denver"
            PageSize            = 100
            AsOfDateDisplay     = NoDisplay
        }


/// A member of a small group
[<NoComparison; NoEquality>]
type Member =
    {   /// The ID of the small group member
        Id : MemberId
        
        /// The Id of the small group to which this member belongs
        SmallGroupId : SmallGroupId
        
        /// The name of the member
        Name : string
        
        /// The e-mail address for the member
        Email : string
        
        /// The type of e-mail preferred by this member
        Format : EmailFormat option
    }

/// Functions to support small group members
module Member =
    
    /// An empty member
    let empty =
        {   Id           = MemberId Guid.Empty
            SmallGroupId = SmallGroupId Guid.Empty
            Name         = ""
            Email        = ""
            Format       = None
        }


/// This represents a single prayer request
[<NoComparison; NoEquality>]
type PrayerRequest =
    {   /// The ID of this request
        Id : PrayerRequestId
        
        /// The type of the request
        RequestType : PrayerRequestType
        
        /// The ID of the user who entered the request
        UserId : UserId
        
        /// The small group to which this request belongs
        SmallGroupId : SmallGroupId
        
        /// The date/time on which this request was entered
        EnteredDate : Instant
        
        /// The date/time this request was last updated
        UpdatedDate : Instant
        
        /// The name of the requestor or subject, or title of announcement
        Requestor : string option
        
        /// The text of the request
        Text : string
        
        /// Whether the chaplain should be notified for this request
        NotifyChaplain : bool
        
        /// Is this request expired?
        Expiration : Expiration
    }
// functions are below small group functions


/// This represents a small group (Sunday School class, Bible study group, etc.)
[<NoComparison; NoEquality>]
type SmallGroup =
    {   /// The ID of this small group
        Id : SmallGroupId
        
        /// The church to which this group belongs
        ChurchId : ChurchId
        
        /// The name of the group
        Name : string
        
        /// The preferences for the request list
        Preferences : ListPreferences
    }

/// Functions to support small groups
module SmallGroup =
    
    /// An empty small group
    let empty =
        {   Id          = SmallGroupId Guid.Empty
            ChurchId    = ChurchId Guid.Empty
            Name        = "" 
            Preferences = ListPreferences.empty
        }

    /// The DateTimeZone for the time zone ID for this small group
    let timeZone group =
        let tzId = TimeZoneId.toString group.Preferences.TimeZoneId
        if DateTimeZoneProviders.Tzdb.Ids.Contains tzId then DateTimeZoneProviders.Tzdb[tzId]
        else DateTimeZone.Utc
    
    /// Get the local date/time for this group
    let localTimeNow (clock : IClock) group =
        if isNull clock then nullArg (nameof clock)
        clock.GetCurrentInstant().InZone(timeZone group).LocalDateTime

    /// Get the local date for this group
    let localDateNow clock group =
        (localTimeNow clock group).Date


/// Functions to support prayer requests
module PrayerRequest =
    
    /// An empty request
    let empty =
        {   Id             = PrayerRequestId Guid.Empty
            RequestType    = CurrentRequest
            UserId         = UserId Guid.Empty
            SmallGroupId   = SmallGroupId Guid.Empty
            EnteredDate    = Instant.MinValue
            UpdatedDate    = Instant.MinValue
            Requestor      = None
            Text           = "" 
            NotifyChaplain = false
            Expiration     = Automatic
        }

    /// Is this request expired?
    let isExpired (asOf : LocalDate) group req =
        match req.Expiration, req.RequestType with
        | Forced, _ -> true
        | Manual, _ 
        | Automatic, LongTermRequest
        | Automatic, Expecting  -> false
        | Automatic, _ ->
            // Automatic expiration
            Period.Between(req.UpdatedDate.InZone(SmallGroup.timeZone group).Date, asOf, PeriodUnits.Days).Days
                >= group.Preferences.DaysToExpire

    /// Is an update required for this long-term request?
    let updateRequired asOf group req =
        if isExpired asOf group req then false
        else asOf.PlusWeeks -group.Preferences.LongTermUpdateWeeks
                >= req.UpdatedDate.InZone(SmallGroup.timeZone group).Date


/// This represents a user of PrayerTracker
[<NoComparison; NoEquality>]
type User =
    {   /// The ID of this user
        Id : UserId
        
        /// The first name of this user
        FirstName : string
        
        /// The last name of this user
        LastName : string
        
        /// The e-mail address of the user
        Email : string
        
        /// Whether this user is a PrayerTracker system administrator
        IsAdmin : bool
        
        /// The user's hashed password
        PasswordHash : string
        
        /// The last time the user was seen (set whenever the user is loaded into a session)
        LastSeen : Instant option
    }
with
    /// The full name of the user
    member this.Name =
        $"{this.FirstName} {this.LastName}"

/// Functions to support users
module User =
    
    /// An empty user
    let empty =
        {   Id           = UserId Guid.Empty
            FirstName    = ""
            LastName     = ""
            Email        = ""
            IsAdmin      = false
            PasswordHash = ""
            LastSeen     = None
        }


/// Cross-reference between user and small group
[<NoComparison; NoEquality>]
type UserSmallGroup =
    {   /// The Id of the user who has access to the small group
        UserId : UserId
        
        /// The Id of the small group to which the user has access
        SmallGroupId : SmallGroupId
    }

/// Functions to support user/small group cross-reference
module UserSmallGroup =
    
    /// An empty user/small group xref
    let empty =
        {   UserId       = UserId Guid.Empty
            SmallGroupId = SmallGroupId Guid.Empty
        }
