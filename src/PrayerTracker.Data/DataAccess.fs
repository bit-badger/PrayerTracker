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
            q.OrderByDescending(fun req -> req.UpdatedDate)
                .ThenByDescending(fun req -> req.EnteredDate)
                .ThenBy (fun req -> req.Requestor)
        | SortByRequestor ->
            q.OrderBy(fun req -> req.Requestor)
                .ThenByDescending(fun req -> req.UpdatedDate)
                .ThenByDescending (fun req -> req.EnteredDate)
    
    /// Paginate a prayer request query
    let paginate (pageNbr : int) pageSize (q : IQueryable<PrayerRequest>) =
        if pageNbr > 0 then q.Skip((pageNbr - 1) * pageSize).Take pageSize else q


open System
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
    member this.TryChurchById churchId = backgroundTask {
        let! church = this.Churches.SingleOrDefaultAsync (fun ch -> ch.Id = churchId)
        return Option.fromObject church
    }
        
    /// Find all churches
    member this.AllChurches () = backgroundTask {
        let! churches = this.Churches.OrderBy(fun ch -> ch.Name).ToListAsync ()
        return List.ofSeq churches
    }

    (*-- MEMBER EXTENSIONS --*)

    /// Get a small group member by its Id
    member this.TryMemberById memberId = backgroundTask {
        let! mbr = this.Members.SingleOrDefaultAsync (fun m -> m.Id = memberId)
        return Option.fromObject mbr
    }

    /// Find all members for a small group
    member this.AllMembersForSmallGroup groupId = backgroundTask {
        let! members =
            this.Members.Where(fun mbr -> mbr.SmallGroupId = groupId)
                .OrderBy(fun mbr -> mbr.Name)
                .ToListAsync ()
        return List.ofSeq members
    }

    /// Count members for a small group
    member this.CountMembersForSmallGroup groupId = backgroundTask {
        return! this.Members.CountAsync (fun m -> m.SmallGroupId = groupId)
    }
    
    (*-- PRAYER REQUEST EXTENSIONS --*)

    /// Get a prayer request by its Id
    member this.TryRequestById reqId = backgroundTask {
        let! req = this.PrayerRequests.SingleOrDefaultAsync (fun r -> r.Id = reqId)
        return Option.fromObject req
    }

    /// Get all (or active) requests for a small group as of now or the specified date
    member this.AllRequestsForSmallGroup (grp : SmallGroup) clock listDate activeOnly pageNbr = backgroundTask {
        let theDate = match listDate with Some dt -> dt | _ -> grp.LocalDateNow clock
        let query =
            this.PrayerRequests.Where(fun req -> req.SmallGroupId = grp.Id)
            |> function
            | q when activeOnly ->
                let asOf = DateTime (theDate.AddDays(-(float grp.Preferences.DaysToExpire)).Date.Ticks, DateTimeKind.Utc)
                q.Where(fun req ->
                        (   req.UpdatedDate > asOf
                         || req.Expiration  = Manual
                         || req.RequestType = LongTermRequest
                         || req.RequestType = Expecting)
                     && req.Expiration <> Forced)
                |> reqSort grp.Preferences.RequestSort
                |> paginate pageNbr grp.Preferences.PageSize
            | q -> reqSort grp.Preferences.RequestSort q
        let! reqs = query.ToListAsync ()
        return List.ofSeq reqs
    }

    /// Count prayer requests for the given small group Id
    member this.CountRequestsBySmallGroup groupId = backgroundTask {
        return! this.PrayerRequests.CountAsync (fun pr -> pr.SmallGroupId = groupId)
    }

    /// Count prayer requests for the given church Id
    member this.CountRequestsByChurch churchId = backgroundTask {
        return! this.PrayerRequests.CountAsync (fun pr -> pr.SmallGroup.ChurchId = churchId)
    }

    /// Search requests for a small group using the given case-insensitive search term
    member this.SearchRequestsForSmallGroup (grp : SmallGroup) (searchTerm : string) pageNbr = backgroundTask {
        let sql = """
            SELECT * FROM pt.prayer_request WHERE small_group_id = {0} AND request_text ILIKE {1}
        UNION
            SELECT * FROM pt.prayer_request WHERE small_group_id = {0} AND COALESCE(requestor, '') ILIKE {1}"""
        let like  = sprintf "%%%s%%"
        let query =
            this.PrayerRequests.FromSqlRaw (sql, grp.Id.Value, like searchTerm)
            |> reqSort grp.Preferences.RequestSort
            |> paginate pageNbr grp.Preferences.PageSize
        let! reqs = query.ToListAsync ()
        return List.ofSeq reqs
    }
    
    (*-- SMALL GROUP EXTENSIONS --*)

    /// Find a small group by its Id
    member this.TryGroupById groupId = backgroundTask {
        let! grp =
            this.SmallGroups.Include(fun sg -> sg.Preferences)
                .SingleOrDefaultAsync (fun sg -> sg.Id = groupId)
        return Option.fromObject grp
    }

    /// Get small groups that are public or password protected
    member this.PublicAndProtectedGroups () = backgroundTask {
        let! groups =
            this.SmallGroups.Include(fun sg -> sg.Preferences).Include(fun sg -> sg.Church)
                .Where(fun sg ->
                       sg.Preferences.IsPublic
                    || (sg.Preferences.GroupPassword <> null && sg.Preferences.GroupPassword <> ""))
                .OrderBy(fun sg -> sg.Church.Name).ThenBy(fun sg -> sg.Name)
                .ToListAsync ()
        return List.ofSeq groups
    }

    /// Get small groups that are password protected
    member this.ProtectedGroups () = backgroundTask {
        let! groups =
            this.SmallGroups.Include(fun sg -> sg.Church)
                .Where(fun sg -> sg.Preferences.GroupPassword <> null && sg.Preferences.GroupPassword <> "")
                .OrderBy(fun sg -> sg.Church.Name).ThenBy(fun sg -> sg.Name)
                .ToListAsync ()
        return List.ofSeq groups
    }

    /// Get all small groups
    member this.AllGroups () = backgroundTask {
        let! groups =
            this.SmallGroups
                .Include(fun sg -> sg.Church)
                .Include(fun sg -> sg.Preferences)
                .Include(fun sg -> sg.Preferences.TimeZone)
                .OrderBy(fun sg -> sg.Name)
                .ToListAsync ()
        return List.ofSeq groups
    }

    /// Get a small group list by their Id, with their church prepended to their name
    member this.GroupList () = backgroundTask {
        let! groups =
            this.SmallGroups.Include(fun sg -> sg.Church)
                .OrderBy(fun sg -> sg.Church.Name).ThenBy(fun sg -> sg.Name)
                .ToListAsync ()
        return
            groups
            |> Seq.map (fun sg -> Giraffe.ShortGuid.fromGuid sg.Id.Value, $"{sg.Church.Name} | {sg.Name}")
            |> List.ofSeq
    }

    /// Log on a small group
    member this.TryGroupLogOnByPassword groupId pw = backgroundTask {
        match! this.TryGroupById groupId with
        | Some grp when pw = grp.Preferences.GroupPassword -> return Some grp
        | _ -> return None
    }

    /// Count small groups for the given church Id
    member this.CountGroupsByChurch churchId = backgroundTask {
        return! this.SmallGroups.CountAsync (fun sg -> sg.ChurchId = churchId)
    }
        
    (*-- TIME ZONE EXTENSIONS --*)

    /// Get all time zones
    member this.AllTimeZones () = backgroundTask {
        let! zones = this.TimeZones.OrderBy(fun tz -> tz.SortOrder).ToListAsync ()
        return List.ofSeq zones
    }
    
    (*-- USER EXTENSIONS --*)

    /// Find a user by its Id
    member this.TryUserById userId = backgroundTask {
        let! usr = this.Users.SingleOrDefaultAsync (fun u -> u.Id = userId)
        return Option.fromObject usr
    }

    /// Find a user by its e-mail address and authorized small group
    member this.TryUserByEmailAndGroup email groupId = backgroundTask {
        let! usr =
            this.Users.SingleOrDefaultAsync (fun u ->
                u.Email = email && u.SmallGroups.Any (fun xref -> xref.SmallGroupId = groupId))
        return Option.fromObject usr
    }
    
    /// Find a user by its Id, eagerly loading the user's groups
    member this.TryUserByIdWithGroups userId = backgroundTask {
        let! usr = this.Users.Include(fun u -> u.SmallGroups).SingleOrDefaultAsync (fun u -> u.Id = userId)
        return Option.fromObject usr
    }
    
    /// Get a list of all users
    member this.AllUsers () = backgroundTask {
        let! users = this.Users.OrderBy(fun u -> u.LastName).ThenBy(fun u -> u.FirstName).ToListAsync ()
        return List.ofSeq users
    }

    /// Get all PrayerTracker users as members (used to send e-mails)
    member this.AllUsersAsMembers () = backgroundTask {
        let! users = this.AllUsers ()
        return users |> List.map (fun u -> { Member.empty with Email = u.Email; Name = u.Name })
    }

    /// Count the number of users for a small group
    member this.CountUsersBySmallGroup groupId = backgroundTask {
        return! this.Users.CountAsync (fun u -> u.SmallGroups.Any (fun xref -> xref.SmallGroupId = groupId))
    }

    /// Count the number of users for a church
    member this.CountUsersByChurch churchId = backgroundTask {
        return! this.Users.CountAsync (fun u -> u.SmallGroups.Any (fun xref -> xref.SmallGroup.ChurchId = churchId))
    }
