namespace PrayerTracker.Entities

open FSharp.EFCore.OptionConverter
open Microsoft.EntityFrameworkCore
open NodaTime
open System
open System.Collections.Generic

(*-- SUPPORT TYPES --*)

/// How as-of dates should (or should not) be displayed with requests
type AsOfDateDisplay =
  /// No as-of date should be displayed
  | NoDisplay
  /// The as-of date should be displayed in the culture's short date format
  | ShortDate
  /// The as-of date should be displayed in the culture's long date format
  | LongDate
with
  /// Convert to a DU case from a single-character string
  static member fromCode code =
    match code with
    | "N" -> NoDisplay
    | "S" -> ShortDate
    | "L" -> LongDate
    | _ -> invalidArg "code" (sprintf "Unknown code %s" code)
  /// Convert this DU case to a single-character string
  member this.code =
    match this with
    | NoDisplay -> "N"
    | ShortDate -> "S"
    | LongDate -> "L"


/// Acceptable e-mail formats
type EmailFormat =
  /// HTML e-mail
  | HtmlFormat
  /// Plain-text e-mail
  | PlainTextFormat
with
  /// Convert to a DU case from a single-character string
  static member fromCode code =
    match code with
    | "H" -> HtmlFormat
    | "P" -> PlainTextFormat
    | _ -> invalidArg "code" (sprintf "Unknown code %s" code)
  /// Convert this DU case to a single-character string
  member this.code =
    match this with
    | HtmlFormat -> "H"
    | PlainTextFormat -> "P"


/// Expiration for requests
type Expiration =
  /// Follow the rules for normal expiration
  | Automatic
  /// Do not expire via rules
  | Manual
  /// Force immediate expiration
  | Forced
with
  /// Convert to a DU case from a single-character string
  static member fromCode code =
    match code with
    | "A" -> Automatic
    | "M" -> Manual
    | "F" -> Forced
    | _ -> invalidArg "code" (sprintf "Unknown code %s" code)
  /// Convert this DU case to a single-character string
  member this.code =
    match this with
    | Automatic -> "A"
    | Manual -> "M"
    | Forced -> "F"


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
with
  /// Convert to a DU case from a single-character string
  static member fromCode code =
    match code with
    | "C" -> CurrentRequest
    | "L" -> LongTermRequest
    | "E" -> Expecting
    | "P" -> PraiseReport
    | "A" -> Announcement
    | _ -> invalidArg "code" (sprintf "Unknown code %s" code)
  /// Convert this DU case to a single-character string
  member this.code =
    match this with
    | CurrentRequest -> "C"
    | LongTermRequest -> "L"
    | Expecting -> "E"
    | PraiseReport -> "P"
    | Announcement -> "A"


/// How requests should be sorted
type RequestSort =
  /// Sort by date, then by requestor/subject
  | SortByDate
  /// Sort by requestor/subject, then by date
  | SortByRequestor
with
  /// Convert to a DU case from a single-character string
  static member fromCode code =
    match code with
    | "D" -> SortByDate
    | "R" -> SortByRequestor
    | _ -> invalidArg "code" (sprintf "Unknown code %s" code)
  /// Convert this DU case to a single-character string
  member this.code =
    match this with
    | SortByDate -> "D"
    | SortByRequestor -> "R"


