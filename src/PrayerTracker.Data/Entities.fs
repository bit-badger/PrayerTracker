namespace PrayerTracker.Entities

// fsharplint:disable RecordFieldNames MemberNames

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


/// EF Core value converters for the discriminated union types above
module Converters =

    open Microsoft.EntityFrameworkCore.Storage.ValueConversion
    open Microsoft.FSharp.Linq.RuntimeHelpers
    open System.Linq.Expressions

    let private asOfFromDU =
        <@ Func<AsOfDateDisplay, string>(AsOfDateDisplay.toCode) @>
        |> LeafExpressionConverter.QuotationToExpression
        |> unbox<Expression<Func<AsOfDateDisplay, string>>>

    let private asOfToDU =
        <@ Func<string, AsOfDateDisplay>(AsOfDateDisplay.fromCode) @>
        |> LeafExpressionConverter.QuotationToExpression
        |> unbox<Expression<Func<string, AsOfDateDisplay>>>
    
    let private churchIdFromDU =
        <@ Func<ChurchId, Guid>(fun it -> it.Value) @>
        |> LeafExpressionConverter.QuotationToExpression
        |> unbox<Expression<Func<ChurchId, Guid>>>
    
    let private churchIdToDU =
        <@ Func<Guid, ChurchId>(ChurchId) @>
        |> LeafExpressionConverter.QuotationToExpression
        |> unbox<Expression<Func<Guid, ChurchId>>>
    
    let private emailFromDU =
        <@ Func<EmailFormat, string>(EmailFormat.toCode) @>
        |> LeafExpressionConverter.QuotationToExpression
        |> unbox<Expression<Func<EmailFormat, string>>>

    let private emailToDU =
        <@ Func<string, EmailFormat>(EmailFormat.fromCode) @>
        |> LeafExpressionConverter.QuotationToExpression
        |> unbox<Expression<Func<string, EmailFormat>>>
    
    let private emailOptionFromDU =
        <@ Func<EmailFormat option, string>(fun opt ->
            match opt with Some fmt -> EmailFormat.toCode fmt | None -> null) @>
        |> LeafExpressionConverter.QuotationToExpression
        |> unbox<Expression<Func<EmailFormat option, string>>>

    let private emailOptionToDU =
        <@ Func<string, EmailFormat option>(fun opt ->
            match opt with "" | null -> None | it -> Some (EmailFormat.fromCode it)) @>
        |> LeafExpressionConverter.QuotationToExpression
        |> unbox<Expression<Func<string, EmailFormat option>>>
    
    let private expFromDU =
        <@ Func<Expiration, string>(Expiration.toCode) @>
        |> LeafExpressionConverter.QuotationToExpression
        |> unbox<Expression<Func<Expiration, string>>>

    let private expToDU =
        <@ Func<string, Expiration>(Expiration.fromCode) @>
        |> LeafExpressionConverter.QuotationToExpression
        |> unbox<Expression<Func<string, Expiration>>>
    
    let private memberIdFromDU =
        <@ Func<MemberId, Guid>(fun it -> it.Value) @>
        |> LeafExpressionConverter.QuotationToExpression
        |> unbox<Expression<Func<MemberId, Guid>>>
    
    let private memberIdToDU =
        <@ Func<Guid, MemberId>(MemberId) @>
        |> LeafExpressionConverter.QuotationToExpression
        |> unbox<Expression<Func<Guid, MemberId>>>
    
    let private prayerReqIdFromDU =
        <@ Func<PrayerRequestId, Guid>(fun it -> it.Value) @>
        |> LeafExpressionConverter.QuotationToExpression
        |> unbox<Expression<Func<PrayerRequestId, Guid>>>
    
    let private prayerReqIdToDU =
        <@ Func<Guid, PrayerRequestId>(PrayerRequestId) @>
        |> LeafExpressionConverter.QuotationToExpression
        |> unbox<Expression<Func<Guid, PrayerRequestId>>>
    
    let private smallGrpIdFromDU =
        <@ Func<SmallGroupId, Guid>(fun it -> it.Value) @>
        |> LeafExpressionConverter.QuotationToExpression
        |> unbox<Expression<Func<SmallGroupId, Guid>>>
    
    let private smallGrpIdToDU =
        <@ Func<Guid, SmallGroupId>(SmallGroupId) @>
        |> LeafExpressionConverter.QuotationToExpression
        |> unbox<Expression<Func<Guid, SmallGroupId>>>
    
    let private sortFromDU =
        <@ Func<RequestSort, string>(RequestSort.toCode) @>
        |> LeafExpressionConverter.QuotationToExpression
        |> unbox<Expression<Func<RequestSort, string>>>

    let private sortToDU =
        <@ Func<string, RequestSort>(RequestSort.fromCode) @>
        |> LeafExpressionConverter.QuotationToExpression
        |> unbox<Expression<Func<string, RequestSort>>>
    
    let private typFromDU =
        <@ Func<PrayerRequestType, string>(PrayerRequestType.toCode) @>
        |> LeafExpressionConverter.QuotationToExpression
        |> unbox<Expression<Func<PrayerRequestType, string>>>

    let private typToDU =
        <@ Func<string, PrayerRequestType>(PrayerRequestType.fromCode) @>
        |> LeafExpressionConverter.QuotationToExpression
        |> unbox<Expression<Func<string, PrayerRequestType>>>
    
    let private tzIdFromDU =
        <@ Func<TimeZoneId, string>(TimeZoneId.toString) @>
        |> LeafExpressionConverter.QuotationToExpression
        |> unbox<Expression<Func<TimeZoneId, string>>>
    
    let private tzIdToDU =
        <@ Func<string, TimeZoneId>(TimeZoneId) @>
        |> LeafExpressionConverter.QuotationToExpression
        |> unbox<Expression<Func<string, TimeZoneId>>>
    
    let private userIdFromDU =
        <@ Func<UserId, Guid>(fun it -> it.Value) @>
        |> LeafExpressionConverter.QuotationToExpression
        |> unbox<Expression<Func<UserId, Guid>>>
    
    let private userIdToDU =
        <@ Func<Guid, UserId>(UserId) @>
        |> LeafExpressionConverter.QuotationToExpression
        |> unbox<Expression<Func<Guid, UserId>>>
    
    /// Conversion between a string and an AsOfDateDisplay DU value
    type AsOfDateDisplayConverter () =
        inherit ValueConverter<AsOfDateDisplay, string> (asOfFromDU, asOfToDU)

    /// Conversion between a GUID and a church ID
    type ChurchIdConverter () =
        inherit ValueConverter<ChurchId, Guid> (churchIdFromDU, churchIdToDU)
    
    /// Conversion between a string and an EmailFormat DU value
    type EmailFormatConverter () =
        inherit ValueConverter<EmailFormat, string> (emailFromDU, emailToDU)

    /// Conversion between a string an an optional EmailFormat DU value
    type EmailFormatOptionConverter () =
        inherit ValueConverter<EmailFormat option, string> (emailOptionFromDU, emailOptionToDU)
    
    /// Conversion between a string and an Expiration DU value
    type ExpirationConverter () =
        inherit ValueConverter<Expiration, string> (expFromDU, expToDU)

    /// Conversion between a GUID and a member ID
    type MemberIdConverter () =
        inherit ValueConverter<MemberId, Guid> (memberIdFromDU, memberIdToDU)
    
    /// Conversion between a GUID and a prayer request ID
    type PrayerRequestIdConverter () =
        inherit ValueConverter<PrayerRequestId, Guid> (prayerReqIdFromDU, prayerReqIdToDU)
    
    /// Conversion between a string and a PrayerRequestType DU value
    type PrayerRequestTypeConverter () =
        inherit ValueConverter<PrayerRequestType, string> (typFromDU, typToDU)

    /// Conversion between a string and a RequestSort DU value
    type RequestSortConverter () =
        inherit ValueConverter<RequestSort, string> (sortFromDU, sortToDU)
    
    /// Conversion between a GUID and a small group ID
    type SmallGroupIdConverter () =
        inherit ValueConverter<SmallGroupId, Guid> (smallGrpIdFromDU, smallGrpIdToDU)

    /// Conversion between a string and a time zone ID
    type TimeZoneIdConverter () =
        inherit ValueConverter<TimeZoneId, string> (tzIdFromDU, tzIdToDU)

    /// Conversion between a GUID and a user ID
    type UserIdConverter () =
        inherit ValueConverter<UserId, Guid> (userIdFromDU, userIdToDU)


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

