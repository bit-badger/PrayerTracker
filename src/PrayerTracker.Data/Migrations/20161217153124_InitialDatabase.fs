namespace PrayerTracker.Migrations

open Microsoft.EntityFrameworkCore
open Microsoft.EntityFrameworkCore.Infrastructure
open Microsoft.EntityFrameworkCore.Migrations
open Microsoft.EntityFrameworkCore.Migrations.Operations
open Microsoft.EntityFrameworkCore.Migrations.Operations.Builders
open Npgsql.EntityFrameworkCore.PostgreSQL.Metadata
open PrayerTracker
open PrayerTracker.Entities
open System

// fsharplint:disable RecordFieldNames

type ChurchTable =
  { churchId         : OperationBuilder<AddColumnOperation>
    city             : OperationBuilder<AddColumnOperation>
    hasInterface     : OperationBuilder<AddColumnOperation>
    interfaceAddress : OperationBuilder<AddColumnOperation>
    name             : OperationBuilder<AddColumnOperation>
    st               : OperationBuilder<AddColumnOperation>
    }

type ListPreferencesTable =
  { smallGroupId        : OperationBuilder<AddColumnOperation>
    daysToExpire        : OperationBuilder<AddColumnOperation>
    daysToKeepNew       : OperationBuilder<AddColumnOperation>
    defaultEmailType    : OperationBuilder<AddColumnOperation>
    emailFromAddress    : OperationBuilder<AddColumnOperation>
    emailFromName       : OperationBuilder<AddColumnOperation>
    groupPassword       : OperationBuilder<AddColumnOperation>
    headingColor        : OperationBuilder<AddColumnOperation>
    headingFontSize     : OperationBuilder<AddColumnOperation>
    isPublic            : OperationBuilder<AddColumnOperation>
    lineColor           : OperationBuilder<AddColumnOperation>
    listFonts           : OperationBuilder<AddColumnOperation>
    longTermUpdateWeeks : OperationBuilder<AddColumnOperation>
    requestSort         : OperationBuilder<AddColumnOperation>
    textFontSize        : OperationBuilder<AddColumnOperation>
    timeZoneId          : OperationBuilder<AddColumnOperation>
    pageSize            : OperationBuilder<AddColumnOperation>
    asOfDateDisplay     : OperationBuilder<AddColumnOperation>
    }

type MemberTable =
  { memberId     : OperationBuilder<AddColumnOperation>
    email        : OperationBuilder<AddColumnOperation>
    format       : OperationBuilder<AddColumnOperation>
    memberName   : OperationBuilder<AddColumnOperation>
    smallGroupId : OperationBuilder<AddColumnOperation>
    }

type PrayerRequestTable =
  { prayerRequestId : OperationBuilder<AddColumnOperation>
    enteredDate     : OperationBuilder<AddColumnOperation>
    expiration      : OperationBuilder<AddColumnOperation>
    notifyChaplain  : OperationBuilder<AddColumnOperation>
    requestType     : OperationBuilder<AddColumnOperation>
    requestor       : OperationBuilder<AddColumnOperation>
    smallGroupId    : OperationBuilder<AddColumnOperation>
    text            : OperationBuilder<AddColumnOperation>
    updatedDate     : OperationBuilder<AddColumnOperation>
    userId          : OperationBuilder<AddColumnOperation>
    }

type SmallGroupTable =
  { smallGroupId : OperationBuilder<AddColumnOperation>
    churchId     : OperationBuilder<AddColumnOperation>
    name         : OperationBuilder<AddColumnOperation>
    }

type TimeZoneTable =
  { timeZoneId  : OperationBuilder<AddColumnOperation>
    description : OperationBuilder<AddColumnOperation>
    isActive    : OperationBuilder<AddColumnOperation>
    sortOrder   : OperationBuilder<AddColumnOperation>
    }

type UserSmallGroupTable =
  { userId       : OperationBuilder<AddColumnOperation>
    smallGroupId : OperationBuilder<AddColumnOperation>
    }

type UserTable =
  { userId       : OperationBuilder<AddColumnOperation>
    emailAddress : OperationBuilder<AddColumnOperation>
    firstName    : OperationBuilder<AddColumnOperation>
    isAdmin      : OperationBuilder<AddColumnOperation>
    lastName     : OperationBuilder<AddColumnOperation>
    passwordHash : OperationBuilder<AddColumnOperation>
    salt         : OperationBuilder<AddColumnOperation>
    }

