namespace PrayerTracker.Migrations

open Microsoft.EntityFrameworkCore
open Microsoft.EntityFrameworkCore.Infrastructure
open Npgsql.EntityFrameworkCore.PostgreSQL.Metadata
open PrayerTracker
open PrayerTracker.Entities
open System

[<DbContext (typeof<AppDbContext>)>]
type AppDbContextModelSnapshot () =
  inherit ModelSnapshot ()

  override __.BuildModel (modelBuilder : ModelBuilder) =
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
          b.Property<string>("defaultEmailType").IsRequired().ValueGeneratedOnAdd().HasDefaultValue("H").HasMaxLength(1) |> ignore
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
          b.Property<DateTime>("enteredDate") |> ignore
          b.Property<string>("expiration").IsRequired().HasMaxLength(1) |> ignore
          b.Property<bool>("notifyChaplain") |> ignore
          b.Property<string>("requestType").IsRequired().HasMaxLength(1) |> ignore
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