(*-- ENTITIES --*)

open System.Collections.Generic
open FSharp.EFCore.OptionConverter
open Microsoft.EntityFrameworkCore
open NodaTime

/// This represents a church
type [<CLIMutable; NoComparison; NoEquality>] Church =
    {   /// The ID of this church
        Id : ChurchId
        
        /// The name of the church
        Name : string
        
        /// The city where the church is
        City : string
        
        /// The 2-letter state or province code for the church's location
        State : string
        
        /// Does this church have an active interface with Virtual Prayer Room?
        HasInterface : bool
        
        /// The address for the interface
        InterfaceAddress : string option
        
        /// Small groups for this church
        SmallGroups : ICollection<SmallGroup>
    }
with
    /// An empty church
    // aww... how sad :(
    static member empty =
        {   Id               = ChurchId Guid.Empty
            Name             = ""
            City             = ""
            State            = ""
            HasInterface     = false
            InterfaceAddress = None
            SmallGroups      = List<SmallGroup> ()
        }
    
    /// Configure EF for this entity
    static member internal configureEF (mb : ModelBuilder) =
        mb.Entity<Church> (fun m ->
            m.ToTable "Church" |> ignore
            m.Property(fun e -> e.Id).HasColumnName "ChurchId" |> ignore
            m.Property(fun e -> e.Name).HasColumnName("Name").IsRequired () |> ignore
            m.Property(fun e -> e.City).HasColumnName("City").IsRequired () |> ignore
            m.Property(fun e -> e.State).HasColumnName("ST").IsRequired().HasMaxLength 2 |> ignore
            m.Property(fun e -> e.HasInterface).HasColumnName "HasVirtualPrayerRoomInterface" |> ignore
            m.Property(fun e -> e.InterfaceAddress).HasColumnName "InterfaceAddress" |> ignore)
        |> ignore
        mb.Model.FindEntityType(typeof<Church>).FindProperty("Id").SetValueConverter(Converters.ChurchIdConverter ())
        mb.Model.FindEntityType(typeof<Church>).FindProperty("InterfaceAddress")
            .SetValueConverter(OptionConverter<string> ())