module Converters =
  open Microsoft.EntityFrameworkCore.Storage.ValueConversion
  open Microsoft.FSharp.Linq.RuntimeHelpers
  open System.Linq.Expressions

  let private asOfFromDU =
    <@ Func<AsOfDateDisplay, string>(fun (x : AsOfDateDisplay) -> x.code) @>
    |> LeafExpressionConverter.QuotationToExpression
    |> unbox<Expression<Func<AsOfDateDisplay, string>>>

  let private asOfToDU =
    <@ Func<string, AsOfDateDisplay>(AsOfDateDisplay.fromCode) @>
    |> LeafExpressionConverter.QuotationToExpression
    |> unbox<Expression<Func<string, AsOfDateDisplay>>>
  
  let private emailFromDU =
    <@ Func<EmailFormat, string>(fun (x : EmailFormat) -> x.code) @>
    |> LeafExpressionConverter.QuotationToExpression
    |> unbox<Expression<Func<EmailFormat, string>>>

  let private emailToDU =
    <@ Func<string, EmailFormat>(EmailFormat.fromCode) @>
    |> LeafExpressionConverter.QuotationToExpression
    |> unbox<Expression<Func<string, EmailFormat>>>
  
  let private expFromDU =
    <@ Func<Expiration, string>(fun (x : Expiration) -> x.code) @>
    |> LeafExpressionConverter.QuotationToExpression
    |> unbox<Expression<Func<Expiration, string>>>

  let private expToDU =
    <@ Func<string, Expiration>(Expiration.fromCode) @>
    |> LeafExpressionConverter.QuotationToExpression
    |> unbox<Expression<Func<string, Expiration>>>
  
  let private sortFromDU =
    <@ Func<RequestSort, string>(fun (x : RequestSort) -> x.code) @>
    |> LeafExpressionConverter.QuotationToExpression
    |> unbox<Expression<Func<RequestSort, string>>>

  let private sortToDU =
    <@ Func<string, RequestSort>(RequestSort.fromCode) @>
    |> LeafExpressionConverter.QuotationToExpression
    |> unbox<Expression<Func<string, RequestSort>>>
  
  let private typFromDU =
    <@ Func<PrayerRequestType, string>(fun (x : PrayerRequestType) -> x.code) @>
    |> LeafExpressionConverter.QuotationToExpression
    |> unbox<Expression<Func<PrayerRequestType, string>>>

  let private typToDU =
    <@ Func<string, PrayerRequestType>(PrayerRequestType.fromCode) @>
    |> LeafExpressionConverter.QuotationToExpression
    |> unbox<Expression<Func<string, PrayerRequestType>>>
  
  /// Conversion between a string and an AsOfDateDisplay DU value
  type AsOfDateDisplayConverter () =
    inherit ValueConverter<AsOfDateDisplay, string> (asOfFromDU, asOfToDU)

  /// Conversion between a string and an EmailFormat DU value
  type EmailFormatConverter () =
    inherit ValueConverter<EmailFormat, string> (emailFromDU, emailToDU)

  /// Conversion between a string and an Expiration DU value
  type ExpirationConverter () =
    inherit ValueConverter<Expiration, string> (expFromDU, expToDU)

  /// Conversion between a string and an AsOfDateDisplay DU value
  type PrayerRequestTypeConverter () =
    inherit ValueConverter<PrayerRequestType, string> (typFromDU, typToDU)

  /// Conversion between a string and a RequestSort DU value
  type RequestSortConverter () =
    inherit ValueConverter<RequestSort, string> (sortFromDU, sortToDU)


/// Statistics for churches
[<NoComparison; NoEquality>]
type ChurchStats =
  { /// The number of small groups in the church
    smallGroups    : int
    /// The number of prayer requests in the church
    prayerRequests : int
    /// The number of users who can access small groups in the church
    users          : int
    }

/// PK type for the Church entity
type ChurchId = Guid

/// PK type for the Member entity
type MemberId = Guid

/// PK type for the PrayerRequest entity
type PrayerRequestId = Guid

/// PK type for the SmallGroup entity
type SmallGroupId = Guid

/// PK type for the TimeZone entity
type TimeZoneId = string

/// PK type for the User entity
type UserId = Guid

/// PK for User/SmallGroup cross-reference table
type UserSmallGroupKey =
 { userId       : UserId
   smallGroupId : SmallGroupId
   }

(*-- ENTITIES --*)

