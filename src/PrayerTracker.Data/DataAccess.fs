[<AutoOpen>]
module PrayerTracker.DataAccess

open FSharp.Control.Tasks.ContextInsensitive
open Microsoft.EntityFrameworkCore
open PrayerTracker.Entities
open System.Collections.Generic
open System.Linq

[<AutoOpen>]
module private Helpers =
  
  open Microsoft.FSharpLu
  open System.Threading.Tasks

  /// Central place to append sort criteria for prayer request queries
  let reqSort sort (q : IQueryable<PrayerRequest>) =
    match sort with
    | SortByDate ->
        query {
          for req in q do
            sortByDescending req.updatedDate
            thenByDescending req.enteredDate
            thenBy           req.requestor
          }
    | SortByRequestor ->
        query {
          for req in q do
            sortBy           req.requestor
            thenByDescending req.updatedDate
            thenByDescending req.enteredDate
          }

  /// Convert a possibly-null object to an option, wrapped as a task
  let toOptionTask<'T> (item : 'T) = (Option.fromObject >> Task.FromResult) item


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
    query {
      for ch in this.Churches.AsNoTracking () do
        where (ch.churchId = cId)
        exactlyOneOrDefault
      }
    |> toOptionTask
      
  /// Find all churches
  member this.AllChurches () =
    task {
      let q =
        query {
          for ch in this.Churches.AsNoTracking () do
            sortBy ch.name
          }
      let! churches = q.ToListAsync ()
      return List.ofSeq churches
      }

  (*-- MEMBER EXTENSIONS --*)

  /// Get a small group member by its Id
  member this.TryMemberById mId =
    query {
      for mbr in this.Members.AsNoTracking () do
        where (mbr.memberId = mId)
        select mbr
        exactlyOneOrDefault
      }
    |> toOptionTask

  /// Find all members for a small group
  member this.AllMembersForSmallGroup gId =
    task {
      let q =
        query {
          for mbr in this.Members.AsNoTracking () do
            where (mbr.smallGroupId = gId)
            sortBy mbr.memberName
          }
      let! mbrs = q.ToListAsync ()
      return List.ofSeq mbrs
      }

  /// Count members for a small group
  member this.CountMembersForSmallGroup gId =
    this.Members.CountAsync (fun m -> m.smallGroupId = gId)

  (*-- PRAYER REQUEST EXTENSIONS --*)

  /// Get a prayer request by its Id
  member this.TryRequestById reqId =
    query {
      for req in this.PrayerRequests.AsNoTracking () do
        where (req.prayerRequestId = reqId)
        exactlyOneOrDefault
      }
    |> toOptionTask

  /// Get all (or active) requests for a small group as of now or the specified date
  // TODO: why not make this an async list like the rest of these methods?
  member this.AllRequestsForSmallGroup (grp : SmallGroup) clock listDate activeOnly pageNbr : PrayerRequest seq =
    let theDate = match listDate with Some dt -> dt | _ -> grp.localDateNow clock
    query {
      for req in this.PrayerRequests.AsNoTracking () do
        where (req.smallGroupId = grp.smallGroupId)
      }
    |> function
    | q when activeOnly ->
        let asOf = theDate.AddDays(-(float grp.preferences.daysToExpire)).Date
        query {
          for req in q do
            where ( (    req.updatedDate > asOf
                      || req.expiration  = Manual
                      || req.requestType = LongTermRequest
                      || req.requestType = Expecting)
                    && req.expiration <> Forced)
          }
    | q -> q
    |> reqSort grp.preferences.requestSort
    |> function
    | q ->
        match activeOnly with
        | true -> upcast q
        | false ->
            upcast query {
              for req in q do
                skip ((pageNbr - 1) * grp.preferences.pageSize)
                take grp.preferences.pageSize
                }
      
  /// Count prayer requests for the given small group Id
  member this.CountRequestsBySmallGroup gId =
    this.PrayerRequests.CountAsync (fun pr -> pr.smallGroupId = gId)

  /// Count prayer requests for the given church Id
  member this.CountRequestsByChurch cId =
    this.PrayerRequests.CountAsync (fun pr -> pr.smallGroup.churchId = cId)

  /// Get all (or active) requests for a small group as of now or the specified date
  // TODO: same as above...
  member this.SearchRequestsForSmallGroup (grp : SmallGroup) (searchTerm : string) pageNbr : PrayerRequest seq =
    let pgSz   = grp.preferences.pageSize
    let toSkip = (pageNbr - 1) * pgSz
    let sql    =
      """ SELECT * FROM pt."PrayerRequest" WHERE "SmallGroupId" = {0} AND "Text" ILIKE {1}
        UNION
          SELECT * FROM pt."PrayerRequest" WHERE "SmallGroupId" = {0} AND COALESCE("Requestor", '') ILIKE {1}"""
    let like = sprintf "%%%s%%"
    this.PrayerRequests.FromSqlRaw(sql, grp.smallGroupId, like searchTerm).AsNoTracking ()
    |> reqSort grp.preferences.requestSort
    |> function
    | q ->
        upcast query {
          for req in q do
            skip toSkip
            take pgSz
          }

  (*-- SMALL GROUP EXTENSIONS --*)

  /// Find a small group by its Id
  member this.TryGroupById gId =
    query {
      for grp in this.SmallGroups.AsNoTracking().Include (fun sg -> sg.preferences) do
        where (grp.smallGroupId = gId)
        exactlyOneOrDefault
      }
    |> toOptionTask

  /// Get small groups that are public or password protected
  member this.PublicAndProtectedGroups () =
    task {
      let smallGroups = this.SmallGroups.AsNoTracking().Include(fun sg -> sg.preferences).Include (fun sg -> sg.church)
      let q =
        query {
          for grp in smallGroups do
            where (   grp.preferences.isPublic
                   || (grp.preferences.groupPassword <> null && grp.preferences.groupPassword <> ""))
            sortBy grp.church.name
            thenBy grp.name
          }
      let! grps = q.ToListAsync ()
      return List.ofSeq grps
      }

  /// Get small groups that are password protected
  member this.ProtectedGroups () =
    task {
      let q =
        query {
          for grp in this.SmallGroups.AsNoTracking().Include (fun sg -> sg.church) do
            where (grp.preferences.groupPassword <> null && grp.preferences.groupPassword <> "")
            sortBy grp.church.name
            thenBy grp.name
          }
      let! grps = q.ToListAsync ()
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
      let q =
        query {
          for grp in this.SmallGroups.AsNoTracking().Include (fun sg -> sg.church) do
            sortBy grp.church.name
            thenBy grp.name
          }
      let! grps = q.ToListAsync ()
      return grps
        |> Seq.map (fun grp -> grp.smallGroupId.ToString "N", sprintf "%s | %s" grp.church.name grp.name)
        |> List.ofSeq
      }

  /// Log on a small group
  member this.TryGroupLogOnByPassword gId pw =
    task {
      match! this.TryGroupById gId with
      | None -> return None
      | Some grp ->
          match pw = grp.preferences.groupPassword with
          | true -> return Some grp
          | _ -> return None
      }

  /// Check a cookie log on for a small group
  member this.TryGroupLogOnByCookie gId pwHash (hasher : string -> string) =
    task {
      match! this.TryGroupById gId with
      | None -> return None
      | Some grp ->
          match pwHash = hasher grp.preferences.groupPassword with
          | true -> return Some grp
          | _ -> return None
      }

  /// Count small groups for the given church Id
  member this.CountGroupsByChurch cId =
    this.SmallGroups.CountAsync (fun sg -> sg.churchId = cId)
      
  (*-- TIME ZONE EXTENSIONS --*)

  /// Get a time zone by its Id
  member this.TryTimeZoneById tzId =
    query {
      for tz in this.TimeZones do
        where (tz.timeZoneId = tzId)
        exactlyOneOrDefault
      }
    |> toOptionTask

  /// Get all time zones
  member this.AllTimeZones () =
    task {
      let q =
        query {
          for tz in this.TimeZones do
            sortBy tz.sortOrder
          }
      let! tzs = q.ToListAsync ()
      return List.ofSeq tzs
      }
  
  (*-- USER EXTENSIONS --*)

  /// Find a user by its Id
  member this.TryUserById uId =
    query {
      for usr in this.Users.AsNoTracking () do
        where (usr.userId = uId)
        exactlyOneOrDefault
      }
    |> toOptionTask

  /// Find a user by its e-mail address and authorized small group
  member this.TryUserByEmailAndGroup email gId =
    query {
      for usr in this.Users.AsNoTracking () do
        where (usr.emailAddress = email && usr.smallGroups.Any (fun xref -> xref.smallGroupId = gId))
        exactlyOneOrDefault
      }
    |> toOptionTask

  /// Find a user by its Id (tracked entity), eagerly loading the user's groups
  member this.TryUserByIdWithGroups uId =
    query {
      for usr in this.Users.AsNoTracking().Include (fun u -> u.smallGroups) do
        where (usr.userId = uId)
        exactlyOneOrDefault
      }
    |> toOptionTask

  /// Get a list of all users
  member this.AllUsers () =
    task {
      let q =
        query {
          for usr in this.Users.AsNoTracking () do
            sortBy usr.lastName
            thenBy usr.firstName
          }
      let! usrs = q.ToListAsync ()
      return List.ofSeq usrs
      }

  /// Get all PrayerTracker users as members (used to send e-mails)
  member this.AllUsersAsMembers () =
    task {
      let q =
        query {
          for usr in this.Users.AsNoTracking () do
            sortBy usr.lastName
            thenBy usr.firstName
            select { Member.empty with email = usr.emailAddress; memberName = usr.fullName }
          }
      let! usrs = q.ToListAsync ()
      return List.ofSeq usrs
      }

  /// Find a user based on their credentials
  member this.TryUserLogOnByPassword email pwHash gId =
    query {
      for usr in this.Users.AsNoTracking () do
        where (   usr.emailAddress = email
               && usr.passwordHash = pwHash
               && usr.smallGroups.Any (fun xref -> xref.smallGroupId = gId))
        exactlyOneOrDefault
      }
    |> toOptionTask

  /// Find a user based on credentials stored in a cookie
  member this.TryUserLogOnByCookie uId gId pwHash =
    task {
      match! this.TryUserByIdWithGroups uId with
      | None -> return None
      | Some usr ->
          match pwHash = usr.passwordHash && usr.smallGroups |> Seq.exists (fun xref -> xref.smallGroupId = gId) with
          | true ->
              this.Entry<User>(usr).State <- EntityState.Detached
              return Some { usr with passwordHash = ""; salt = None; smallGroups = List<UserSmallGroup>() }
          | _ -> return None
      }

  /// Count the number of users for a small group
  member this.CountUsersBySmallGroup gId =
    this.Users.CountAsync (fun u -> u.smallGroups.Any (fun xref -> xref.smallGroupId = gId))

  /// Count the number of users for a church
  member this.CountUsersByChurch cId =
    this.Users.CountAsync (fun u -> u.smallGroups.Any (fun xref -> xref.smallGroup.churchId = cId))