/// Preferences for the form and format of the prayer request list
and [<CLIMutable; NoComparison; NoEquality>] ListPreferences =
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
        
        /// The time zone information
        TimeZone : TimeZone
        
        /// The number of requests displayed per page
        PageSize : int
        
        /// How the as-of date should be automatically displayed
        AsOfDateDisplay : AsOfDateDisplay
    }
with
    
    /// A set of preferences with their default values
    static member empty =
        {   SmallGroupId        = SmallGroupId Guid.Empty
            DaysToExpire        = 14
            DaysToKeepNew       = 7
            LongTermUpdateWeeks = 4
            EmailFromName       = "PrayerTracker"
            EmailFromAddress    = "prayer@djs-consulting.com"
            Fonts               = "Century Gothic,Tahoma,Luxi Sans,sans-serif"
            HeadingColor        = "maroon"
            LineColor           = "navy"
            HeadingFontSize     = 16
            TextFontSize        = 12
            RequestSort         = SortByDate
            GroupPassword       = ""
            DefaultEmailType    = HtmlFormat
            IsPublic            = false
            TimeZoneId          = TimeZoneId "America/Denver"
            TimeZone            = TimeZone.empty
            PageSize            = 100
            AsOfDateDisplay     = NoDisplay
        }
    
    /// Configure EF for this entity
    static member internal configureEF (mb : ModelBuilder) =
        mb.Entity<ListPreferences> (fun m ->
            m.ToTable "ListPreference" |> ignore
            m.HasKey (fun e -> e.SmallGroupId :> obj) |> ignore
            m.Property(fun e -> e.SmallGroupId).HasColumnName "SmallGroupId" |> ignore
            m.Property(fun e -> e.DaysToKeepNew)
                .HasColumnName("DaysToKeepNew")
                .IsRequired()
                .HasDefaultValue 7
            |> ignore
            m.Property(fun e -> e.DaysToExpire)
                .HasColumnName("DaysToExpire")
                .IsRequired()
                .HasDefaultValue 14
            |> ignore
            m.Property(fun e -> e.LongTermUpdateWeeks)
                .HasColumnName("LongTermUpdateWeeks")
                .IsRequired()
                .HasDefaultValue 4
            |> ignore
            m.Property(fun e -> e.EmailFromName)
                .HasColumnName("EmailFromName")
                .IsRequired()
                .HasDefaultValue "PrayerTracker"
            |> ignore
            m.Property(fun e -> e.EmailFromAddress)
                .HasColumnName("EmailFromAddress")
                .IsRequired()
                .HasDefaultValue "prayer@djs-consulting.com"
              |> ignore
            m.Property(fun e -> e.Fonts)
                .HasColumnName("ListFonts")
                .IsRequired()
                .HasDefaultValue "Century Gothic,Tahoma,Luxi Sans,sans-serif"
            |> ignore
            m.Property(fun e -> e.HeadingColor)
                .HasColumnName("HeadingColor")
                .IsRequired()
                .HasDefaultValue "maroon"
            |> ignore
            m.Property(fun e -> e.LineColor)
                .HasColumnName("LineColor")
                .IsRequired()
                .HasDefaultValue "navy"
            |> ignore
            m.Property(fun e -> e.HeadingFontSize)
                .HasColumnName("HeadingFontSize")
                .IsRequired()
                .HasDefaultValue 16
            |> ignore
            m.Property(fun e -> e.TextFontSize)
                .HasColumnName("TextFontSize")
                .IsRequired()
                .HasDefaultValue 12
            |> ignore
            m.Property(fun e -> e.RequestSort)
                .HasColumnName("RequestSort")
                .IsRequired()
                .HasMaxLength(1)
                .HasDefaultValue SortByDate
            |> ignore
            m.Property(fun e -> e.GroupPassword)
                .HasColumnName("GroupPassword")
                .IsRequired()
                .HasDefaultValue ""
            |> ignore
            m.Property(fun e -> e.DefaultEmailType)
                .HasColumnName("DefaultEmailType")
                .IsRequired()
                .HasDefaultValue HtmlFormat
            |> ignore
            m.Property(fun e -> e.IsPublic)
                .HasColumnName("IsPublic")
                .IsRequired()
                .HasDefaultValue false
            |> ignore
            m.Property(fun e -> e.TimeZoneId)
                .HasColumnName("TimeZoneId")
                .IsRequired()
                .HasDefaultValue "America/Denver"
            |> ignore
            m.Property(fun e -> e.PageSize)
                .HasColumnName("PageSize")
                .IsRequired()
                .HasDefaultValue 100
            |> ignore
            m.Property(fun e -> e.AsOfDateDisplay)
                .HasColumnName("AsOfDateDisplay")
                .IsRequired()
                .HasMaxLength(1)
                .HasDefaultValue NoDisplay
            |> ignore)
        |> ignore
        mb.Model.FindEntityType(typeof<ListPreferences>).FindProperty("SmallGroupId")
            .SetValueConverter(Converters.SmallGroupIdConverter ())
        mb.Model.FindEntityType(typeof<ListPreferences>).FindProperty("RequestSort")
            .SetValueConverter(Converters.RequestSortConverter ())
        mb.Model.FindEntityType(typeof<ListPreferences>).FindProperty("DefaultEmailType")
            .SetValueConverter(Converters.EmailFormatConverter ())
        mb.Model.FindEntityType(typeof<ListPreferences>).FindProperty("TimeZoneId")
            .SetValueConverter(Converters.TimeZoneIdConverter ())
        mb.Model.FindEntityType(typeof<ListPreferences>).FindProperty("AsOfDateDisplay")
            .SetValueConverter(Converters.AsOfDateDisplayConverter ())


