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
        
        /// Does this church have an active interface with Virtual Prayer Space?
        HasVpsInterface : bool
        
        /// The address for the interface
        InterfaceAddress : string option
    }
with
    /// An empty church
    // aww... how sad :(
    static member empty =
        {   Id               = ChurchId Guid.Empty
            Name             = ""
            City             = ""
            State            = ""
            HasVpsInterface  = false
            InterfaceAddress = None
        }
    
    /// Configure EF for this entity
    static member internal ConfigureEF (mb : ModelBuilder) =
        mb.Entity<Church> (fun it ->
            seq<obj> {
                it.ToTable "church"
                it.Property(fun c -> c.Id).HasColumnName "id"
                it.Property(fun c -> c.Name).HasColumnName("church_name").IsRequired ()
                it.Property(fun c -> c.City).HasColumnName("city").IsRequired ()
                it.Property(fun c -> c.State).HasColumnName("state").IsRequired().HasMaxLength 2
                it.Property(fun c -> c.HasVpsInterface).HasColumnName "has_vps_interface"
                it.Property(fun c -> c.InterfaceAddress).HasColumnName "interface_address"
            } |> List.ofSeq |> ignore)
        |> ignore
        mb.Model.FindEntityType(typeof<Church>).FindProperty(nameof Church.empty.Id)
            .SetValueConverter (Converters.ChurchIdConverter ())
        mb.Model.FindEntityType(typeof<Church>).FindProperty(nameof Church.empty.InterfaceAddress)
            .SetValueConverter (OptionConverter<string> ())


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
    static member internal ConfigureEF (mb : ModelBuilder) =
        mb.Entity<ListPreferences> (fun it ->
            seq<obj> {
                it.ToTable "list_preference"
                it.HasKey (fun lp -> lp.SmallGroupId :> obj)
                it.Property(fun lp -> lp.SmallGroupId).HasColumnName "small_group_id"
                it.Property(fun lp -> lp.DaysToKeepNew).HasColumnName("days_to_keep_new").IsRequired().HasDefaultValue 7
                it.Property(fun lp -> lp.DaysToExpire).HasColumnName("days_to_expire").IsRequired().HasDefaultValue 14
                it.Property(fun lp -> lp.LongTermUpdateWeeks).HasColumnName("long_term_update_weeks").IsRequired()
                    .HasDefaultValue 4
                it.Property(fun lp -> lp.EmailFromName).HasColumnName("email_from_name").IsRequired()
                    .HasDefaultValue "PrayerTracker"
                it.Property(fun lp -> lp.EmailFromAddress).HasColumnName("email_from_address").IsRequired()
                    .HasDefaultValue "prayer@djs-consulting.com"
                it.Property(fun lp -> lp.Fonts).HasColumnName("fonts").IsRequired()
                    .HasDefaultValue "Century Gothic,Tahoma,Luxi Sans,sans-serif"
                it.Property(fun lp -> lp.HeadingColor).HasColumnName("heading_color").IsRequired()
                    .HasDefaultValue "maroon"
                it.Property(fun lp -> lp.LineColor).HasColumnName("line_color").IsRequired().HasDefaultValue "navy"
                it.Property(fun lp -> lp.HeadingFontSize).HasColumnName("heading_font_size").IsRequired()
                    .HasDefaultValue 16
                it.Property(fun lp -> lp.TextFontSize).HasColumnName("text_font_size").IsRequired().HasDefaultValue 12
                it.Property(fun lp -> lp.RequestSort).HasColumnName("request_sort").IsRequired().HasMaxLength(1)
                    .HasDefaultValue SortByDate
                it.Property(fun lp -> lp.GroupPassword).HasColumnName("group_password").IsRequired().HasDefaultValue ""
                it.Property(fun lp -> lp.DefaultEmailType).HasColumnName("default_email_type").IsRequired()
                    .HasDefaultValue HtmlFormat
                it.Property(fun lp -> lp.IsPublic).HasColumnName("is_public").IsRequired().HasDefaultValue false
                it.Property(fun lp -> lp.TimeZoneId).HasColumnName("time_zone_id").IsRequired()
                    .HasDefaultValue (TimeZoneId "America/Denver")
                it.Property(fun lp -> lp.PageSize).HasColumnName("page_size").IsRequired().HasDefaultValue 100
                it.Property(fun lp -> lp.AsOfDateDisplay).HasColumnName("as_of_date_display").IsRequired()
                    .HasMaxLength(1).HasDefaultValue NoDisplay
            } |> List.ofSeq |> ignore)
        |> ignore
        mb.Model.FindEntityType(typeof<ListPreferences>).FindProperty(nameof ListPreferences.empty.SmallGroupId)
            .SetValueConverter (Converters.SmallGroupIdConverter ())
        mb.Model.FindEntityType(typeof<ListPreferences>).FindProperty(nameof ListPreferences.empty.RequestSort)
            .SetValueConverter (Converters.RequestSortConverter ())
        mb.Model.FindEntityType(typeof<ListPreferences>).FindProperty(nameof ListPreferences.empty.DefaultEmailType)
            .SetValueConverter (Converters.EmailFormatConverter ())
        mb.Model.FindEntityType(typeof<ListPreferences>).FindProperty(nameof ListPreferences.empty.TimeZoneId)
            .SetValueConverter (Converters.TimeZoneIdConverter ())
        mb.Model.FindEntityType(typeof<ListPreferences>).FindProperty(nameof ListPreferences.empty.AsOfDateDisplay)
            .SetValueConverter (Converters.AsOfDateDisplayConverter ())


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
    static member internal ConfigureEF (mb : ModelBuilder) =
        mb.Entity<Member> (fun it ->
            seq<obj> {
                it.ToTable "member"
                it.Property(fun m -> m.Id).HasColumnName "id"
                it.Property(fun m -> m.SmallGroupId).HasColumnName("small_group_id").IsRequired ()
                it.Property(fun m -> m.Name).HasColumnName("member_name").IsRequired ()
                it.Property(fun m -> m.Email).HasColumnName("email").IsRequired ()
                it.Property(fun m -> m.Format).HasColumnName "email_format"
            } |> List.ofSeq |> ignore)
        |> ignore
        mb.Model.FindEntityType(typeof<Member>).FindProperty(nameof Member.empty.Id)
            .SetValueConverter (Converters.MemberIdConverter ())
        mb.Model.FindEntityType(typeof<Member>).FindProperty(nameof Member.empty.SmallGroupId)
            .SetValueConverter (Converters.SmallGroupIdConverter ())
        mb.Model.FindEntityType(typeof<Member>).FindProperty(nameof Member.empty.Format)
            .SetValueConverter (Converters.EmailFormatOptionConverter ())


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
        EnteredDate : Instant
        
        /// The date/time this request was last updated
        UpdatedDate : Instant
        
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
        {   Id             = PrayerRequestId Guid.Empty
            RequestType    = CurrentRequest
            UserId         = UserId Guid.Empty
            SmallGroupId   = SmallGroupId Guid.Empty
            EnteredDate    = Instant.MinValue
            UpdatedDate    = Instant.MinValue
            Requestor      = None
            Text           = "" 
            NotifyChaplain = false
            User           = User.empty
            SmallGroup     = SmallGroup.empty
            Expiration     = Automatic
        }
    
    /// Configure EF for this entity
    static member internal ConfigureEF (mb : ModelBuilder) =
        mb.Entity<PrayerRequest> (fun it ->
            seq<obj> {
                it.ToTable "prayer_request"
                it.Property(fun pr -> pr.Id).HasColumnName "id"
                it.Property(fun pr -> pr.RequestType).HasColumnName("request_type").IsRequired ()
                it.Property(fun pr -> pr.UserId).HasColumnName "user_id"
                it.Property(fun pr -> pr.SmallGroupId).HasColumnName "small_group_id"
                it.Property(fun pr -> pr.EnteredDate).HasColumnName "entered_date"
                it.Property(fun pr -> pr.UpdatedDate).HasColumnName "updated_date"
                it.Property(fun pr -> pr.Requestor).HasColumnName "requestor"
                it.Property(fun pr -> pr.Text).HasColumnName("request_text").IsRequired ()
                it.Property(fun pr -> pr.NotifyChaplain).HasColumnName "notify_chaplain"
                it.Property(fun pr -> pr.Expiration).HasColumnName "expiration"
            } |> List.ofSeq |> ignore)
        |> ignore
        mb.Model.FindEntityType(typeof<PrayerRequest>).FindProperty(nameof PrayerRequest.empty.Id)
            .SetValueConverter (Converters.PrayerRequestIdConverter ())
        mb.Model.FindEntityType(typeof<PrayerRequest>).FindProperty(nameof PrayerRequest.empty.RequestType)
            .SetValueConverter (Converters.PrayerRequestTypeConverter ())
        mb.Model.FindEntityType(typeof<PrayerRequest>).FindProperty(nameof PrayerRequest.empty.UserId)
            .SetValueConverter (Converters.UserIdConverter ())
        mb.Model.FindEntityType(typeof<PrayerRequest>).FindProperty(nameof PrayerRequest.empty.SmallGroupId)
            .SetValueConverter (Converters.SmallGroupIdConverter ())
        mb.Model.FindEntityType(typeof<PrayerRequest>).FindProperty(nameof PrayerRequest.empty.Requestor)
            .SetValueConverter (OptionConverter<string> ())
        mb.Model.FindEntityType(typeof<PrayerRequest>).FindProperty(nameof PrayerRequest.empty.Expiration)
            .SetValueConverter (Converters.ExpirationConverter ())


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
        Members : ResizeArray<Member>
        
        /// Prayer requests for this small group
        PrayerRequests : ResizeArray<PrayerRequest>
        
        /// The users authorized to manage this group
        Users : ResizeArray<UserSmallGroup>
    }