/// This represents a church
type [<CLIMutable; NoComparison; NoEquality>] Church =
  { /// The Id of this church
    churchId         : ChurchId
    /// The name of the church
    name             : string
    /// The city where the church is
    city             : string
    /// The state where the church is
    st               : string
    /// Does this church have an active interface with Virtual Prayer Room?
    hasInterface     : bool
    /// The address for the interface
    interfaceAddress : string option
    
    /// Small groups for this church
    smallGroups : ICollection<SmallGroup>
    }
  with
    /// An empty church
    // aww... how sad :(
    static member empty =
      { churchId         = Guid.Empty
        name             = ""
        city             = ""
        st               = ""
        hasInterface     = false
        interfaceAddress = None
        smallGroups      = List<SmallGroup> ()
        }
    /// Configure EF for this entity
    static member internal configureEF (mb : ModelBuilder) =
      mb.Entity<Church> (
        fun m ->
          m.ToTable "Church" |> ignore
          m.Property(fun e -> e.churchId).HasColumnName "ChurchId" |> ignore
          m.Property(fun e -> e.name).HasColumnName("Name").IsRequired () |> ignore
          m.Property(fun e -> e.city).HasColumnName("City").IsRequired () |> ignore
          m.Property(fun e -> e.st).HasColumnName("ST").IsRequired().HasMaxLength 2 |> ignore
          m.Property(fun e -> e.hasInterface).HasColumnName "HasVirtualPrayerRoomInterface" |> ignore
          m.Property(fun e -> e.interfaceAddress).HasColumnName "InterfaceAddress" |> ignore)
      |> ignore
      mb.Model.FindEntityType(typeof<Church>).FindProperty("interfaceAddress")
        .SetValueConverter(OptionConverter<string> ())