/// A member of a small group
and [<CLIMutable; NoComparison; NoEquality>] Member =
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
        
        /// The small group to which this member belongs
        SmallGroup : SmallGroup
    }
with
    
    /// An empty member
    static member empty =
        {   Id           = MemberId Guid.Empty
            SmallGroupId = SmallGroupId Guid.Empty
            Name         = ""
            Email        = ""
            Format       = None
            SmallGroup   = SmallGroup.empty
        }
    
    /// Configure EF for this entity
    static member internal configureEF (mb : ModelBuilder) =
        mb.Entity<Member> (fun m ->
            m.ToTable "Member" |> ignore
            m.Property(fun e -> e.Id).HasColumnName "MemberId" |> ignore
            m.Property(fun e -> e.SmallGroupId).HasColumnName "SmallGroupId" |> ignore
            m.Property(fun e -> e.Name).HasColumnName("MemberName").IsRequired() |> ignore
            m.Property(fun e -> e.Email).HasColumnName("Email").IsRequired() |> ignore
            m.Property(fun e -> e.Format).HasColumnName "Format" |> ignore)
        |> ignore
        mb.Model.FindEntityType(typeof<Member>).FindProperty("Id").SetValueConverter(Converters.MemberIdConverter ())
        mb.Model.FindEntityType(typeof<Member>).FindProperty("SmallGroupId")
            .SetValueConverter(Converters.SmallGroupIdConverter ())
        mb.Model.FindEntityType(typeof<Member>).FindProperty("Format")
            .SetValueConverter(Converters.EmailFormatOptionConverter ())


