namespace PrayerTracker

open Microsoft.EntityFrameworkCore
open PrayerTracker.Entities

/// EF Core data context for PrayerTracker
[<AllowNullLiteral>]
type AppDbContext (options : DbContextOptions<AppDbContext>) =
    inherit DbContext (options)

    [<DefaultValue>]
    val mutable private churches       : DbSet<Church>
    [<DefaultValue>]
    val mutable private members        : DbSet<Member>
    [<DefaultValue>]
    val mutable private prayerRequests : DbSet<PrayerRequest>
    [<DefaultValue>]
    val mutable private preferences    : DbSet<ListPreferences>
    [<DefaultValue>]
    val mutable private smallGroups    : DbSet<SmallGroup>
    [<DefaultValue>]
    val mutable private timeZones      : DbSet<TimeZone>
    [<DefaultValue>]
    val mutable private users          : DbSet<User>
    [<DefaultValue>]
    val mutable private userGroupXref  : DbSet<UserSmallGroup>

    /// Churches
    member this.Churches
      with get() = this.churches
       and set v = this.churches <- v

    /// Small group members
    member this.Members
      with get() = this.members
       and set v = this.members <- v

    /// Prayer requests
    member this.PrayerRequests
      with get() = this.prayerRequests
       and set v = this.prayerRequests <- v

    /// Request list preferences (by class)
    member this.Preferences
      with get() = this.preferences
       and set v = this.preferences <- v

    /// Small groups
    member this.SmallGroups
      with get() = this.smallGroups
       and set v = this.smallGroups <- v

    /// Time zones
    member this.TimeZones
      with get() = this.timeZones
       and set v = this.timeZones <- v

    /// Users
    member this.Users
      with get() = this.users
       and set v = this.users <- v

    /// User / small group cross-reference
    member this.UserGroupXref
      with get() = this.userGroupXref
       and set v = this.userGroupXref <- v

    /// F#-style async for saving changes
    member this.AsyncSaveChanges () =
        this.SaveChangesAsync () |> Async.AwaitTask

    override _.OnConfiguring (optionsBuilder : DbContextOptionsBuilder) =
        base.OnConfiguring optionsBuilder
        optionsBuilder.UseQueryTrackingBehavior QueryTrackingBehavior.NoTracking |> ignore
    
    override _.OnModelCreating (modelBuilder : ModelBuilder) =
        base.OnModelCreating modelBuilder

        modelBuilder.HasDefaultSchema "pt" |> ignore

        [ Church.configureEF
          ListPreferences.configureEF
          Member.configureEF
          PrayerRequest.configureEF
          SmallGroup.configureEF
          TimeZone.configureEF
          User.configureEF
          UserSmallGroup.configureEF
          ]
        |> List.iter (fun x -> x modelBuilder)
