[<AutoOpen>]
module PrayerTracker.DataAccess

open FSharp.Control.Tasks.ContextInsensitive
open Microsoft.EntityFrameworkCore
open Microsoft.FSharpLu
open PrayerTracker.Entities
open System.Collections.Generic
open System.Linq

[<AutoOpen>]
module private Helpers =
  
  /// Central place to append sort criteria for prayer request queries
  let reqSort sort (query : IQueryable<PrayerRequest>) =
    match sort with
    | "D" ->
        query.OrderByDescending(fun pr -> pr.updatedDate)
          .ThenByDescending(fun pr -> pr.enteredDate)
          .ThenBy(fun pr -> pr.requestor)
    | _ ->
        query.OrderBy(fun pr -> pr.requestor)
          .ThenByDescending(fun pr -> pr.updatedDate)
          .ThenByDescending(fun pr -> pr.enteredDate)


type AppDbContext with
  
  (*-- DISCONNECTED DATA EXTENSIONS --*)

  /// Add an entity entry to the tracked data context with the status of Added
  member this.AddEntry<'TEntity when 'TEntity : not struct> (e : 'TEntity) =
    this.Entry<'TEntity>(e).State <- EntityState.Added
  
  /// Add an entity entry to the tracked data context with the status of Updated
  member this.UpdateEntry<'TEntity when 'TEntity : not struct> (e : 'TEntity) =
    this.Entry<'TEntity>(e).State <- EntityState.Modified

  /// Add an entity entry to the tracked data context with the status of Deleted
  member this.RemoveEntry<'TEntity when 'TEntity : not struct> (e : 'TEntity) =
    this.Entry<'TEntity>(e).State <- EntityState.Deleted

  (*-- CHURCH EXTENSIONS --*)

  /// Find a church by its Id
  member this.TryChurchById cId =
    task {
      let! church = this.Churches.AsNoTracking().FirstOrDefaultAsync (fun c -> c.churchId = cId)
      return Option.fromObject church
      }
      
  /// Find all churches
  member this.AllChurches () =
    task {
      let! churches = this.Churches.AsNoTracking().OrderBy(fun c -> c.name).ToListAsync ()
      return List.ofSeq churches
      }

  (*-- MEMBER EXTENSIONS --*)

  /// Get a small group member by its Id
  member this.TryMemberById mId =
    task {
      let! mbr = this.Members.AsNoTracking().FirstOrDefaultAsync (fun m -> m.memberId = mId)
      return Option.fromObject mbr
      }

  /// Find all members for a small group
  member this.AllMembersForSmallGroup gId =
    task {
      let! mbrs =
        this.Members.AsNoTracking()
          .Where(fun m -> m.smallGroupId = gId)
          .OrderBy(fun m -> m.memberName)
          .ToListAsync ()
      return List.ofSeq mbrs
      }

  /// Count members for a small group
  member this.CountMembersForSmallGroup gId =
    this.Members.CountAsync (fun m -> m.smallGroupId = gId)

  (*-- PRAYER REQUEST EXTENSIONS --*)

  /// Get a prayer request by its Id
  member this.TryRequestById reqId =
    task {
      let! req = this.PrayerRequests.AsNoTracking().FirstOrDefaultAsync (fun pr -> pr.prayerRequestId = reqId)
      return Option.fromObject req
      }

  /// Get all (or active) requests for a small group as of now or the specified date
  member this.AllRequestsForSmallGroup (grp : SmallGroup) clock listDate activeOnly pageNbr : PrayerRequest seq =
    let theDate = match listDate with Some dt -> dt | _ -> grp.localDateNow clock
    upcast (
      this.PrayerRequests.AsNoTracking().Where(fun pr -> pr.smallGroupId = grp.smallGroupId)
      |> function
      | query when activeOnly ->
          let asOf = theDate.AddDays(-(float grp.preferences.daysToExpire)).Date
          query.Where(fun pr ->
              (pr.updatedDate > asOf
                  || pr.doNotExpire
                  || RequestType.Recurring = pr.requestType
                  || RequestType.Expecting = pr.requestType)
              && not pr.isManuallyExpired)
      | query -> query
      |> reqSort grp.preferences.requestSort
      |> function
      | query ->
          match activeOnly with
          | true -> query.Skip 0
          | false -> query.Skip((pageNbr - 1) * grp.preferences.pageSize).Take grp.preferences.pageSize)
      
  /// Count prayer requests for the given small group Id
  member this.CountRequestsBySmallGroup gId =
    this.PrayerRequests.CountAsync (fun pr -> pr.smallGroupId = gId)

  /// Count prayer requests for the given church Id
  member this.CountRequestsByChurch cId =
    this.PrayerRequests.CountAsync (fun pr -> pr.smallGroup.churchId = cId)

  /// Get all (or active) requests for a small group as of now or the specified date
  member this.SearchRequestsForSmallGroup (grp : SmallGroup) (searchTerm : string) pageNbr : PrayerRequest seq =
    let pgSz = grp.preferences.pageSize
    let skip = (pageNbr - 1) * pgSz
    let sql  =
      """ SELECT * FROM pt."PrayerRequest" WHERE "SmallGroupId" = {0} AND "Text" ILIKE {1}
        UNION
          SELECT * FROM pt."PrayerRequest" WHERE "SmallGroupId" = {0} AND COALESCE("Requestor", '') ILIKE {1}"""
      |> RawSqlString
    let like = sprintf "%%%s%%"
    upcast (
      this.PrayerRequests.FromSql(sql, grp.smallGroupId, like searchTerm).AsNoTracking ()
      |> reqSort grp.preferences.requestSort
      |> function query -> (query.Skip skip).Take pgSz)

  (*-- SMALL GROUP EXTENSIONS --*)

  /// Find a small group by its Id
  member this.TryGroupById gId =
    task {
      let! grp =
        this.SmallGroups.AsNoTracking()
          .Include(fun sg -> sg.preferences)
          .FirstOrDefaultAsync (fun sg -> sg.smallGroupId = gId)
      return Option.fromObject grp
      }

  /// Get small groups that are public or password protected
  member this.PublicAndProtectedGroups () =
    task {
      let! grps = 
        this.SmallGroups.AsNoTracking()
          .Include(fun sg -> sg.preferences)
          .Include(fun sg -> sg.church)
          .Where(fun sg ->
              sg.preferences.isPublic || (sg.preferences.groupPassword <> null && sg.preferences.groupPassword <> ""))
          .OrderBy(fun sg -> sg.church.name)
          .ThenBy(fun sg -> sg.name)
          .ToListAsync ()
      return List.ofSeq grps
      }

  /// Get small groups that are password protected
  member this.ProtectedGroups () =
    task {
      let! grps =
        this.SmallGroups.AsNoTracking()
          .Include(fun sg -> sg.church)
          .Where(fun sg -> sg.preferences.groupPassword <> null && sg.preferences.groupPassword <> "")
          .OrderBy(fun sg -> sg.church.name)
          .ThenBy(fun sg -> sg.name)
          .ToListAsync ()
      return List.ofSeq grps
      }

  /// Get all small groups
  member this.AllGroups () =
    task {
      let! grps =
        this.SmallGroups.AsNoTracking()
          .Include(fun sg -> sg.church)
          .Include(fun sg -> sg.preferences)
          .Include(fun sg -> sg.preferences.timeZone)
          .OrderBy(fun sg -> sg.name)
          .ToListAsync ()
      return List.ofSeq grps
      }

  /// Get a small group list by their Id, with their church prepended to their name
  member this.GroupList () =
    task {
      let! grps =
        this.SmallGroups.AsNoTracking()
          .Include(fun sg -> sg.church)
          .OrderBy(fun sg -> sg.church.name)
          .ThenBy(fun sg -> sg.name)
          .ToListAsync ()
      return grps
        |> Seq.map (fun grp -> grp.smallGroupId.ToString "N", sprintf "%s | %s" grp.church.name grp.name)
        |> List.ofSeq
      }

  /// Log on a small group
  member this.TryGroupLogOnByPassword gId pw =
    task {
      let! grp = this.TryGroupById gId
      match grp with
      | None -> return None
      | Some g ->
          match pw = g.preferences.groupPassword with
          | true -> return grp
          | _ -> return None
      }

  /// Check a cookie log on for a small group
  member this.TryGroupLogOnByCookie gId pwHash (hasher : string -> string) =
    task {
      let! grp = this.TryGroupById gId
      match grp with
      | None -> return None
      | Some g ->
          match pwHash = hasher g.preferences.groupPassword with
          | true -> return grp
          | _ -> return None
      }

  /// Count small groups for the given church Id
  member this.CountGroupsByChurch cId =
    this.SmallGroups.CountAsync (fun sg -> sg.churchId = cId)
      
  (*-- TIME ZONE EXTENSIONS --*)

  /// Get a time zone by its Id
  member this.TryTimeZoneById tzId =
    task {
      let! tz = this.TimeZones.FirstOrDefaultAsync (fun t -> t.timeZoneId = tzId)
      return Option.fromObject tz
      }

  /// Get all time zones
  member this.AllTimeZones () =
    task {
      let! tzs = this.TimeZones.OrderBy(fun t -> t.sortOrder).ToListAsync ()
      return List.ofSeq tzs
      }
  
  (*-- USER EXTENSIONS --*)

  /// Find a user by its Id
  member this.TryUserById uId =
    task {
      let! user = this.Users.AsNoTracking().FirstOrDefaultAsync (fun u -> u.userId = uId)
      return Option.fromObject user
      }

  /// Find a user by its e-mail address and authorized small group
  member this.TryUserByEmailAndGroup email gId =
    task {
      let! user =
        this.Users.AsNoTracking().FirstOrDefaultAsync (fun u ->
            u.emailAddress = email
            && u.smallGroups.Any (fun xref -> xref.smallGroupId = gId))
      return Option.fromObject user
      }

  /// Find a user by its Id (tracked entity), eagerly loading the user's groups
  member this.TryUserByIdWithGroups uId =
    task {
      let! user = this.Users.Include(fun u -> u.smallGroups).FirstOrDefaultAsync (fun u -> u.userId = uId)
      return Option.fromObject user
      }

  /// Get a list of all users
  member this.AllUsers () =
    task {
      let! usrs = this.Users.AsNoTracking().OrderBy(fun u -> u.lastName).ThenBy(fun u -> u.firstName).ToListAsync ()
      return List.ofSeq usrs
      }

  /// Get all PrayerTracker users as members (used to send e-mails)
  member this.AllUsersAsMembers () =
    task {
      let! usrs =
        this.Users.AsNoTracking().OrderBy(fun u -> u.lastName).ThenBy(fun u -> u.firstName).ToListAsync ()
      return usrs
        |> Seq.map (fun u -> { Member.empty with email = u.emailAddress; memberName = u.fullName })
        |> List.ofSeq
      }

  /// Find a user based on their credentials
  member this.TryUserLogOnByPassword email pwHash gId =
    task {
      let! user =
        this.Users.FirstOrDefaultAsync (fun u ->
          u.emailAddress = email
          && u.passwordHash = pwHash
          && u.smallGroups.Any (fun xref -> xref.smallGroupId = gId))
      return Option.fromObject user
      }

  /// Find a user based on credentials stored in a cookie
  member this.TryUserLogOnByCookie uId gId pwHash =
    task {
      let! user = this.TryUserByIdWithGroups uId
      match user with
      | None -> return None
      | Some u ->
          match pwHash = u.passwordHash && u.smallGroups |> Seq.exists (fun xref -> xref.smallGroupId = gId) with
          | true ->
              this.Entry<User>(u).State <- EntityState.Detached
              return Some { u with passwordHash = ""; salt = None; smallGroups = List<UserSmallGroup>() }
          | _ -> return None
      }

  /// Count the number of users for a small group
  member this.CountUsersBySmallGroup gId =
    this.Users.CountAsync (fun u -> u.smallGroups.Any (fun xref -> xref.smallGroupId = gId))

  /// Count the number of users for a church
  member this.CountUsersByChurch cId =
    this.Users.CountAsync (fun u -> u.smallGroups.Any (fun xref -> xref.smallGroup.churchId = cId))