/// This represents a single prayer request
and [<CLIMutable; NoComparison; NoEquality>] PrayerRequest =
    {   /// The ID of this request
        Id : PrayerRequestId
        
        /// The type of the request
        RequestType : PrayerRequestType
        
        /// The ID of the user who entered the request
        UserId : UserId
        
        /// The small group to which this request belongs
        SmallGroupId : SmallGroupId
        
        /// The date/time on which this request was entered
        EnteredDate : DateTime
        
        /// The date/time this request was last updated
        UpdatedDate : DateTime
        
        /// The name of the requestor or subject, or title of announcement
        Requestor : string option
        
        /// The text of the request
        Text : string
        
        /// Whether the chaplain should be notified for this request
        NotifyChaplain : bool
        
        /// The user who entered this request
        User : User
        
        /// The small group to which this request belongs
        SmallGroup : SmallGroup
        
        /// Is this request expired?
        Expiration : Expiration
    }
with
    
    /// An empty request
    static member empty =
        {   Id              = PrayerRequestId Guid.Empty
            RequestType     = CurrentRequest
            UserId          = UserId Guid.Empty
            SmallGroupId    = SmallGroupId Guid.Empty
            EnteredDate     = DateTime.MinValue
            UpdatedDate     = DateTime.MinValue
            Requestor       = None
            Text            = "" 
            NotifyChaplain  = false
            User            = User.empty
            SmallGroup      = SmallGroup.empty
            Expiration      = Automatic
        }
    
    /// Is this request expired?
    member this.isExpired (curr : DateTime) expDays =
        match this.Expiration with
        | Forced -> true
        | Manual -> false 
        | Automatic ->
            match this.RequestType with
            | LongTermRequest
            | Expecting -> false
            | _ -> curr.AddDays(-(float expDays)).Date > this.UpdatedDate.Date // Automatic expiration

    /// Is an update required for this long-term request?
    member this.updateRequired curr expDays updWeeks =
        match this.isExpired curr expDays with
        | true -> false
        | false -> curr.AddDays(-(float (updWeeks * 7))).Date > this.UpdatedDate.Date

    /// Configure EF for this entity
    static member internal configureEF (mb : ModelBuilder) =
        mb.Entity<PrayerRequest> (fun m ->
            m.ToTable "PrayerRequest" |> ignore
            m.Property(fun e -> e.Id).HasColumnName "PrayerRequestId" |> ignore
            m.Property(fun e -> e.RequestType).HasColumnName("RequestType").IsRequired() |> ignore
            m.Property(fun e -> e.UserId).HasColumnName "UserId" |> ignore
            m.Property(fun e -> e.SmallGroupId).HasColumnName "SmallGroupId" |> ignore
            m.Property(fun e -> e.EnteredDate).HasColumnName "EnteredDate" |> ignore
            m.Property(fun e -> e.UpdatedDate).HasColumnName "UpdatedDate" |> ignore
            m.Property(fun e -> e.Requestor).HasColumnName "Requestor" |> ignore
            m.Property(fun e -> e.Text).HasColumnName("Text").IsRequired() |> ignore
            m.Property(fun e -> e.NotifyChaplain).HasColumnName "NotifyChaplain" |> ignore
            m.Property(fun e -> e.Expiration).HasColumnName "Expiration" |> ignore)
        |> ignore
        mb.Model.FindEntityType(typeof<PrayerRequest>).FindProperty("Id")
            .SetValueConverter(Converters.PrayerRequestIdConverter ())
        mb.Model.FindEntityType(typeof<PrayerRequest>).FindProperty("RequestType")
            .SetValueConverter(Converters.PrayerRequestTypeConverter ())
        mb.Model.FindEntityType(typeof<PrayerRequest>).FindProperty("UserId")
            .SetValueConverter(Converters.UserIdConverter ())
        mb.Model.FindEntityType(typeof<PrayerRequest>).FindProperty("SmallGroupId")
            .SetValueConverter(Converters.SmallGroupIdConverter ())
        mb.Model.FindEntityType(typeof<PrayerRequest>).FindProperty("Requestor")
            .SetValueConverter(OptionConverter<string> ())
        mb.Model.FindEntityType(typeof<PrayerRequest>).FindProperty("Expiration")
            .SetValueConverter(Converters.ExpirationConverter ())