/// Preferences for the form and format of the prayer request list
and [<CLIMutable; NoComparison; NoEquality>] ListPreferences =
  { /// The Id of the small group to which these preferences belong
    smallGroupId        : SmallGroupId
    /// The days after which regular requests expire
    daysToExpire        : int
    /// The number of days a new or updated request is considered new
    daysToKeepNew       : int
    /// The number of weeks after which long-term requests are flagged for follow-up
    longTermUpdateWeeks : int
    /// The name from which e-mails are sent
    emailFromName       : string
    /// The e-mail address from which e-mails are sent
    emailFromAddress    : string
    /// The fonts to use in generating the list of prayer requests
    listFonts           : string
    /// The color for the prayer request list headings
    headingColor        : string
    /// The color for the lines offsetting the prayer request list headings
    lineColor           : string
    /// The font size for the headings on the prayer request list
    headingFontSize     : int
    /// The font size for the text on the prayer request list
    textFontSize        : int
    /// The order in which the prayer requests are sorted
    requestSort         : RequestSort
    /// The password used for "small group login" (view-only request list)
    groupPassword       : string
    /// The default e-mail type for this class
    defaultEmailType    : EmailFormat
    /// Whether this class makes its request list public
    isPublic            : bool
    /// The time zone which this class uses (use tzdata names)
    timeZoneId          : TimeZoneId
    /// The time zone information
    timeZone            : TimeZone
    /// The number of requests displayed per page
    pageSize            : int
    /// How the as-of date should be automatically displayed
    asOfDateDisplay     : AsOfDateDisplay
    }
  with
    /// A set of preferences with their default values
    static member empty =
      { smallGroupId        = Guid.Empty
        daysToExpire        = 14
        daysToKeepNew       = 7
        longTermUpdateWeeks = 4
        emailFromName       = "PrayerTracker"
        emailFromAddress    = "prayer@djs-consulting.com"
        listFonts           = "Century Gothic,Tahoma,Luxi Sans,sans-serif"
        headingColor        = "maroon"
        lineColor           = "navy"
        headingFontSize     = 16
        textFontSize        = 12
        requestSort         = SortByDate
        groupPassword       = ""
        defaultEmailType    = HtmlFormat
        isPublic            = false
        timeZoneId          = "America/Denver"
        timeZone            = TimeZone.empty
        pageSize            = 100
        asOfDateDisplay     = NoDisplay
      }
    /// Configure EF for this entity
    static member internal configureEF (mb : ModelBuilder) =
      mb.Entity<ListPreferences> (
        fun m ->
          m.ToTable "ListPreference" |> ignore
          m.HasKey (fun e -> e.smallGroupId :> obj) |> ignore
          m.Property(fun e -> e.smallGroupId).HasColumnName "SmallGroupId" |> ignore
          m.Property(fun e -> e.daysToKeepNew)
            .HasColumnName("DaysToKeepNew")
            .IsRequired()
            .HasDefaultValue 7
          |> ignore
          m.Property(fun e -> e.daysToExpire)
            .HasColumnName("DaysToExpire")
            .IsRequired()
            .HasDefaultValue 14
          |> ignore
          m.Property(fun e -> e.longTermUpdateWeeks)
            .HasColumnName("LongTermUpdateWeeks")
            .IsRequired()
            .HasDefaultValue 4
          |> ignore
          m.Property(fun e -> e.emailFromName)
            .HasColumnName("EmailFromName")
            .IsRequired()
            .HasDefaultValue "PrayerTracker"
          |> ignore
          m.Property(fun e -> e.emailFromAddress)
            .HasColumnName("EmailFromAddress")
            .IsRequired()
            .HasDefaultValue "prayer@djs-consulting.com"
          |> ignore
          m.Property(fun e -> e.listFonts)
            .HasColumnName("ListFonts")
            .IsRequired()
            .HasDefaultValue "Century Gothic,Tahoma,Luxi Sans,sans-serif"
          |> ignore
          m.Property(fun e -> e.headingColor)
            .HasColumnName("HeadingColor")
            .IsRequired()
            .HasDefaultValue "maroon"
          |> ignore
          m.Property(fun e -> e.lineColor)
            .HasColumnName("LineColor")
            .IsRequired()
            .HasDefaultValue "navy"
          |> ignore
          m.Property(fun e -> e.headingFontSize)
            .HasColumnName("HeadingFontSize")
            .IsRequired()
            .HasDefaultValue 16
          |> ignore
          m.Property(fun e -> e.textFontSize)
            .HasColumnName("TextFontSize")
            .IsRequired()
            .HasDefaultValue 12
          |> ignore
          m.Property(fun e -> e.requestSort)
            .HasColumnName("RequestSort")
            .IsRequired()
            .HasMaxLength(1)
            .HasDefaultValue SortByDate
          |> ignore
          m.Property(fun e -> e.groupPassword)
            .HasColumnName("GroupPassword")
            .IsRequired()
            .HasDefaultValue ""
          |> ignore
          m.Property(fun e -> e.defaultEmailType)
            .HasColumnName("DefaultEmailType")
            .IsRequired()
            .HasDefaultValue HtmlFormat
          |> ignore
          m.Property(fun e -> e.isPublic)
            .HasColumnName("IsPublic")
            .IsRequired()
            .HasDefaultValue false
          |> ignore
          m.Property(fun e -> e.timeZoneId)
            .HasColumnName("TimeZoneId")
            .IsRequired()
            .HasDefaultValue "America/Denver"
          |> ignore
          m.Property(fun e -> e.pageSize)
            .HasColumnName("PageSize")
            .IsRequired()
            .HasDefaultValue 100
          |> ignore
          m.Property(fun e -> e.asOfDateDisplay)
            .HasColumnName("AsOfDateDisplay")
            .IsRequired()
            .HasMaxLength(1)
            .HasDefaultValue NoDisplay
          |> ignore)
      |> ignore
      mb.Model.FindEntityType(typeof<ListPreferences>).FindProperty("requestSort")
        .SetValueConverter(Converters.RequestSortConverter ())
      mb.Model.FindEntityType(typeof<ListPreferences>).FindProperty("defaultEmailType")
        .SetValueConverter(Converters.EmailFormatConverter ())
      mb.Model.FindEntityType(typeof<ListPreferences>).FindProperty("asOfDateDisplay")
        .SetValueConverter(Converters.AsOfDateDisplayConverter ())