with
    
    /// An empty small group
    static member empty =
        {   Id             = SmallGroupId Guid.Empty
            ChurchId       = ChurchId Guid.Empty
            Name           = "" 
            Church         = Church.empty
            Preferences    = ListPreferences.empty
            Members        = ResizeArray<Member> ()
            PrayerRequests = ResizeArray<PrayerRequest> ()
            Users          = ResizeArray<UserSmallGroup> ()
        }

    /// Configure EF for this entity
    static member internal ConfigureEF (mb : ModelBuilder) =
        mb.Entity<SmallGroup> (fun it ->
            seq<obj> {
                it.ToTable "small_group"
                it.Property(fun sg -> sg.Id).HasColumnName "id"
                it.Property(fun sg -> sg.ChurchId).HasColumnName "church_id"
                it.Property(fun sg -> sg.Name).HasColumnName("group_name").IsRequired ()
                it.HasOne(fun sg -> sg.Preferences)
                    .WithOne()
                    .HasPrincipalKey(fun sg -> sg.Id :> obj)
                    .HasForeignKey(fun (lp : ListPreferences) -> lp.SmallGroupId :> obj)
            } |> List.ofSeq |> ignore)
        |> ignore
        mb.Model.FindEntityType(typeof<SmallGroup>).FindProperty(nameof SmallGroup.empty.Id)
            .SetValueConverter (Converters.SmallGroupIdConverter ())
        mb.Model.FindEntityType(typeof<SmallGroup>).FindProperty(nameof SmallGroup.empty.ChurchId)
            .SetValueConverter (Converters.ChurchIdConverter ())


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
    static member internal ConfigureEF (mb : ModelBuilder) =
        mb.Entity<TimeZone> (fun it ->
            seq<obj> {
                it.ToTable "time_zone"
                it.Property(fun tz -> tz.Id).HasColumnName "id"
                it.Property(fun tz -> tz.Description).HasColumnName("description").IsRequired ()
                it.Property(fun tz -> tz.SortOrder).HasColumnName "sort_order"
                it.Property(fun tz -> tz.IsActive).HasColumnName "is_active"
            } |> List.ofSeq |> ignore)
        |> ignore
        mb.Model.FindEntityType(typeof<TimeZone>).FindProperty(nameof TimeZone.empty.Id)
            .SetValueConverter (Converters.TimeZoneIdConverter ())


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
        
        /// The last time the user was seen (set whenever the user is loaded into a session)
        LastSeen : Instant option
        
        /// The small groups which this user is authorized
        SmallGroups : ResizeArray<UserSmallGroup>
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
            LastSeen     = None
            SmallGroups  = ResizeArray<UserSmallGroup> ()
        }
    
    /// The full name of the user
    member this.Name =
        $"{this.FirstName} {this.LastName}"

    /// Configure EF for this entity
    static member internal ConfigureEF (mb : ModelBuilder) =
        mb.Entity<User> (fun it ->
            seq<obj> {
                it.ToTable "pt_user"
                it.Ignore(fun u -> u.Name :> obj)
                it.Property(fun u -> u.Id).HasColumnName "id"
                it.Property(fun u -> u.FirstName).HasColumnName("first_name").IsRequired ()
                it.Property(fun u -> u.LastName).HasColumnName("last_name").IsRequired ()
                it.Property(fun u -> u.Email).HasColumnName("email").IsRequired ()
                it.Property(fun u -> u.IsAdmin).HasColumnName "is_admin"
                it.Property(fun u -> u.PasswordHash).HasColumnName("password_hash").IsRequired ()
                it.Property(fun u -> u.Salt).HasColumnName "salt"
                it.Property(fun u -> u.LastSeen).HasColumnName "last_seen"
            } |> List.ofSeq |> ignore)
        |> ignore
        mb.Model.FindEntityType(typeof<User>).FindProperty(nameof User.empty.Id)
            .SetValueConverter (Converters.UserIdConverter ())
        mb.Model.FindEntityType(typeof<User>).FindProperty(nameof User.empty.Salt)
            .SetValueConverter (OptionConverter<Guid> ())
        mb.Model.FindEntityType(typeof<User>).FindProperty(nameof User.empty.LastSeen)
            .SetValueConverter (OptionConverter<Instant> ())


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
    static member internal ConfigureEF (mb : ModelBuilder) =
        mb.Entity<UserSmallGroup> (fun it ->
            seq<obj> {
                it.ToTable "user_small_group"
                it.HasKey (nameof UserSmallGroup.empty.UserId, nameof UserSmallGroup.empty.SmallGroupId)
                it.Property(fun usg -> usg.UserId).HasColumnName "user_id"
                it.Property(fun usg -> usg.SmallGroupId).HasColumnName "small_group_id"
                it.HasOne(fun usg -> usg.User)
                    .WithMany(fun u -> u.SmallGroups :> seq<UserSmallGroup>)
                    .HasForeignKey(fun usg -> usg.UserId :> obj)
                it.HasOne(fun usg -> usg.SmallGroup)
                    .WithMany(fun sg -> sg.Users :> seq<UserSmallGroup>)
                    .HasForeignKey(fun usg -> usg.SmallGroupId :> obj)
            } |> List.ofSeq |> ignore)
        |> ignore
        mb.Model.FindEntityType(typeof<UserSmallGroup>).FindProperty(nameof UserSmallGroup.empty.UserId)
            .SetValueConverter (Converters.UserIdConverter ())
        mb.Model.FindEntityType(typeof<UserSmallGroup>).FindProperty(nameof UserSmallGroup.empty.SmallGroupId)
            .SetValueConverter (Converters.SmallGroupIdConverter ())


/// Support functions for small groups
module SmallGroup =
    
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
    


/// Support functions for prayer requests
module PrayerRequest =
    
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