/// This represents a small group (Sunday School class, Bible study group, etc.)
and [<CLIMutable; NoComparison; NoEquality>] SmallGroup =
    {   /// The ID of this small group
        Id : SmallGroupId
        
        /// The church to which this group belongs
        ChurchId : ChurchId
        
        /// The name of the group
        Name : string
        
        /// The church to which this small group belongs
        Church : Church
        
        /// The preferences for the request list
        Preferences : ListPreferences
        
        /// The members of the group
        Members : ICollection<Member>
        
        /// Prayer requests for this small group
        PrayerRequests : ICollection<PrayerRequest>
        
        /// The users authorized to manage this group
        Users : ICollection<UserSmallGroup>
    }
with
    
    /// An empty small group
    static member empty =
        {   Id             = SmallGroupId Guid.Empty
            ChurchId       = ChurchId Guid.Empty
            Name           = "" 
            Church         = Church.empty
            Preferences    = ListPreferences.empty
            Members        = List<Member> ()
            PrayerRequests = List<PrayerRequest> ()
            Users          = List<UserSmallGroup> ()
        }

    /// Get the local date for this group
    member this.localTimeNow (clock : IClock) =
        match clock with null -> nullArg "clock" | _ -> ()
        let tzId = TimeZoneId.toString this.Preferences.TimeZoneId
        let tz =
            if DateTimeZoneProviders.Tzdb.Ids.Contains tzId then DateTimeZoneProviders.Tzdb[tzId]
            else DateTimeZone.Utc
        clock.GetCurrentInstant().InZone(tz).ToDateTimeUnspecified ()

    /// Get the local date for this group
    member this.localDateNow clock =
        (this.localTimeNow clock).Date
    
    /// Configure EF for this entity
    static member internal configureEF (mb : ModelBuilder) =
        mb.Entity<SmallGroup> (fun m ->
            m.ToTable "SmallGroup" |> ignore
            m.Property(fun e -> e.Id).HasColumnName "SmallGroupId" |> ignore
            m.Property(fun e -> e.ChurchId).HasColumnName "ChurchId" |> ignore
            m.Property(fun e -> e.Name).HasColumnName("Name").IsRequired() |> ignore
            m.HasOne(fun e -> e.Preferences) |> ignore)
        |> ignore
        mb.Model.FindEntityType(typeof<SmallGroup>).FindProperty("Id")
            .SetValueConverter(Converters.SmallGroupIdConverter ())
        mb.Model.FindEntityType(typeof<SmallGroup>).FindProperty("ChurchId")
            .SetValueConverter(Converters.ChurchIdConverter ())


/// This represents a time zone in which a class may reside
and [<CLIMutable; NoComparison; NoEquality>] TimeZone =
    {   /// The Id for this time zone (uses tzdata names)
        Id : TimeZoneId
        
        /// The description of this time zone
        Description : string
        
        /// The order in which this timezone should be displayed
        SortOrder : int
        
        /// Whether this timezone is active
        IsActive : bool
    }
with
    
    /// An empty time zone
    static member empty =
        {   Id          = TimeZoneId ""
            Description = ""
            SortOrder   = 0
            IsActive    = false
        }
    
    /// Configure EF for this entity
    static member internal configureEF (mb : ModelBuilder) =
        mb.Entity<TimeZone> ( fun m ->
            m.ToTable "TimeZone" |> ignore
            m.Property(fun e -> e.Id).HasColumnName "TimeZoneId" |> ignore
            m.Property(fun e -> e.Description).HasColumnName("Description").IsRequired() |> ignore
            m.Property(fun e -> e.SortOrder).HasColumnName "SortOrder" |> ignore
            m.Property(fun e -> e.IsActive).HasColumnName "IsActive" |> ignore)
        |> ignore
        mb.Model.FindEntityType(typeof<TimeZone>).FindProperty("Id")
            .SetValueConverter(Converters.TimeZoneIdConverter ())