/// A member of a small group
and [<CLIMutable; NoComparison; NoEquality>] Member =
  { /// The Id of the member
    memberId     : MemberId
    /// The Id of the small group to which this member belongs
    smallGroupId : SmallGroupId
    /// The name of the member
    memberName   : string
    /// The e-mail address for the member
    email        : string
    /// The type of e-mail preferred by this member (see <see cref="EmailTypes"/> constants)
    format       : string option // TODO - do I need a custom formatter for this?
    /// The small group to which this member belongs
    smallGroup   : SmallGroup
    }
  with
    /// An empty member
    static member empty =
      { memberId     = Guid.Empty
        smallGroupId = Guid.Empty
        memberName   = ""
        email        = ""
        format       = None
        smallGroup   = SmallGroup.empty
        }
    /// Configure EF for this entity
    static member internal configureEF (mb : ModelBuilder) =
      mb.Entity<Member> (
        fun m ->
          m.ToTable "Member" |> ignore
          m.Property(fun e -> e.memberId).HasColumnName "MemberId" |> ignore
          m.Property(fun e -> e.smallGroupId).HasColumnName "SmallGroupId" |> ignore
          m.Property(fun e -> e.memberName).HasColumnName("MemberName").IsRequired() |> ignore
          m.Property(fun e -> e.email).HasColumnName("Email").IsRequired() |> ignore
          m.Property(fun e -> e.format).HasColumnName "Format" |> ignore)
      |> ignore
      mb.Model.FindEntityType(typeof<Member>).FindProperty("format").SetValueConverter(OptionConverter<string> ())


/// This represents a single prayer request
and [<CLIMutable; NoComparison; NoEquality>] PrayerRequest =
  { /// The Id of this request
    prayerRequestId : PrayerRequestId
    /// The type of the request
    requestType     : PrayerRequestType
    /// The user who entered the request
    userId          : UserId
    /// The small group to which this request belongs
    smallGroupId    : SmallGroupId
    /// The date/time on which this request was entered
    enteredDate     : DateTime
    /// The date/time this request was last updated
    updatedDate     : DateTime
    /// The name of the requestor or subject, or title of announcement
    requestor       : string option
    /// The text of the request
    text            : string
    /// Whether the chaplain should be notified for this request
    notifyChaplain  : bool
    /// The user who entered this request
    user            : User
    /// The small group to which this request belongs
    smallGroup      : SmallGroup
    /// Is this request expired?
    expiration      : Expiration
    }
  with
    /// An empty request
    static member empty =
      { prayerRequestId = Guid.Empty
        requestType     = CurrentRequest
        userId          = Guid.Empty
        smallGroupId    = Guid.Empty
        enteredDate     = DateTime.MinValue
        updatedDate     = DateTime.MinValue
        requestor       = None
        text            = "" 
        notifyChaplain  = false
        user            = User.empty
        smallGroup      = SmallGroup.empty
        expiration      = Automatic
        }
    /// Is this request expired?
    member this.isExpired (curr : DateTime) expDays =
      match this.expiration with
      | Forced -> true
      | Manual -> false 
      | Automatic ->
          match this.requestType with
          | LongTermRequest
          | Expecting -> false
          | _ -> curr.AddDays(-(float expDays)) > this.updatedDate // Automatic expiration

    /// Is an update required for this long-term request?
    member this.updateRequired curr expDays updWeeks =
      match this.isExpired curr expDays with
      | true -> false
      | false -> curr.AddDays(-(float (updWeeks * 7))) > this.updatedDate

    /// Configure EF for this entity
    static member internal configureEF (mb : ModelBuilder) =
      mb.Entity<PrayerRequest> (
        fun m ->
          m.ToTable "PrayerRequest" |> ignore
          m.Property(fun e -> e.prayerRequestId).HasColumnName "PrayerRequestId" |> ignore
          m.Property(fun e -> e.requestType).HasColumnName("RequestType").IsRequired() |> ignore
          m.Property(fun e -> e.userId).HasColumnName "UserId" |> ignore
          m.Property(fun e -> e.smallGroupId).HasColumnName "SmallGroupId" |> ignore
          m.Property(fun e -> e.enteredDate).HasColumnName "EnteredDate" |> ignore
          m.Property(fun e -> e.updatedDate).HasColumnName "UpdatedDate" |> ignore
          m.Property(fun e -> e.requestor).HasColumnName "Requestor" |> ignore
          m.Property(fun e -> e.text).HasColumnName("Text").IsRequired() |> ignore
          m.Property(fun e -> e.notifyChaplain).HasColumnName "NotifyChaplain" |> ignore
          m.Property(fun e -> e.expiration).HasColumnName "Expiration" |> ignore)
      |> ignore
      mb.Model.FindEntityType(typeof<PrayerRequest>).FindProperty("requestType")
        .SetValueConverter(Converters.PrayerRequestTypeConverter ())
      mb.Model.FindEntityType(typeof<PrayerRequest>).FindProperty("requestor")
        .SetValueConverter(OptionConverter<string> ())
      mb.Model.FindEntityType(typeof<PrayerRequest>).FindProperty("expiration")
        .SetValueConverter(Converters.ExpirationConverter ())


