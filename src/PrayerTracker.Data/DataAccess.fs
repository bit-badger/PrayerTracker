[<AutoOpen>]
module PrayerTracker.DataAccess

open System.Linq
open PrayerTracker.Entities

[<AutoOpen>]
module private Helpers =
  
    /// Central place to append sort criteria for prayer request queries
    let reqSort sort (q : IQueryable<PrayerRequest>) =
        match sort with
        | SortByDate ->
            q.OrderByDescending(fun req -> req.updatedDate)
                .ThenByDescending(fun req -> req.enteredDate)
                .ThenBy (fun req -> req.requestor)
        | SortByRequestor ->
            q.OrderBy(fun req -> req.requestor)
                .ThenByDescending(fun req -> req.updatedDate)
                .ThenByDescending (fun req -> req.enteredDate)
    
    /// Paginate a prayer request query
    let paginate (pageNbr : int) pageSize (q : IQueryable<PrayerRequest>) =
        if pageNbr > 0 then q.Skip((pageNbr - 1) * pageSize).Take pageSize else q


open System
open System.Collections.Generic
open Microsoft.EntityFrameworkCore
open Microsoft.FSharpLu

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
    member this.TryChurchById cId = backgroundTask {
        let! church = this.Churches.SingleOrDefaultAsync (fun ch -> ch.churchId = cId)
        return Option.fromObject church
    }
        
    /// Find all churches
    member this.AllChurches () = backgroundTask {
        let! churches = this.Churches.OrderBy(fun ch -> ch.name).ToListAsync ()
        return List.ofSeq churches
    }

    (*-- MEMBER EXTENSIONS --*)

    /// Get a small group member by its Id
    member this.TryMemberById mbrId = backgroundTask {
        let! mbr = this.Members.SingleOrDefaultAsync (fun m -> m.memberId = mbrId)
        return Option.fromObject mbr
    }

    /// Find all members for a small group
    member this.AllMembersForSmallGroup gId = backgroundTask {
        let! members =
            this.Members.Where(fun mbr -> mbr.smallGroupId = gId)
                .OrderBy(fun mbr -> mbr.memberName)
                .ToListAsync ()
        return List.ofSeq members
    }

    /// Count members for a small group
    member this.CountMembersForSmallGroup gId = backgroundTask {
        return! this.Members.CountAsync (fun m -> m.smallGroupId = gId)
    }
    
    (*-- PRAYER REQUEST EXTENSIONS --*)

    /// Get a prayer request by its Id
    member this.TryRequestById reqId = backgroundTask {
        let! req = this.PrayerRequests.SingleOrDefaultAsync (fun r -> r.prayerRequestId = reqId)
        return Option.fromObject req
    }

    /// Get all (or active) requests for a small group as of now or the specified date
    member this.AllRequestsForSmallGroup (grp : SmallGroup) clock listDate activeOnly pageNbr = backgroundTask {
        let theDate = match listDate with Some dt -> dt | _ -> grp.localDateNow clock
        let query =
            this.PrayerRequests.Where(fun req -> req.smallGroupId = grp.smallGroupId)
            |> function
            | q when activeOnly ->
                let asOf = DateTime (theDate.AddDays(-(float grp.preferences.daysToExpire)).Date.Ticks, DateTimeKind.Utc)
                q.Where(fun req ->
                        (   req.updatedDate > asOf
                         || req.expiration  = Manual
                         || req.requestType = LongTermRequest
                         || req.requestType = Expecting)
                     && req.expiration <> Forced)
                |> reqSort grp.preferences.requestSort
                |> paginate pageNbr grp.preferences.pageSize
            | q -> reqSort grp.preferences.requestSort q
        let! reqs = query.ToListAsync ()
        return List.ofSeq reqs
    }

    /// Count prayer requests for the given small group Id
    member this.CountRequestsBySmallGroup gId = backgroundTask {
        return! this.PrayerRequests.CountAsync (fun pr -> pr.smallGroupId = gId)
    }

    /// Count prayer requests for the given church Id
    member this.CountRequestsByChurch cId = backgroundTask {
        return! this.PrayerRequests.CountAsync (fun pr -> pr.smallGroup.churchId = cId)
    }

    /// Get all (or active) requests for a small group as of now or the specified date
    member this.SearchRequestsForSmallGroup (grp : SmallGroup) (searchTerm : string) pageNbr = backgroundTask {
        let sql = """
            SELECT * FROM pt."PrayerRequest" WHERE "SmallGroupId" = {0} AND "Text" ILIKE {1}
        UNION
            SELECT * FROM pt."PrayerRequest" WHERE "SmallGroupId" = {0} AND COALESCE("Requestor", '') ILIKE {1}"""
        let like  = sprintf "%%%s%%"
        let query =
            this.PrayerRequests.FromSqlRaw(sql, grp.smallGroupId, like searchTerm)
            |> reqSort grp.preferences.requestSort
            |> paginate pageNbr grp.preferences.pageSize
        let! reqs = query.ToListAsync ()
        return List.ofSeq reqs
    }
    
    (*-- SMALL GROUP EXTENSIONS --*)

    /// Find a small group by its Id
    member this.TryGroupById gId = backgroundTask {
        let! grp =
            this.SmallGroups.Include(fun sg -> sg.preferences)
                .SingleOrDefaultAsync (fun sg -> sg.smallGroupId = gId)
        return Option.fromObject grp
    }

    /// Get small groups that are public or password protected
    member this.PublicAndProtectedGroups () = backgroundTask {
        let! groups =
            this.SmallGroups.Include(fun sg -> sg.preferences).Include(fun sg -> sg.church)
                .Where(fun sg ->
                       sg.preferences.isPublic
                    || (sg.preferences.groupPassword <> null && sg.preferences.groupPassword <> ""))
                .OrderBy(fun sg -> sg.church.name).ThenBy(fun sg -> sg.name)
                .ToListAsync ()
        return List.ofSeq groups
    }

    /// Get small groups that are password protected
    member this.ProtectedGroups () = backgroundTask {
        let! groups =
            this.SmallGroups.Include(fun sg -> sg.church)
                .Where(fun sg -> sg.preferences.groupPassword <> null && sg.preferences.groupPassword <> "")
                .OrderBy(fun sg -> sg.church.name).ThenBy(fun sg -> sg.name)
                .ToListAsync ()
        return List.ofSeq groups
    }

    /// Get all small groups
    member this.AllGroups () = backgroundTask {
        let! groups =
            this.SmallGroups
                .Include(fun sg -> sg.church)
                .Include(fun sg -> sg.preferences)
                .Include(fun sg -> sg.preferences.timeZone)
                .OrderBy(fun sg -> sg.name)
                .ToListAsync ()
        return List.ofSeq groups
    }

    /// Get a small group list by their Id, with their church prepended to their name
    member this.GroupList () = backgroundTask {
        let! groups =
            this.SmallGroups.Include(fun sg -> sg.church)
                .OrderBy(fun sg -> sg.church.name).ThenBy(fun sg -> sg.name)
                .ToListAsync ()
        return groups
          |> Seq.map (fun sg -> sg.smallGroupId.ToString "N", $"{sg.church.name} | {sg.name}")
          |> List.ofSeq
    }

    /// Log on a small group
    member this.TryGroupLogOnByPassword gId pw = backgroundTask {
        match! this.TryGroupById gId with
        | None -> return None
        | Some grp -> return if pw = grp.preferences.groupPassword then Some grp else None
    }

    /// Check a cookie log on for a small group
    member this.TryGroupLogOnByCookie gId pwHash (hasher : string -> string) = backgroundTask {
        match! this.TryGroupById gId with
        | None -> return None
        | Some grp -> return if pwHash = hasher grp.preferences.groupPassword then Some grp else None
    }

    /// Count small groups for the given church Id
    member this.CountGroupsByChurch cId = backgroundTask {
        return! this.SmallGroups.CountAsync (fun sg -> sg.churchId = cId)
    }
        
    (*-- TIME ZONE EXTENSIONS --*)

    /// Get a time zone by its Id
    member this.TryTimeZoneById tzId = backgroundTask {
        let! zone = this.TimeZones.SingleOrDefaultAsync (fun tz -> tz.timeZoneId = tzId)
        return Option.fromObject zone
    }

    /// Get all time zones
    member this.AllTimeZones () = backgroundTask {
        let! zones = this.TimeZones.OrderBy(fun tz -> tz.sortOrder).ToListAsync ()
        return List.ofSeq zones
    }
    
    (*-- USER EXTENSIONS --*)

    /// Find a user by its Id
    member this.TryUserById uId = backgroundTask {
        let! usr = this.Users.SingleOrDefaultAsync (fun u -> u.userId = uId)
        return Option.fromObject usr
    }

    /// Find a user by its e-mail address and authorized small group
    member this.TryUserByEmailAndGroup email gId = backgroundTask {
        let! usr =
            this.Users.SingleOrDefaultAsync (fun u ->
                u.emailAddress = email && u.smallGroups.Any (fun xref -> xref.smallGroupId = gId))
        return Option.fromObject usr
    }
    
    /// Find a user by its Id, eagerly loading the user's groups
    member this.TryUserByIdWithGroups uId = backgroundTask {
        let! usr = this.Users.Include(fun u -> u.smallGroups).SingleOrDefaultAsync (fun u -> u.userId = uId)
        return Option.fromObject usr
    }

    /// Get a list of all users
    member this.AllUsers () = backgroundTask {
        let! users = this.Users.OrderBy(fun u -> u.lastName).ThenBy(fun u -> u.firstName).ToListAsync ()
        return List.ofSeq users
    }

    /// Get all PrayerTracker users as members (used to send e-mails)
    member this.AllUsersAsMembers () = backgroundTask {
        let! users = this.AllUsers ()
        return users |> List.map (fun u -> { Member.empty with email = u.emailAddress; memberName = u.fullName })
    }

    /// Find a user based on their credentials
    member this.TryUserLogOnByPassword email pwHash gId = backgroundTask {
        let! usr =
            this.Users.SingleOrDefaultAsync (fun u ->
                   u.emailAddress = email
                && u.passwordHash = pwHash
                && u.smallGroups.Any (fun xref -> xref.smallGroupId = gId))
        return Option.fromObject usr
    }

    /// Find a user based on credentials stored in a cookie
    member this.TryUserLogOnByCookie uId gId pwHash = backgroundTask {
        match! this.TryUserByIdWithGroups uId with
        | None -> return None
        | Some usr ->
            if pwHash = usr.passwordHash && usr.smallGroups |> Seq.exists (fun xref -> xref.smallGroupId = gId) then
                return Some { usr with passwordHash = ""; salt = None; smallGroups = List<UserSmallGroup>() }
            else return None
    }

    /// Count the number of users for a small group
    member this.CountUsersBySmallGroup gId = backgroundTask {
        return! this.Users.CountAsync (fun u -> u.smallGroups.Any (fun xref -> xref.smallGroupId = gId))
    }

    /// Count the number of users for a church
    member this.CountUsersByChurch cId = backgroundTask {
        return! this.Users.CountAsync (fun u -> u.smallGroups.Any (fun xref -> xref.smallGroup.churchId = cId))
    }