/// This represents a user of PrayerTracker
and [<CLIMutable; NoComparison; NoEquality>] User =
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
        
        /// The salt for the user's hashed password
        Salt : Guid option
        
        /// The small groups which this user is authorized
        SmallGroups : ICollection<UserSmallGroup>
    }
with
    
    /// An empty user
    static member empty =
        {   Id           = UserId Guid.Empty
            FirstName    = ""
            LastName     = ""
            Email        = ""
            IsAdmin      = false
            PasswordHash = ""
            Salt         = None
            SmallGroups  = List<UserSmallGroup> ()
        }
    
    /// The full name of the user
    member this.fullName =
        $"{this.FirstName} {this.LastName}"

    /// Configure EF for this entity
    static member internal configureEF (mb : ModelBuilder) =
        mb.Entity<User> (fun m ->
            m.ToTable "User" |> ignore
            m.Ignore(fun e -> e.fullName :> obj) |> ignore
            m.Property(fun e -> e.Id).HasColumnName "UserId" |> ignore
            m.Property(fun e -> e.FirstName).HasColumnName("FirstName").IsRequired() |> ignore
            m.Property(fun e -> e.LastName).HasColumnName("LastName").IsRequired() |> ignore
            m.Property(fun e -> e.Email).HasColumnName("EmailAddress").IsRequired() |> ignore
            m.Property(fun e -> e.IsAdmin).HasColumnName "IsSystemAdmin" |> ignore
            m.Property(fun e -> e.PasswordHash).HasColumnName("PasswordHash").IsRequired() |> ignore
            m.Property(fun e -> e.Salt).HasColumnName "Salt" |> ignore)
        |> ignore
        mb.Model.FindEntityType(typeof<User>).FindProperty("Id").SetValueConverter(Converters.UserIdConverter ())
        mb.Model.FindEntityType(typeof<User>).FindProperty("Salt")
            .SetValueConverter(OptionConverter<Guid> ())


/// Cross-reference between user and small group
and [<CLIMutable; NoComparison; NoEquality>] UserSmallGroup =
    {   /// The Id of the user who has access to the small group
        UserId : UserId
        
        /// The Id of the small group to which the user has access
        SmallGroupId : SmallGroupId
        
        /// The user who has access to the small group
        User : User
        
        /// The small group to which the user has access
        SmallGroup : SmallGroup
    }
with
    
    /// An empty user/small group xref
    static member empty =
        {   UserId       = UserId Guid.Empty
            SmallGroupId = SmallGroupId Guid.Empty
            User         = User.empty
            SmallGroup   = SmallGroup.empty
        }
    
    /// Configure EF for this entity
    static member internal configureEF (mb : ModelBuilder) =
        mb.Entity<UserSmallGroup> (fun m ->
            m.ToTable "User_SmallGroup" |> ignore
            m.HasKey(fun e -> {| UserId = e.UserId; SmallGroupId = e.SmallGroupId |} :> obj) |> ignore
            m.Property(fun e -> e.UserId).HasColumnName "UserId" |> ignore
            m.Property(fun e -> e.SmallGroupId).HasColumnName "SmallGroupId" |> ignore
            m.HasOne(fun e -> e.User)
                .WithMany(fun e -> e.SmallGroups :> IEnumerable<UserSmallGroup>)
                .HasForeignKey(fun e -> e.UserId :> obj)
            |> ignore
            m.HasOne(fun e -> e.SmallGroup)
                .WithMany(fun e -> e.Users :> IEnumerable<UserSmallGroup>)
                .HasForeignKey(fun e -> e.SmallGroupId :> obj)
            |> ignore)
        |> ignore
        mb.Model.FindEntityType(typeof<UserSmallGroup>).FindProperty("UserId")
            .SetValueConverter(Converters.UserIdConverter ())
        mb.Model.FindEntityType(typeof<UserSmallGroup>).FindProperty("SmallGroupId")
            .SetValueConverter(Converters.SmallGroupIdConverter ())
        