/// This represents a small group (Sunday School class, Bible study group, etc.)
and [<CLIMutable; NoComparison; NoEquality>] SmallGroup =
  { /// The Id of this small group
    smallGroupId   : SmallGroupId
    /// The church to which this group belongs
    churchId       : ChurchId
    /// The name of the group
    name           : string
    /// The church to which this small group belongs
    church         : Church
    /// The preferences for the request list
    preferences    : ListPreferences
    /// The members of the group
    members        : ICollection<Member>
    /// Prayer requests for this small group
    prayerRequests : ICollection<PrayerRequest>
    /// The users authorized to manage this group
    users          : ICollection<UserSmallGroup>
    }
  with
    /// An empty small group
    static member empty =
      { smallGroupId   = Guid.Empty
        churchId       = Guid.Empty
        name           = "" 
        church         = Church.empty
        preferences    = ListPreferences.empty
        members        = List<Member> ()
        prayerRequests = List<PrayerRequest> ()
        users          = List<UserSmallGroup> ()
        }

    /// Get the local date for this group
    member this.localTimeNow (clock : IClock) =
      match clock with null -> nullArg "clock" | _ -> ()
      let tz =
        match DateTimeZoneProviders.Tzdb.Ids.Contains this.preferences.timeZoneId with
        | true -> DateTimeZoneProviders.Tzdb.[this.preferences.timeZoneId]
        | false -> DateTimeZone.Utc
      clock.GetCurrentInstant().InZone(tz).ToDateTimeUnspecified()

    /// Get the local date for this group
    member this.localDateNow clock =
      (this.localTimeNow clock).Date
    
    /// Configure EF for this entity
    static member internal configureEF (mb : ModelBuilder) =
      mb.Entity<SmallGroup> (
        fun m ->
          m.ToTable "SmallGroup" |> ignore
          m.Property(fun e -> e.smallGroupId).HasColumnName "SmallGroupId" |> ignore
          m.Property(fun e -> e.churchId).HasColumnName "ChurchId" |> ignore
          m.Property(fun e -> e.name).HasColumnName("Name").IsRequired() |> ignore
          m.HasOne(fun e -> e.preferences) |> ignore)
      |> ignore


/// This represents a time zone in which a class may reside
and [<CLIMutable; NoComparison; NoEquality>] TimeZone =
  { /// The Id for this time zone (uses tzdata names)
    timeZoneId  : TimeZoneId
    /// The description of this time zone
    description : string
    /// The order in which this timezone should be displayed
    sortOrder   : int
    /// Whether this timezone is active
    isActive    : bool
    }
  with
    /// An empty time zone
    static member empty =
      { timeZoneId  = ""
        description = ""
        sortOrder   = 0
        isActive    = false
        }
    /// Configure EF for this entity
    static member internal configureEF (mb : ModelBuilder) =
      mb.Entity<TimeZone> (
        fun m ->
          m.ToTable "TimeZone" |> ignore
          m.Property(fun e -> e.timeZoneId).HasColumnName "TimeZoneId" |> ignore
          m.Property(fun e -> e.description).HasColumnName("Description").IsRequired() |> ignore
          m.Property(fun e -> e.sortOrder).HasColumnName "SortOrder" |> ignore
          m.Property(fun e -> e.isActive).HasColumnName "IsActive" |> ignore)
      |> ignore


