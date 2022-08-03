namespace PrayerTracker.Migrations

open System
open Microsoft.EntityFrameworkCore
open Microsoft.EntityFrameworkCore.Infrastructure
open Npgsql.EntityFrameworkCore.PostgreSQL.Metadata
open PrayerTracker
open PrayerTracker.Entities

[<DbContext (typeof<AppDbContext>)>]
type AppDbContextModelSnapshot () =
    inherit ModelSnapshot ()

    override _.BuildModel (modelBuilder : ModelBuilder) =
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
                .HasForeignKey("PrayerTracker.Entities.ListPreferences", "smallGroupId")
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
