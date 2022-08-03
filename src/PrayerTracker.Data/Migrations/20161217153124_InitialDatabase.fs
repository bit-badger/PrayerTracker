namespace PrayerTracker.Migrations

open System
open Microsoft.EntityFrameworkCore
open Microsoft.EntityFrameworkCore.Infrastructure
open Microsoft.EntityFrameworkCore.Migrations
open Npgsql.EntityFrameworkCore.PostgreSQL.Metadata
open PrayerTracker
open PrayerTracker.Entities

[<DbContext (typeof<AppDbContext>)>]
[<Migration "20161217153124_InitialDatabase">]
type InitialDatabase () =
    inherit Migration ()
    override _.Up (migrationBuilder : MigrationBuilder) =
        migrationBuilder.EnsureSchema (name = "pt")
        |> ignore

        migrationBuilder.CreateTable (
            name    = "church",
            schema  = "pt",
            columns = (fun table ->
                {|  Id               = table.Column<Guid>   (name = "id",                nullable = false)
                    City             = table.Column<string> (name = "city",              nullable = false)
                    HasVpsInterface  = table.Column<bool>   (name = "has_vps_interface", nullable = false)
                    InterfaceAddress = table.Column<string> (name = "interface_address", nullable = true)
                    Name             = table.Column<string> (name = "church_Name",       nullable = false)
                    State            = table.Column<string> (name = "state",             nullable = false, maxLength = Nullable<int> 2)
                |}),
            constraints = fun table ->
                table.PrimaryKey ("pk_church", fun x -> upcast x.Id) |> ignore)
        |> ignore

        migrationBuilder.CreateTable (
            name    = "time_zone",
            schema  = "pt",
            columns = (fun table ->
                {|  Id          = table.Column<string> (name = "id",          nullable = false)
                    Description = table.Column<string> (name = "description", nullable = false)
                    IsActive    = table.Column<bool>   (name = "is_active",   nullable = false)
                    SortOrder   = table.Column<int>    (name = "sort_order",  nullable = false)
                |}),
            constraints = fun table ->
                table.PrimaryKey ("pk_time_zone", fun x -> upcast x.Id) |> ignore)
        |> ignore

        migrationBuilder.CreateTable (
            name    = "pt_user",
            schema  = "pt",
            columns = (fun table ->
                {|  Id           = table.Column<Guid>   (name = "id",            nullable = false)
                    Email        = table.Column<string> (name = "email",         nullable = false)
                    FirstName    = table.Column<string> (name = "first_name",    nullable = false)
                    IsAdmin      = table.Column<bool>   (name = "is_admin",      nullable = false)
                    LastName     = table.Column<string> (name = "last_name",     nullable = false)
                    PasswordHash = table.Column<string> (name = "password_hash", nullable = false)
                    Salt         = table.Column<Guid>   (name = "salt",          nullable = true)
                |}),
            constraints = fun table ->
                table.PrimaryKey("pk_pt_user", fun x -> upcast x.Id) |> ignore)
        |> ignore

        migrationBuilder.CreateTable (
            name    = "small_group",
            schema  = "pt",
            columns = (fun table ->
                {|  Id       = table.Column<Guid>   (name = "id",         nullable = false)
                    ChurchId = table.Column<Guid>   (name = "church_id",  nullable = false)
                    Name     = table.Column<string> (name = "group_name", nullable = false)
                |}),
            constraints = fun table ->
                table.PrimaryKey ("pk_small_group", fun x -> upcast x.Id) |> ignore
                table.ForeignKey (
                    name            = "fk_small_group_church_id",
                    column          = (fun x -> upcast x.ChurchId),
                    principalSchema = "pt",
                    principalTable  = "church",
                    principalColumn = "id",
                    onDelete        = ReferentialAction.Cascade)
                |> ignore)
        |> ignore

        migrationBuilder.CreateTable (
            name    = "list_preference",
            schema  = "pt",
            columns = (fun table ->
                {|  SmallGroupId        = table.Column<Guid>   (name = "small_group_id",         nullable = false)
                    AsOfDateDisplay     = table.Column<string> (name = "as_of_date_display",     nullable = false, defaultValue = "N", maxLength = Nullable<int> 1)
                    DaysToExpire        = table.Column<int>    (name = "days_to_expire",         nullable = false, defaultValue = 14)
                    DaysToKeepNew       = table.Column<int>    (name = "days_to_keep_new",       nullable = false, defaultValue = 7)
                    DefaultEmailType    = table.Column<string> (name = "default_email_type",     nullable = false, defaultValue = "Html")
                    EmailFromAddress    = table.Column<string> (name = "email_from_address",     nullable = false, defaultValue = "prayer@djs-consulting.com")
                    EmailFromName       = table.Column<string> (name = "email_from_name",        nullable = false, defaultValue = "PrayerTracker")
                    Fonts               = table.Column<string> (name = "fonts",                  nullable = false, defaultValue = "Century Gothic,Tahoma,Luxi Sans,sans-serif")
                    GroupPassword       = table.Column<string> (name = "group_password",         nullable = false, defaultValue = "")
                    HeadingColor        = table.Column<string> (name = "heading_color",          nullable = false, defaultValue = "maroon")
                    HeadingFontSize     = table.Column<int>    (name = "heading_font_size",      nullable = false, defaultValue = 16)
                    IsPublic            = table.Column<bool>   (name = "is_public",              nullable = false, defaultValue = false)
                    LineColor           = table.Column<string> (name = "line_color",             nullable = false, defaultValue = "navy")
                    LongTermUpdateWeeks = table.Column<int>    (name = "long_term_update_weeks", nullable = false, defaultValue = 4)
                    PageSize            = table.Column<int>    (name = "page_size",              nullable = false, defaultValue = 100)
                    RequestSort         = table.Column<string> (name = "request_sort",           nullable = false, defaultValue = "D", maxLength = Nullable<int> 1)
                    TextFontSize        = table.Column<int>    (name = "text_font_size",         nullable = false, defaultValue = 12)
                    TimeZoneId          = table.Column<string> (name = "time_zone_id",           nullable = false, defaultValue = "America/Denver")
                |}),
            constraints = fun table ->
                table.PrimaryKey ("pk_list_preference", fun x -> upcast x.SmallGroupId) |> ignore
                table.ForeignKey (
                    name            = "fk_list_preference_small_group_id",
                    column          = (fun x -> upcast x.SmallGroupId),
                    principalSchema = "pt",
                    principalTable  = "small_group",
                    principalColumn = "id",
                    onDelete        = ReferentialAction.Cascade)
                |> ignore
                table.ForeignKey (
                    name            = "fk_list_preference_time_zone_id",
                    column          = (fun x -> upcast x.TimeZoneId),
                    principalSchema = "pt",
                    principalTable  = "time_zone",
                    principalColumn = "id",
                    onDelete        = ReferentialAction.Cascade)
                |> ignore)
        |> ignore
        
        migrationBuilder.CreateTable (
            name    = "member",
            schema  = "pt",
            columns = (fun table ->
                {|  Id           = table.Column<Guid>   (name = "id",             nullable = false)
                    Email        = table.Column<string> (name = "email",          nullable = false)
                    Format       = table.Column<string> (name = "email_format",   nullable = true)
                    Name         = table.Column<string> (name = "member_name",    nullable = false)
                    SmallGroupId = table.Column<Guid>   (name = "small_group_id", nullable = false)
                |}),
            constraints = fun table ->
                table.PrimaryKey ("pk_member", fun x -> upcast x.Id) |> ignore
                table.ForeignKey (
                    name            = "fk_member_small_group_id",
                    column          = (fun x -> upcast x.SmallGroupId),
                    principalSchema = "pt",
                    principalTable  = "small_group",
                    principalColumn = "id",
                    onDelete        = ReferentialAction.Cascade)
                |> ignore)
        |> ignore

        migrationBuilder.CreateTable (
            name    = "prayer_request",
            schema  = "pt",
            columns = (fun table ->
                {|  Id             = table.Column<Guid>     (name = "id",              nullable = false)
                    Expiration     = table.Column<bool>     (name = "expiration",      nullable = false)
                    EnteredDate    = table.Column<DateTime> (name = "entered_date",    nullable = false)
                    NotifyChaplain = table.Column<bool>     (name = "notify_chaplain", nullable = false)
                    RequestType    = table.Column<string>   (name = "request_type",    nullable = false)
                    Requestor      = table.Column<string>   (name = "requestor",       nullable = true)
                    SmallGroupId   = table.Column<Guid>     (name = "small_group_id",  nullable = false)
                    Text           = table.Column<string>   (name = "request_text",    nullable = false)
                    UpdatedDate    = table.Column<DateTime> (name = "updated_date",    nullable = false)
                    UserId         = table.Column<Guid>     (name = "user_id",         nullable = false)
                |}),
            constraints = fun table ->
                table.PrimaryKey ("pk_prayer_request", fun x -> upcast x.Id) |> ignore
                table.ForeignKey (
                    name            = "fk_prayer_request_small_group_id",
                    column          = (fun x -> upcast x.SmallGroupId),
                    principalSchema = "pt",
                    principalTable  = "small_group",
                    principalColumn = "i",
                    onDelete        = ReferentialAction.Cascade)
                |> ignore
                table.ForeignKey (
                    name            = "fk_prayer_request_user_id",
                    column          = (fun x -> upcast x.UserId),
                    principalSchema = "pt",
                    principalTable  = "pt_user",
                    principalColumn = "id",
                    onDelete        = ReferentialAction.Cascade)
                |> ignore)
        |> ignore
        
        migrationBuilder.CreateTable(
            name    = "user_small_group",
            schema  = "pt",
            columns = (fun table ->
                {|  UserId       = table.Column<Guid> (name = "user_id",        nullable = false)
                    SmallGroupId = table.Column<Guid> (name = "small_group_id", nullable = false)
                |}),
            constraints = fun table ->
                table.PrimaryKey ("pk_user_small_group", fun x -> upcast x) |> ignore
                table.ForeignKey (
                    name            = "fk_user_small_group_small_group_id",
                    column          = (fun x -> upcast x.SmallGroupId),
                    principalSchema = "pt",
                    principalTable  = "small_group",
                    principalColumn = "id",
                    onDelete        = ReferentialAction.Cascade)
                |> ignore
                table.ForeignKey (
                    name            = "fk_user_small_group_user_id",
                    column          = (fun x -> upcast x.UserId),
                    principalSchema = "pt",
                    principalTable  = "pt_user",
                    principalColumn = "id",
                    onDelete        = ReferentialAction.Cascade)
                |> ignore)
        |> ignore

        migrationBuilder.CreateIndex (name = "ix_list_preference_time_zone_id",    schema = "pt", table = "list_preference",  column = "time_zone_id")   |> ignore
        migrationBuilder.CreateIndex (name = "ix_member_small_group_id",           schema = "pt", table = "member",           column = "small_group_id") |> ignore
        migrationBuilder.CreateIndex (name = "ix_prayer_request_small_group_id",   schema = "pt", table = "prayer_request",   column = "small_group_id") |> ignore
        migrationBuilder.CreateIndex (name = "ix_prayer_request_user_id",          schema = "pt", table = "prayer_request",   column = "user_id")        |> ignore
        migrationBuilder.CreateIndex (name = "ix_small_group_church_id",           schema = "pt", table = "small_group",      column = "church_id")      |> ignore
        migrationBuilder.CreateIndex (name = "ix_user_small_group_small_group_id", schema = "pt", table = "user_small_group", column = "small_group_id") |> ignore
  
    override _.Down (migrationBuilder : MigrationBuilder) =
        migrationBuilder.DropTable (name = "list_preference",  schema = "pt") |> ignore
        migrationBuilder.DropTable (name = "member",           schema = "pt") |> ignore
        migrationBuilder.DropTable (name = "prayer_request",   schema = "pt") |> ignore
        migrationBuilder.DropTable (name = "user_small_group", schema = "pt") |> ignore
        migrationBuilder.DropTable (name = "time_zone",        schema = "pt") |> ignore
        migrationBuilder.DropTable (name = "small_group",      schema = "pt") |> ignore
        migrationBuilder.DropTable (name = "pt_user",          schema = "pt") |> ignore
        migrationBuilder.DropTable (name = "church",           schema = "pt") |> ignore


    override _.BuildTargetModel (modelBuilder : ModelBuilder) =
        modelBuilder
            .HasDefaultSchema("pt")
            .HasAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn)
            .HasAnnotation("ProductVersion", "1.1.0-rtm-22752")
        |> ignore

        modelBuilder.Entity (typeof<Church>, fun b ->
            b.Property<Guid>("Id").HasColumnName("id").ValueGeneratedOnAdd() |> ignore
            b.Property<string>("City").HasColumnName("city").IsRequired() |> ignore
            b.Property<bool>("HasVpsInterface").HasColumnName("has_vps_interface") |> ignore
            b.Property<string>("InterfaceAddress").HasColumnName("interface_address") |> ignore
            b.Property<string>("Name").HasColumnName("church_name").IsRequired() |> ignore
            b.Property<string>("State").HasColumnName("state").IsRequired().HasMaxLength(2) |> ignore
            b.HasKey("Id") |> ignore
            b.ToTable("church") |> ignore)
        |> ignore

        modelBuilder.Entity (typeof<ListPreferences>, fun b ->
            b.Property<Guid>("SmallGroupId").HasColumnName("small_group_id") |> ignore
            b.Property<string>("AsOfDateDisplay").HasColumnName("as_of_date_display").IsRequired().ValueGeneratedOnAdd().HasDefaultValue("N").HasMaxLength(1) |> ignore
            b.Property<int>("DaysToExpire").HasColumnName("days_to_expire").ValueGeneratedOnAdd().HasDefaultValue(14) |> ignore
            b.Property<int>("DaysToKeepNew").HasColumnName("days_to_keep_new").ValueGeneratedOnAdd().HasDefaultValue(7) |> ignore
            b.Property<string>("DefaultEmailType").HasColumnName("default_email_type").IsRequired().ValueGeneratedOnAdd().HasDefaultValue("H") |> ignore
            b.Property<string>("EmailFromAddress").HasColumnName("email_from_address").IsRequired().ValueGeneratedOnAdd().HasDefaultValue("prayer@djs-consulting.com") |> ignore
            b.Property<string>("EmailFromName").HasColumnName("email_from_name").IsRequired().ValueGeneratedOnAdd().HasDefaultValue("PrayerTracker") |> ignore
            b.Property<string>("Fonts").HasColumnName("fonts").IsRequired().ValueGeneratedOnAdd().HasDefaultValue("Century Gothic,Tahoma,Luxi Sans,sans-serif") |> ignore
            b.Property<string>("GroupPassword").HasColumnName("group_password").IsRequired().ValueGeneratedOnAdd().HasDefaultValue("") |> ignore
            b.Property<string>("HeadingColor").HasColumnName("heading_color").IsRequired().ValueGeneratedOnAdd().HasDefaultValue("maroon") |> ignore
            b.Property<int>("HeadingFontSize").HasColumnName("heading_font_size").ValueGeneratedOnAdd().HasDefaultValue(16) |> ignore
            b.Property<bool>("IsPublic").HasColumnName("is_public").ValueGeneratedOnAdd().HasDefaultValue(false) |> ignore
            b.Property<string>("LineColor").HasColumnName("line_color").IsRequired().ValueGeneratedOnAdd().HasDefaultValue("navy") |> ignore
            b.Property<int>("LongTermUpdateWeeks").HasColumnName("long_term_update_weeks").ValueGeneratedOnAdd().HasDefaultValue(4) |> ignore
            b.Property<int>("PageSize").HasColumnName("page_size").IsRequired().ValueGeneratedOnAdd().HasDefaultValue(100) |> ignore
            b.Property<string>("RequestSort").HasColumnName("request_sort").IsRequired().ValueGeneratedOnAdd().HasDefaultValue("D").HasMaxLength(1) |> ignore
            b.Property<int>("TextFontSize").HasColumnName("text_font_size").ValueGeneratedOnAdd().HasDefaultValue(12) |> ignore
            b.Property<string>("TimeZoneId").HasColumnName("time_zone_id").IsRequired().ValueGeneratedOnAdd().HasDefaultValue("America/Denver") |> ignore
            b.HasKey("SmallGroupId") |> ignore
            b.HasIndex("TimeZoneId").HasDatabaseName "ix_list_preference_time_zone_id" |> ignore
            b.ToTable("list_preference") |> ignore)
        |> ignore

        modelBuilder.Entity (typeof<Member>, fun b ->
            b.Property<Guid>("Id").HasColumnName("id").ValueGeneratedOnAdd() |> ignore
            b.Property<string>("Email").HasColumnName("email").IsRequired() |> ignore
            b.Property<string>("Format").HasColumnName("email_format") |> ignore
            b.Property<string>("Name").HasColumnName("member_name").IsRequired() |> ignore
            b.Property<Guid>("SmallGroupId").HasColumnName("small_group_id") |> ignore
            b.HasKey("Id") |> ignore
            b.HasIndex("SmallGroupId").HasDatabaseName "ix_member_small_group_id" |> ignore
            b.ToTable("member") |> ignore)
        |> ignore

        modelBuilder.Entity (typeof<PrayerRequest>, fun b ->
            b.Property<Guid>("Id").HasColumnName("id").ValueGeneratedOnAdd() |> ignore
            b.Property<DateTime>("EnteredDate").HasColumnName("entered_date").IsRequired() |> ignore
            b.Property<string>("Expiration").HasColumnName("expiration").IsRequired().HasMaxLength 1 |> ignore
            b.Property<bool>("NotifyChaplain").HasColumnName("notify_chaplain") |> ignore
            b.Property<string>("RequestType").HasColumnName("request_type").IsRequired().HasMaxLength 1 |> ignore
            b.Property<string>("Requestor").HasColumnName("requestor") |> ignore
            b.Property<Guid>("SmallGroupId").HasColumnName("small_group_id") |> ignore
            b.Property<string>("Text").HasColumnName("request_text").IsRequired() |> ignore
            b.Property<DateTime>("UpdatedDate").HasColumnName("updated_date") |> ignore
            b.Property<Guid>("UserId").HasColumnName("user_id") |> ignore
            b.HasKey("Id") |> ignore
            b.HasIndex("SmallGroupId").HasDatabaseName "ix_prayer_request_small_group_id" |> ignore
            b.HasIndex("UserId").HasDatabaseName "ix_prayer_request_user_id" |> ignore
            b.ToTable("prayer_request") |> ignore)
        |> ignore

        modelBuilder.Entity (typeof<SmallGroup>, fun b ->
            b.Property<Guid>("Id").HasColumnName("id").ValueGeneratedOnAdd() |> ignore
            b.Property<Guid>("ChurchId").HasColumnName("church_id") |> ignore
            b.Property<string>("Name").HasColumnName("group_name").IsRequired() |> ignore
            b.HasKey("Id") |> ignore
            b.HasIndex("ChurchId").HasDatabaseName "ix_small_group_church_id" |> ignore
            b.ToTable("small_group") |> ignore)
        |> ignore

        modelBuilder.Entity (typeof<TimeZone>, fun b ->
            b.Property<string>("Id").HasColumnName("id").ValueGeneratedOnAdd() |> ignore
            b.Property<string>("Description").HasColumnName("description").IsRequired() |> ignore
            b.Property<bool>("IsActive").HasColumnName("is_active") |> ignore
            b.Property<int>("SortOrder").HasColumnName("sort_order") |> ignore
            b.HasKey("Id") |> ignore
            b.ToTable("time_zone") |> ignore)
        |> ignore

        modelBuilder.Entity (typeof<User>, fun b ->
            b.Property<Guid>("Id").HasColumnName("id").ValueGeneratedOnAdd() |> ignore
            b.Property<string>("Email").HasColumnName("email").IsRequired() |> ignore
            b.Property<string>("FirstName").HasColumnName("first_name").IsRequired() |> ignore
            b.Property<bool>("IsAdmin").HasColumnName("is_admin") |> ignore
            b.Property<string>("LastName").HasColumnName("last_name").IsRequired() |> ignore
            b.Property<string>("PasswordHash").HasColumnName("password_hash").IsRequired() |> ignore
            b.Property<Guid>("Salt").HasColumnName("salt") |> ignore
            b.HasKey("Id") |> ignore
            b.ToTable("pt_user") |> ignore)
        |> ignore

        modelBuilder.Entity (typeof<UserSmallGroup>, fun b ->
            b.Property<Guid>("UserId").HasColumnName("user_id") |> ignore
            b.Property<Guid>("SmallGroupId").HasColumnName("small_group_id") |> ignore
            b.HasKey("UserId", "SmallGroupId") |> ignore
            b.HasIndex("SmallGroupId").HasDatabaseName "ix_user_small_group_small_group_id" |> ignore
            b.ToTable("user_small_group") |> ignore)
        |> ignore

        modelBuilder.Entity (typeof<ListPreferences>, fun b ->
            b.HasOne("PrayerTracker.Entities.SmallGroup")
                .WithOne("Preferences")
                .HasForeignKey("PrayerTracker.Entities.ListPreferences", "SmallGroupId")
                .OnDelete(DeleteBehavior.Cascade)
            |> ignore
            b.HasOne("PrayerTracker.Entities.TimeZone", "TimeZone")
                .WithMany()
                .HasForeignKey("TimeZoneId")
                .OnDelete(DeleteBehavior.Cascade)
            |> ignore)
        |> ignore
        
        modelBuilder.Entity (typeof<Member>, fun b ->
            b.HasOne("PrayerTracker.Entities.SmallGroup", "SmallGroup")
                .WithMany("Members")
                .HasForeignKey("SmallGroupId")
                .OnDelete(DeleteBehavior.Cascade)
            |> ignore)
        |> ignore

        modelBuilder.Entity (typeof<PrayerRequest>, fun b ->
            b.HasOne("PrayerTracker.Entities.SmallGroup", "SmallGroup")
                .WithMany("PrayerRequests")
                .HasForeignKey("SmallGroupId")
                .OnDelete(DeleteBehavior.Cascade)
            |> ignore
            b.HasOne("PrayerTracker.Entities.User", "User")
                .WithMany()
                .HasForeignKey("UserId")
                .OnDelete(DeleteBehavior.Cascade)
            |> ignore)
        |> ignore

        modelBuilder.Entity (typeof<SmallGroup>, fun b ->
            b.HasOne("PrayerTracker.Entities.Church", "Church")
                .WithMany("SmallGroups")
                .HasForeignKey("ChurchId")
                .OnDelete(DeleteBehavior.Cascade)
            |> ignore)
        |> ignore

        modelBuilder.Entity (typeof<UserSmallGroup>, fun b ->
            b.HasOne("PrayerTracker.Entities.SmallGroup", "SmallGroup")
                .WithMany("Users")
                .HasForeignKey("SmallGroupId")
                .OnDelete(DeleteBehavior.Cascade)
            |> ignore
            b.HasOne("PrayerTracker.Entities.User", "User")
                .WithMany("SmallGroups")
                .HasForeignKey("UserId")
                .OnDelete(DeleteBehavior.Cascade)
            |> ignore)
        |> ignore