/// This represents a user of PrayerTracker
and [<CLIMutable; NoComparison; NoEquality>] User =
  { /// The Id of this user
    userId       : UserId
    /// The first name of this user
    firstName    : string
    /// The last name of this user
    lastName     : string
    /// The e-mail address of the user
    emailAddress : string
    /// Whether this user is a PrayerTracker system administrator
    isAdmin      : bool
    /// The user's hashed password
    passwordHash : string
    /// The salt for the user's hashed password
    salt         : Guid option
    /// The small groups which this user is authorized
    smallGroups  : ICollection<UserSmallGroup>
    }
  with
    /// An empty user
    static member empty =
      { userId       = Guid.Empty
        firstName    = ""
        lastName     = ""
        emailAddress = ""
        isAdmin      = false
        passwordHash = ""
        salt         = None
        smallGroups  = List<UserSmallGroup> ()
        }
    /// The full name of the user
    member this.fullName =
      sprintf "%s %s" this.firstName this.lastName

    /// Configure EF for this entity
    static member internal configureEF (mb : ModelBuilder) =
      mb.Entity<User> (
        fun m ->
          m.ToTable "User" |> ignore
          m.Ignore(fun e -> e.fullName :> obj) |> ignore
          m.Property(fun e -> e.userId).HasColumnName "UserId" |> ignore
          m.Property(fun e -> e.firstName).HasColumnName("FirstName").IsRequired() |> ignore
          m.Property(fun e -> e.lastName).HasColumnName("LastName").IsRequired() |> ignore
          m.Property(fun e -> e.emailAddress).HasColumnName("EmailAddress").IsRequired() |> ignore
          m.Property(fun e -> e.isAdmin).HasColumnName "IsSystemAdmin" |> ignore
          m.Property(fun e -> e.passwordHash).HasColumnName("PasswordHash").IsRequired() |> ignore
          m.Property(fun e -> e.salt).HasColumnName "Salt" |> ignore)
      |> ignore
      mb.Model.FindEntityType(typeof<User>).FindProperty("salt")
        .SetValueConverter(OptionConverter<Guid> ())


/// Cross-reference between user and small group
and [<CLIMutable; NoComparison; NoEquality>] UserSmallGroup =
  { /// The Id of the user who has access to the small group
    userId       : UserId
    /// The Id of the small group to which the user has access
    smallGroupId : SmallGroupId
    /// The user who has access to the small group
    user         : User
    /// The small group to which the user has access
    smallGroup   : SmallGroup
    }
  with
    /// An empty user/small group xref
    static member empty =
      { userId       = Guid.Empty
        smallGroupId = Guid.Empty
        user         = User.empty
        smallGroup   = SmallGroup.empty
        }
    /// Configure EF for this entity
    static member internal configureEF (mb : ModelBuilder) =
      mb.Entity<UserSmallGroup> (
        fun m ->
          m.ToTable "User_SmallGroup" |> ignore
          m.HasKey(fun e -> { userId = e.userId; smallGroupId = e.smallGroupId } :> obj) |> ignore
          m.Property(fun e -> e.userId).HasColumnName "UserId" |> ignore
          m.Property(fun e -> e.smallGroupId).HasColumnName "SmallGroupId" |> ignore
          m.HasOne(fun e -> e.user)
            .WithMany(fun e -> e.smallGroups :> IEnumerable<UserSmallGroup>)
            .HasForeignKey(fun e -> e.userId :> obj)
          |> ignore
          m.HasOne(fun e -> e.smallGroup)
            .WithMany(fun e -> e.users :> IEnumerable<UserSmallGroup>)
            .HasForeignKey(fun e -> e.smallGroupId :> obj)
          |> ignore)
      |> ignore