[<DbContext (typeof<AppDbContext>)>]
[<Migration "20161217153124_InitialDatabase">]
type InitialDatabase () =
  inherit Migration ()
  override __.Up (migrationBuilder : MigrationBuilder) =
    migrationBuilder.EnsureSchema (name = "pt")
    |> ignore

    migrationBuilder.CreateTable (
      name    = "Church",
      schema  = "pt",
      columns =
        (fun table ->
            { churchId         = table.Column<Guid>   (name = "ChurchId",         nullable = false)
              city             = table.Column<string> (name = "City",             nullable = false)
              hasInterface     = table.Column<bool>   (name = "HasVirtualPrayerRoomInterface", nullable = false)
              interfaceAddress = table.Column<string> (name = "InterfaceAddress", nullable = true)
              name             = table.Column<string> (name = "Name",             nullable = false)
              st               = table.Column<string> (name = "ST",               nullable = false, maxLength = Nullable<int> 2)
              }),
      constraints =
        fun table ->
          table.PrimaryKey ("PK_Church", fun x -> upcast x.churchId) |> ignore)
    |> ignore

    migrationBuilder.CreateTable (
      name    = "TimeZone",
      schema  = "pt",
      columns =
        (fun table ->
            { timeZoneId  = table.Column<string> (name = "TimeZoneId",  nullable = false)
              description = table.Column<string> (name = "Description", nullable = false)
              isActive    = table.Column<bool>   (name = "IsActive",    nullable = false)
              sortOrder   = table.Column<int>    (name = "SortOrder",   nullable = false)
              }),
      constraints =
        fun table ->
          table.PrimaryKey ("PK_TimeZone", fun x -> upcast x.timeZoneId) |> ignore)
    |> ignore

    migrationBuilder.CreateTable (
      name    = "User",
      schema  = "pt",
      columns =
        (fun table ->
            { userId       = table.Column<Guid>   (name = "UserId",        nullable = false)
              emailAddress = table.Column<string> (name = "EmailAddress",  nullable = false)
              firstName    = table.Column<string> (name = "FirstName",     nullable = false)
              isAdmin      = table.Column<bool>   (name = "IsSystemAdmin", nullable = false)
              lastName     = table.Column<string> (name = "LastName",      nullable = false)
              passwordHash = table.Column<string> (name = "PasswordHash",  nullable = false)
              salt         = table.Column<Guid>   (name = "Salt",          nullable = true)
              }),
      constraints =
        fun table ->
          table.PrimaryKey("PK_User", fun x -> upcast x.userId) |> ignore)
    |> ignore

    migrationBuilder.CreateTable (
      name    = "SmallGroup",
      schema  = "pt",
      columns =
        (fun table ->
            { smallGroupId = table.Column<Guid>   (name = "SmallGroupId", nullable = false)
              churchId     = table.Column<Guid>   (name = "ChurchId",     nullable = false)
              name         = table.Column<string> (name = "Name",         nullable = false)
              }),
      constraints =
        fun table ->
          table.PrimaryKey ("PK_SmallGroup", fun x -> upcast x.smallGroupId) |> ignore
          table.ForeignKey (
            name            = "FK_SmallGroup_Church_ChurchId",
            column          = (fun x -> upcast x.churchId),
            principalSchema = "pt",
            principalTable  = "Church",
            principalColumn = "ChurchId",
            onDelete        = ReferentialAction.Cascade)
          |> ignore)
    |> ignore

    migrationBuilder.CreateTable (
      name    = "ListPreference",
      schema  = "pt",
      columns =
        (fun table ->
            { smallGroupId        = table.Column<Guid>   (name = "SmallGroupId",        nullable = false)
              daysToExpire        = table.Column<int>    (name = "DaysToExpire",        nullable = false, defaultValue = 14)
              daysToKeepNew       = table.Column<int>    (name = "DaysToKeepNew",       nullable = false, defaultValue = 7)
              defaultEmailType    = table.Column<string> (name = "DefaultEmailType",    nullable = false, defaultValue = "Html")
              emailFromAddress    = table.Column<string> (name = "EmailFromAddress",    nullable = false, defaultValue = "prayer@djs-consulting.com")
              emailFromName       = table.Column<string> (name = "EmailFromName",       nullable = false, defaultValue = "PrayerTracker")
              groupPassword       = table.Column<string> (name = "GroupPassword",       nullable = false, defaultValue = "")
              headingColor        = table.Column<string> (name = "HeadingColor",        nullable = false, defaultValue = "maroon")
              headingFontSize     = table.Column<int>    (name = "HeadingFontSize",     nullable = false, defaultValue = 16)
              isPublic            = table.Column<bool>   (name = "IsPublic",            nullable = false, defaultValue = false)
              lineColor           = table.Column<string> (name = "LineColor",           nullable = false, defaultValue = "navy")
              listFonts           = table.Column<string> (name = "ListFonts",           nullable = false, defaultValue = "Century Gothic,Tahoma,Luxi Sans,sans-serif")
              longTermUpdateWeeks = table.Column<int>    (name = "LongTermUpdateWeeks", nullable = false, defaultValue = 4)
              requestSort         = table.Column<string> (name = "RequestSort",         nullable = false, defaultValue = "D", maxLength = Nullable<int> 1)
              textFontSize        = table.Column<int>    (name = "TextFontSize",        nullable = false, defaultValue = 12)
              timeZoneId          = table.Column<string> (name = "TimeZoneId",          nullable = false, defaultValue = "America/Denver")
              pageSize            = table.Column<int>    (name = "PageSize",            nullable = false, defaultValue = 100)
              asOfDateDisplay     = table.Column<string> (name = "AsOfDateDisplay",     nullable = false, defaultValue = "N", maxLength = Nullable<int> 1)
              }),
      constraints =
        fun table ->
          table.PrimaryKey ("PK_ListPreference", fun x -> upcast x.smallGroupId) |> ignore
          table.ForeignKey (
            name            = "FK_ListPreference_SmallGroup_SmallGroupId",
            column          = (fun x -> upcast x.smallGroupId),
            principalSchema = "pt",
            principalTable  = "SmallGroup",
            principalColumn = "SmallGroupId",
            onDelete        = ReferentialAction.Cascade)
          |> ignore
          table.ForeignKey (
            name            = "FK_ListPreference_TimeZone_TimeZoneId",
            column          = (fun x -> upcast x.timeZoneId),
            principalSchema = "pt",
            principalTable  = "TimeZone",
            principalColumn = "TimeZoneId",
            onDelete        = ReferentialAction.Cascade)
          |> ignore)
    |> ignore
    
    migrationBuilder.CreateTable (
      name    = "Member",
      schema  = "pt",
      columns =
        (fun table ->
            { memberId     = table.Column<Guid>   (name = "MemberId",     nullable = false)
              email        = table.Column<string> (name = "Email",        nullable = false)
              format       = table.Column<string> (name = "Format",       nullable = true)
              memberName   = table.Column<string> (name = "MemberName",   nullable = false)
              smallGroupId = table.Column<Guid>   (name = "SmallGroupId", nullable = false)
              }),
      constraints =
        fun table ->
          table.PrimaryKey ("PK_Member", fun x -> upcast x.memberId) |> ignore
          table.ForeignKey (
            name            = "FK_Member_SmallGroup_SmallGroupId",
            column          = (fun x -> upcast x.smallGroupId),
            principalSchema = "pt",
            principalTable  = "SmallGroup",
            principalColumn = "SmallGroupId",
            onDelete        = ReferentialAction.Cascade)
          |> ignore)
    |> ignore

    migrationBuilder.CreateTable (
      name    = "PrayerRequest",
      schema  = "pt",
      columns =
        (fun table ->
            { prayerRequestId   = table.Column<Guid>     (name = "PrayerRequestId", nullable = false)
              expiration        = table.Column<bool>     (name = "Expiration",      nullable = false)
              enteredDate       = table.Column<DateTime> (name = "EnteredDate",     nullable = false)
              notifyChaplain    = table.Column<bool>     (name = "NotifyChaplain",  nullable = false)
              requestType       = table.Column<string>   (name = "RequestType",     nullable = false)
              requestor         = table.Column<string>   (name = "Requestor",       nullable = true)
              smallGroupId      = table.Column<Guid>     (name = "SmallGroupId",    nullable = false)
              text              = table.Column<string>   (name = "Text",            nullable = false)
              updatedDate       = table.Column<DateTime> (name = "UpdatedDate",     nullable = false)
              userId            = table.Column<Guid>     (name = "UserId",          nullable = false)
              }),
      constraints =
        fun table ->
          table.PrimaryKey ("PK_PrayerRequest", fun x -> upcast x.prayerRequestId) |> ignore
          table.ForeignKey (
            name            = "FK_PrayerRequest_SmallGroup_SmallGroupId",
            column          = (fun x -> upcast x.smallGroupId),
            principalSchema = "pt",
            principalTable  = "SmallGroup",
            principalColumn = "SmallGroupId",
            onDelete        = ReferentialAction.Cascade)
          |> ignore
          table.ForeignKey (
            name            = "FK_PrayerRequest_User_UserId",
            column          = (fun x -> upcast x.userId),
            principalSchema = "pt",
            principalTable  = "User",
            principalColumn = "UserId",
            onDelete        = ReferentialAction.Cascade)
          |> ignore)
    |> ignore
    
    migrationBuilder.CreateTable(
      name    = "User_SmallGroup",
      schema  = "pt",
      columns =
        (fun table ->
            { userId       = table.Column<Guid> (name = "UserId",       nullable = false)
              smallGroupId = table.Column<Guid> (name = "SmallGroupId", nullable = false)
              }),
      constraints =
        fun table ->
          table.PrimaryKey ("PK_User_SmallGroup", fun x -> upcast x) |> ignore
          table.ForeignKey (
            name            = "FK_User_SmallGroup_SmallGroup_SmallGroupId",
            column          = (fun x -> upcast x.smallGroupId),
            principalSchema = "pt",
            principalTable  = "SmallGroup",
            principalColumn = "SmallGroupId",
            onDelete        = ReferentialAction.Cascade)
          |> ignore
          table.ForeignKey (
            name            = "FK_User_SmallGroup_User_UserId",
            column          = (fun x -> upcast x.userId),
            principalSchema = "pt",
            principalTable  = "User",
            principalColumn = "UserId",
            onDelete        = ReferentialAction.Cascade)
          |> ignore)
    |> ignore

    migrationBuilder.CreateIndex (name = "IX_ListPreference_TimeZoneId",    schema = "pt", table = "ListPreference",  column = "TimeZoneId")   |> ignore
    migrationBuilder.CreateIndex (name = "IX_Member_SmallGroupId",          schema = "pt", table = "Member",          column = "SmallGroupId") |> ignore
    migrationBuilder.CreateIndex (name = "IX_PrayerRequest_SmallGroupId",   schema = "pt", table = "PrayerRequest",   column = "SmallGroupId") |> ignore
    migrationBuilder.CreateIndex (name = "IX_PrayerRequest_UserId",         schema = "pt", table = "PrayerRequest",   column = "UserId")       |> ignore
    migrationBuilder.CreateIndex (name = "IX_SmallGroup_ChurchId",          schema = "pt", table = "SmallGroup",      column = "ChurchId")     |> ignore
    migrationBuilder.CreateIndex (name = "IX_User_SmallGroup_SmallGroupId", schema = "pt", table = "User_SmallGroup", column = "SmallGroupId") |> ignore
  
  override __.Down (migrationBuilder : MigrationBuilder) =
    migrationBuilder.DropTable (name = "ListPreference",  schema = "pt") |> ignore
    migrationBuilder.DropTable (name = "Member",          schema = "pt") |> ignore
    migrationBuilder.DropTable (name = "PrayerRequest",   schema = "pt") |> ignore
    migrationBuilder.DropTable (name = "User_SmallGroup", schema = "pt") |> ignore
    migrationBuilder.DropTable (name = "TimeZone",        schema = "pt") |> ignore
    migrationBuilder.DropTable (name = "SmallGroup",      schema = "pt") |> ignore
    migrationBuilder.DropTable (name = "User",            schema = "pt") |> ignore
    migrationBuilder.DropTable (name = "Church",          schema = "pt") |> ignore


  override __.BuildTargetModel (modelBuilder : ModelBuilder) =
    modelBuilder
      .HasDefaultSchema("pt")
      .HasAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn)
      .HasAnnotation("ProductVersion", "1.1.0-rtm-22752")
    |> ignore

    modelBuilder.Entity (
      typeof<Church>,
      fun b ->
          b.Property<Guid>("churchId").ValueGeneratedOnAdd() |> ignore
          b.Property<string>("city").IsRequired() |> ignore
          b.Property<bool>("hasInterface") |> ignore
          b.Property<string>("interfaceAddress") |> ignore
          b.Property<string>("name").IsRequired() |> ignore
          b.Property<string>("st").IsRequired().HasMaxLength(2) |> ignore
          b.HasKey("churchId") |> ignore
          b.ToTable("Church") |> ignore)
    |> ignore

    modelBuilder.Entity (
      typeof<ListPreferences>,
      fun b ->
          b.Property<Guid>("smallGroupId") |> ignore
          b.Property<int>("daysToExpire").ValueGeneratedOnAdd().HasDefaultValue(14) |> ignore
          b.Property<int>("daysToKeepNew").ValueGeneratedOnAdd().HasDefaultValue(7) |> ignore
          b.Property<string>("defaultEmailType").IsRequired().ValueGeneratedOnAdd().HasDefaultValue("H") |> ignore
          b.Property<string>("emailFromAddress").IsRequired().ValueGeneratedOnAdd().HasDefaultValue("prayer@djs-consulting.com") |> ignore
          b.Property<string>("emailFromName").IsRequired().ValueGeneratedOnAdd().HasDefaultValue("PrayerTracker") |> ignore
          b.Property<string>("groupPassword").IsRequired().ValueGeneratedOnAdd().HasDefaultValue("") |> ignore
          b.Property<string>("headingColor").IsRequired().ValueGeneratedOnAdd().HasDefaultValue("maroon") |> ignore
          b.Property<int>("headingFontSize").ValueGeneratedOnAdd().HasDefaultValue(16) |> ignore
          b.Property<bool>("isPublic").ValueGeneratedOnAdd().HasDefaultValue(false) |> ignore
          b.Property<string>("lineColor").IsRequired().ValueGeneratedOnAdd().HasDefaultValue("navy") |> ignore
          b.Property<string>("listFonts").IsRequired().ValueGeneratedOnAdd().HasDefaultValue("Century Gothic,Tahoma,Luxi Sans,sans-serif") |> ignore
          b.Property<int>("longTermUpdateWeeks").ValueGeneratedOnAdd().HasDefaultValue(4) |> ignore
          b.Property<string>("requestSort").IsRequired().ValueGeneratedOnAdd().HasDefaultValue("D").HasMaxLength(1) |> ignore
          b.Property<int>("textFontSize").ValueGeneratedOnAdd().HasDefaultValue(12) |> ignore
          b.Property<string>("timeZoneId").IsRequired().ValueGeneratedOnAdd().HasDefaultValue("America/Denver") |> ignore
          b.Property<int>("pageSize").IsRequired().ValueGeneratedOnAdd().HasDefaultValue(100) |> ignore
          b.Property<string>("asOfDateDisplay").IsRequired().ValueGeneratedOnAdd().HasDefaultValue("N").HasMaxLength(1) |> ignore
          b.HasKey("smallGroupId") |> ignore
          b.HasIndex("timeZoneId") |> ignore
          b.ToTable("ListPreference") |> ignore)
    |> ignore

    modelBuilder.Entity (
      typeof<Member>,
      fun b ->
          b.Property<Guid>("memberId").ValueGeneratedOnAdd() |> ignore
          b.Property<string>("email").IsRequired() |> ignore
          b.Property<string>("format") |> ignore
          b.Property<string>("memberName").IsRequired() |> ignore
          b.Property<Guid>("smallGroupId") |> ignore
          b.HasKey("memberId") |> ignore
          b.HasIndex("smallGroupId") |> ignore
          b.ToTable("Member") |> ignore)
    |> ignore

    modelBuilder.Entity (
      typeof<PrayerRequest>,
      fun b ->
          b.Property<Guid>("prayerRequestId").ValueGeneratedOnAdd() |> ignore
          b.Property<DateTime>("enteredDate").IsRequired() |> ignore
          b.Property<string>("expiration").IsRequired().HasMaxLength 1 |> ignore
          b.Property<bool>("notifyChaplain") |> ignore
          b.Property<string>("requestType").IsRequired().HasMaxLength 1 |> ignore
          b.Property<string>("requestor") |> ignore
          b.Property<Guid>("smallGroupId") |> ignore
          b.Property<string>("text").IsRequired() |> ignore
          b.Property<DateTime>("updatedDate") |> ignore
          b.Property<Guid>("userId") |> ignore
          b.HasKey("prayerRequestId") |> ignore
          b.HasIndex("smallGroupId") |> ignore
          b.HasIndex("userId") |> ignore
          b.ToTable("PrayerRequest") |> ignore)
    |> ignore

    modelBuilder.Entity (
      typeof<SmallGroup>,
      fun b ->
          b.Property<Guid>("smallGroupId").ValueGeneratedOnAdd() |> ignore
          b.Property<Guid>("churchId") |> ignore
          b.Property<string>("name").IsRequired() |> ignore
          b.HasKey("smallGroupId") |> ignore
          b.HasIndex("churchId") |> ignore
          b.ToTable("SmallGroup") |> ignore)
    |> ignore

    modelBuilder.Entity (
      typeof<PrayerTracker.Entities.TimeZone>,
      fun b ->
          b.Property<string>("timeZoneId").ValueGeneratedOnAdd() |> ignore
          b.Property<string>("description").IsRequired() |> ignore
          b.Property<bool>("isActive") |> ignore
          b.Property<int>("sortOrder") |> ignore
          b.HasKey("timeZoneId") |> ignore
          b.ToTable("TimeZone") |> ignore)
    |> ignore

    modelBuilder.Entity (
      typeof<User>,
      fun b ->
          b.Property<Guid>("userId").ValueGeneratedOnAdd() |> ignore
          b.Property<string>("emailAddress").IsRequired() |> ignore
          b.Property<string>("firstName").IsRequired() |> ignore
          b.Property<bool>("isAdmin") |> ignore
          b.Property<string>("lastName").IsRequired() |> ignore
          b.Property<string>("passwordHash").IsRequired() |> ignore
          b.Property<Guid>("salt") |> ignore
          b.HasKey("userId") |> ignore
          b.ToTable("User") |> ignore)
    |> ignore

    modelBuilder.Entity (
      typeof<UserSmallGroup>,
      fun b ->
          b.Property<Guid>("userId") |> ignore
          b.Property<Guid>("smallGroupId") |> ignore
          b.HasKey("userId", "smallGroupId") |> ignore
          b.HasIndex("smallGroupId") |> ignore
          b.ToTable("User_SmallGroup") |> ignore)
    |> ignore

    modelBuilder.Entity (
      typeof<ListPreferences>,
      fun b ->
          b.HasOne("PrayerTracker.Entities.SmallGroup")
            .WithOne("preferences")
            .HasForeignKey("PrayerTracker.Entities.ListPreferences", "smallGroupId")
            .OnDelete(DeleteBehavior.Cascade)
          |> ignore
          b.HasOne("PrayerTracker.Entities.TimeZone", "timeZone")
            .WithMany()
            .HasForeignKey("timeZoneId")
            .OnDelete(DeleteBehavior.Cascade)
          |> ignore)
    |> ignore
    
    modelBuilder.Entity (
      typeof<Member>,
      fun b ->
          b.HasOne("PrayerTracker.Entities.SmallGroup", "smallGroup")
            .WithMany("members")
            .HasForeignKey("smallGroupId")
            .OnDelete(DeleteBehavior.Cascade)
          |> ignore)
    |> ignore

    modelBuilder.Entity (
      typeof<PrayerRequest>,
      fun b ->
          b.HasOne("PrayerTracker.Entities.SmallGroup", "smallGroup")
            .WithMany("prayerRequests")
            .HasForeignKey("smallGroupId")
            .OnDelete(DeleteBehavior.Cascade)
          |> ignore
          b.HasOne("PrayerTracker.Entities.User", "user")
            .WithMany()
            .HasForeignKey("userId")
            .OnDelete(DeleteBehavior.Cascade)
          |> ignore)
    |> ignore

    modelBuilder.Entity (
      typeof<SmallGroup>,
      fun b ->
          b.HasOne("PrayerTracker.Entities.Church", "Church")
            .WithMany("SmallGroups")
            .HasForeignKey("ChurchId")
            .OnDelete(DeleteBehavior.Cascade)
          |> ignore)
    |> ignore

    modelBuilder.Entity (
      typeof<UserSmallGroup>,
      fun b ->
          b.HasOne("PrayerTracker.Entities.SmallGroup", "smallGroup")
            .WithMany("users")
            .HasForeignKey("smallGroupId")
            .OnDelete(DeleteBehavior.Cascade)
          |> ignore
          b.HasOne("PrayerTracker.Entities.User", "user")
            .WithMany("smallGroups")
            .HasForeignKey("userId")
            .OnDelete(DeleteBehavior.Cascade)
          |> ignore)
    |> ignore
