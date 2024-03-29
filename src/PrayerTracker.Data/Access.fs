namespace PrayerTracker.Data

open NodaTime
open Npgsql
open Npgsql.FSharp
open PrayerTracker.Entities

/// Helper functions for the PostgreSQL data implementation
[<AutoOpen>]
module private Helpers =
    
    /// Map a row to a Church instance
    let mapToChurch (row : RowReader) =
        {   Id               = ChurchId         (row.uuid "id")
            Name             = row.string       "church_name"
            City             = row.string       "city"
            State            = row.string       "state"
            HasVpsInterface  = row.bool         "has_vps_interface"
            InterfaceAddress = row.stringOrNone "interface_address"
        }
    
    /// Map a row to a ListPreferences instance
    let mapToListPreferences (row : RowReader) =
        {   SmallGroupId        = SmallGroupId (row.uuid "small_group_id")
            DaysToKeepNew       = row.int      "days_to_keep_new"
            DaysToExpire        = row.int      "days_to_expire"
            LongTermUpdateWeeks = row.int      "long_term_update_weeks"
            EmailFromName       = row.string   "email_from_name"
            EmailFromAddress    = row.string   "email_from_address"
            Fonts               = row.string   "fonts"
            HeadingColor        = row.string   "heading_color"
            LineColor           = row.string   "line_color"
            HeadingFontSize     = row.int      "heading_font_size"
            TextFontSize        = row.int      "text_font_size"
            GroupPassword       = row.string   "group_password"
            IsPublic            = row.bool     "is_public"
            PageSize            = row.int      "page_size"
            TimeZoneId          = TimeZoneId    (row.string "time_zone_id")
            RequestSort         = RequestSort.fromCode     (row.string "request_sort")
            DefaultEmailType    = EmailFormat.fromCode     (row.string "default_email_type")
            AsOfDateDisplay     = AsOfDateDisplay.fromCode (row.string "as_of_date_display")
        }
    
    /// Map a row to a Member instance
    let mapToMember (row : RowReader) =
        {   Id           = MemberId         (row.uuid "id")
            SmallGroupId = SmallGroupId     (row.uuid "small_group_id")
            Name         = row.string       "member_name"
            Email        = row.string       "email"
            Format       = row.stringOrNone "email_format" |> Option.map EmailFormat.fromCode
        }
    
    /// Map a row to a Prayer Request instance
    let mapToPrayerRequest (row : RowReader) =
        {   Id             = PrayerRequestId         (row.uuid "id")
            UserId         = UserId                  (row.uuid "user_id")
            SmallGroupId   = SmallGroupId            (row.uuid "small_group_id")
            EnteredDate    = row.fieldValue<Instant> "entered_date"
            UpdatedDate    = row.fieldValue<Instant> "updated_date"
            Requestor      = row.stringOrNone        "requestor"
            Text           = row.string              "request_text"
            NotifyChaplain = row.bool                "notify_chaplain"
            RequestType    = PrayerRequestType.fromCode (row.string "request_type")
            Expiration     = Expiration.fromCode        (row.string "expiration")
        }
    
    /// Map a row to a Small Group instance
    let mapToSmallGroup (row : RowReader) =
        {   Id          = SmallGroupId (row.uuid "id")
            ChurchId    = ChurchId     (row.uuid "church_id")
            Name        = row.string   "group_name"
            Preferences = ListPreferences.empty
        }
    
    /// Map a row to a Small Group information set
    let mapToSmallGroupInfo (row : RowReader) =
        {   Id         = Giraffe.ShortGuid.fromGuid (row.uuid "id")
            Name       = row.string "group_name"
            ChurchName = row.string "church_name"
            TimeZoneId = TimeZoneId (row.string "time_zone_id")
            IsPublic   = row.bool   "is_public"
        }
    
    /// Map a row to a Small Group list item
    let mapToSmallGroupItem (row : RowReader) =
        Giraffe.ShortGuid.fromGuid (row.uuid "id"), $"""{row.string "church_name"} | {row.string "group_name"}"""
    
    /// Map a row to a Small Group instance with populated list preferences    
    let mapToSmallGroupWithPreferences (row : RowReader) =
        { mapToSmallGroup row with
            Preferences = mapToListPreferences row
        }
    
    /// Map a row to a User instance
    let mapToUser (row : RowReader) =
        {   Id           = UserId     (row.uuid "id")
            FirstName    = row.string "first_name"
            LastName     = row.string "last_name"
            Email        = row.string "email"
            IsAdmin      = row.bool   "is_admin"
            PasswordHash = row.string "password_hash"
            LastSeen     = row.fieldValueOrNone<Instant> "last_seen"
        }


open BitBadger.Npgsql.FSharp.Documents

/// Functions to manipulate churches
module Churches =
    
    /// Get a list of all churches
    let all () =
        Custom.list "SELECT * FROM pt.church ORDER BY church_name" [] mapToChurch
    
    /// Delete a church by its ID
    let deleteById (churchId : ChurchId) = backgroundTask {
        let idParam = [ [ "@churchId", Sql.uuid churchId.Value ] ]
        let where   = "WHERE small_group_id IN (SELECT id FROM pt.small_group WHERE church_id = @churchId)"
        let! _ =
            Configuration.dataSource ()
            |> Sql.fromDataSource
            |> Sql.executeTransactionAsync
                [   $"DELETE FROM pt.prayer_request {where}", idParam
                    $"DELETE FROM pt.user_small_group {where}", idParam
                    $"DELETE FROM pt.list_preference {where}", idParam
                    "DELETE FROM pt.small_group WHERE church_id = @churchId", idParam
                    "DELETE FROM pt.church WHERE id = @churchId", idParam ]
        ()
    }
    
    /// Save a church's information
    let save (church : Church) =
        Custom.nonQuery
            "INSERT INTO pt.church (
                id, church_name, city, state, has_vps_interface, interface_address
            ) VALUES (
                @id, @name, @city, @state, @hasVpsInterface, @interfaceAddress
            ) ON CONFLICT (id) DO UPDATE
            SET church_name       = EXCLUDED.church_name,
                city              = EXCLUDED.city,
                state             = EXCLUDED.state,
                has_vps_interface = EXCLUDED.has_vps_interface,
                interface_address = EXCLUDED.interface_address"
            [   "@id",               Sql.uuid         church.Id.Value
                "@name",             Sql.string       church.Name
                "@city",             Sql.string       church.City
                "@state",            Sql.string       church.State
                "@hasVpsInterface",  Sql.bool         church.HasVpsInterface
                "@interfaceAddress", Sql.stringOrNone church.InterfaceAddress ]
    
    /// Find a church by its ID
    let tryById (churchId : ChurchId) =
        Custom.single "SELECT * FROM pt.church WHERE id = @id" [ "@id", Sql.uuid churchId.Value ] mapToChurch


/// Functions to manipulate small group members
module Members =
    
    /// Count members for the given small group
    let countByGroup (groupId : SmallGroupId) =
        Custom.scalar "SELECT COUNT(id) AS mbr_count FROM pt.member WHERE small_group_id = @groupId"
                      [ "@groupId", Sql.uuid groupId.Value ] (fun row -> row.int "mbr_count")
    
    /// Delete a small group member by its ID
    let deleteById (memberId : MemberId) =
        Custom.nonQuery "DELETE FROM pt.member WHERE id = @id" [ "@id", Sql.uuid memberId.Value ]
    
    /// Retrieve all members for a given small group
    let forGroup (groupId : SmallGroupId) =
        Custom.list "SELECT * FROM pt.member WHERE small_group_id = @groupId ORDER BY member_name"
                    [ "@groupId", Sql.uuid groupId.Value ] mapToMember
    
    /// Save a small group member
    let save (mbr : Member) =
        Custom.nonQuery
            "INSERT INTO pt.member (
                id, small_group_id, member_name, email, email_format
            ) VALUES (
                @id, @groupId, @name, @email, @format
            ) ON CONFLICT (id) DO UPDATE
            SET member_name  = EXCLUDED.member_name,
                email        = EXCLUDED.email,
                email_format = EXCLUDED.email_format"
            [   "@id",      Sql.uuid         mbr.Id.Value
                "@groupId", Sql.uuid         mbr.SmallGroupId.Value
                "@name",    Sql.string       mbr.Name
                "@email",   Sql.string       mbr.Email
                "@format",  Sql.stringOrNone (mbr.Format |> Option.map EmailFormat.toCode) ]
    
    /// Retrieve a small group member by its ID
    let tryById (memberId : MemberId) =
        Custom.single "SELECT * FROM pt.member WHERE id = @id" [ "@id", Sql.uuid memberId.Value ] mapToMember


/// Options to retrieve a list of requests
type PrayerRequestOptions =
    {   /// The small group for which requests should be retrieved
        SmallGroup : SmallGroup
        
        /// The clock instance to use for date/time manipulation
        Clock : IClock
        
        /// The date for which the list is being retrieved
        ListDate : LocalDate option
        
        /// Whether only active requests should be retrieved
        ActiveOnly : bool
        
        /// The page number, for paged lists
        PageNumber : int
    }


/// Functions to manipulate prayer requests
module PrayerRequests =
    
    /// Central place to append sort criteria for prayer request queries
    let private orderBy sort =
        match sort with
        | SortByDate -> "updated_date DESC, entered_date DESC, requestor"
        | SortByRequestor -> "requestor, updated_date DESC, entered_date DESC"
    
    /// Paginate a prayer request query
    let private paginate (pageNbr : int) pageSize =
        if pageNbr > 0 then $"LIMIT {pageSize} OFFSET {(pageNbr - 1) * pageSize}" else ""
    
    /// Count the number of prayer requests for a church
    let countByChurch (churchId : ChurchId) =
        Custom.scalar
            "SELECT COUNT(id) AS req_count
               FROM pt.prayer_request
              WHERE small_group_id IN (SELECT id FROM pt.small_group WHERE church_id = @churchId)"
            [ "@churchId", Sql.uuid churchId.Value ] (fun row -> row.int "req_count")
    
    /// Count the number of prayer requests for a small group
    let countByGroup (groupId : SmallGroupId) =
        Custom.scalar "SELECT COUNT(id) AS req_count FROM pt.prayer_request WHERE small_group_id = @groupId"
                      [ "@groupId", Sql.uuid groupId.Value ] (fun row -> row.int "req_count")
    
    /// Delete a prayer request by its ID
    let deleteById (reqId : PrayerRequestId) =
        Custom.nonQuery "DELETE FROM pt.prayer_request WHERE id = @id" [ "@id", Sql.uuid reqId.Value ]
    
    /// Get all (or active) requests for a small group as of now or the specified date
    let forGroup (opts : PrayerRequestOptions) =
        let theDate = defaultArg opts.ListDate (SmallGroup.localDateNow opts.Clock opts.SmallGroup)
        let where, parameters =
            if opts.ActiveOnly then
                let asOf = NpgsqlParameter (
                    "@asOf",
                    (theDate.AtStartOfDayInZone(SmallGroup.timeZone opts.SmallGroup)
                            - Duration.FromDays opts.SmallGroup.Preferences.DaysToExpire)
                        .ToInstant ())
                "   AND (   updated_date > @asOf
                         OR expiration   = @manual
                         OR request_type = @longTerm
                         OR request_type = @expecting)
                    AND expiration <> @forced",
                [   "@asOf",      Sql.parameter asOf
                    "@manual",    Sql.string    (Expiration.toCode Manual)
                    "@longTerm",  Sql.string    (PrayerRequestType.toCode LongTermRequest)
                    "@expecting", Sql.string    (PrayerRequestType.toCode Expecting)
                    "@forced",    Sql.string    (Expiration.toCode Forced) ]
            else "", []
        Custom.list
            $"SELECT *
                FROM pt.prayer_request
               WHERE small_group_id = @groupId {where}
               ORDER BY {orderBy opts.SmallGroup.Preferences.RequestSort}
               {paginate opts.PageNumber opts.SmallGroup.Preferences.PageSize}"
            (("@groupId", Sql.uuid opts.SmallGroup.Id.Value) :: parameters) mapToPrayerRequest
    
    /// Save a prayer request
    let save (req : PrayerRequest) =
        Custom.nonQuery
            "INSERT into pt.prayer_request (
                id, request_type, user_id, small_group_id, entered_date, updated_date, requestor, request_text,
                notify_chaplain, expiration
            ) VALUES (
                @id, @type, @userId, @groupId, @entered, @updated, @requestor, @text,
                @notifyChaplain, @expiration
            ) ON CONFLICT (id) DO UPDATE
            SET request_type    = EXCLUDED.request_type,
                updated_date    = EXCLUDED.updated_date,
                requestor       = EXCLUDED.requestor,
                request_text    = EXCLUDED.request_text,
                notify_chaplain = EXCLUDED.notify_chaplain,
                expiration      = EXCLUDED.expiration"
            [   "@id",             Sql.uuid         req.Id.Value
                "@type",           Sql.string       (PrayerRequestType.toCode req.RequestType)
                "@userId",         Sql.uuid         req.UserId.Value
                "@groupId",        Sql.uuid         req.SmallGroupId.Value
                "@entered",        Sql.parameter    (NpgsqlParameter ("@entered", req.EnteredDate))
                "@updated",        Sql.parameter    (NpgsqlParameter ("@updated", req.UpdatedDate))
                "@requestor",      Sql.stringOrNone req.Requestor
                "@text",           Sql.string       req.Text
                "@notifyChaplain", Sql.bool         req.NotifyChaplain
                "@expiration",     Sql.string       (Expiration.toCode req.Expiration) ]
    
    /// Search prayer requests for the given term
    let searchForGroup group searchTerm pageNbr =
        Custom.list 
            $"SELECT * FROM pt.prayer_request WHERE small_group_id = @groupId AND request_text ILIKE @search
                  UNION
              SELECT * FROM pt.prayer_request WHERE small_group_id = @groupId AND COALESCE(requestor, '') ILIKE @search
              ORDER BY {orderBy group.Preferences.RequestSort}
              {paginate pageNbr group.Preferences.PageSize}"
            [ "@groupId", Sql.uuid group.Id.Value; "@search", Sql.string $"%%%s{searchTerm}%%" ] mapToPrayerRequest

    /// Retrieve a prayer request by its ID
    let tryById (reqId : PrayerRequestId) =
        Custom.single "SELECT * FROM pt.prayer_request WHERE id = @id" [ "@id", Sql.uuid reqId.Value ]
                      mapToPrayerRequest
    
    /// Update the expiration for the given prayer request
    let updateExpiration (req : PrayerRequest) withTime =
        let sql, parameters =
            if withTime then
                ", updated_date = @updated",
                [ "@updated", Sql.parameter (NpgsqlParameter ("@updated", req.UpdatedDate)) ]
            else "", []
        Custom.nonQuery $"UPDATE pt.prayer_request SET expiration = @expiration{sql} WHERE id = @id"
                        ([  "@expiration", Sql.string (Expiration.toCode req.Expiration)
                            "@id",         Sql.uuid   req.Id.Value ]
                        |> List.append parameters)


/// Functions to retrieve small group information
module SmallGroups =
    
    /// Count the number of small groups for a church
    let countByChurch (churchId : ChurchId) =
        Custom.scalar "SELECT COUNT(id) AS group_count FROM pt.small_group WHERE church_id = @churchId"
                      [ "@churchId", Sql.uuid churchId.Value ] (fun row -> row.int "group_count")
    
    /// Delete a small group by its ID
    let deleteById (groupId : SmallGroupId) = backgroundTask {
        let idParam = [ [ "@groupId", Sql.uuid groupId.Value ] ]
        let! _ =
            Configuration.dataSource ()
            |> Sql.fromDataSource
            |> Sql.executeTransactionAsync
                [   "DELETE FROM pt.prayer_request   WHERE small_group_id = @groupId", idParam
                    "DELETE FROM pt.user_small_group WHERE small_group_id = @groupId", idParam
                    "DELETE FROM pt.list_preference  WHERE small_group_id = @groupId", idParam
                    "DELETE FROM pt.small_group      WHERE id             = @groupId", idParam ]
        ()
    }
    
    /// Get information for all small groups
    let infoForAll () =
        Custom.list
            "SELECT sg.id, sg.group_name, c.church_name, lp.time_zone_id, lp.is_public
               FROM pt.small_group sg
                    INNER JOIN pt.church c ON c.id = sg.church_id
                    INNER JOIN pt.list_preference lp ON lp.small_group_id = sg.id
              ORDER BY sg.group_name"
            [] mapToSmallGroupInfo
    
    /// Get a list of small group IDs along with a description that includes the church name
    let listAll () =
        Custom.list
            "SELECT g.group_name, g.id, c.church_name
               FROM pt.small_group g
                    INNER JOIN pt.church c ON c.id = g.church_id
              ORDER BY c.church_name, g.group_name"
            [] mapToSmallGroupItem
    
    /// Get a list of small group IDs and descriptions for groups with a group password
    let listProtected () =
        Custom.list
            "SELECT g.group_name, g.id, c.church_name, lp.is_public
               FROM pt.small_group g
                    INNER JOIN pt.church           c ON c.id = g.church_id
                    INNER JOIN pt.list_preference lp ON lp.small_group_id = g.id
              WHERE COALESCE(lp.group_password, '') <> ''
              ORDER BY c.church_name, g.group_name"
            [] mapToSmallGroupItem
    
    /// Get a list of small group IDs and descriptions for groups that are public or have a group password
    let listPublicAndProtected () =
        Custom.list
            "SELECT g.group_name, g.id, c.church_name, lp.time_zone_id, lp.is_public
               FROM pt.small_group g
                    INNER JOIN pt.church           c ON c.id = g.church_id
                    INNER JOIN pt.list_preference lp ON lp.small_group_id = g.id
              WHERE lp.is_public = TRUE
                 OR COALESCE(lp.group_password, '') <> ''
              ORDER BY c.church_name, g.group_name"
            [] mapToSmallGroupInfo
    
    /// Log on for a small group (includes list preferences)
    let logOn (groupId : SmallGroupId) password =
        Custom.single
            "SELECT sg.*, lp.*
               FROM pt.small_group sg
                    INNER JOIN pt.list_preference lp ON lp.small_group_id = sg.id
              WHERE sg.id             = @id
                AND lp.group_password = @password"
            [ "@id", Sql.uuid groupId.Value; "@password", Sql.string password ] mapToSmallGroupWithPreferences
    
    /// Save a small group
    let save (group : SmallGroup) isNew = backgroundTask {
        let! _ =
            Configuration.dataSource ()
            |> Sql.fromDataSource
            |> Sql.executeTransactionAsync [
                "INSERT INTO pt.small_group (
                        id, church_id, group_name
                ) VALUES (
                    @id, @churchId, @name
                ) ON CONFLICT (id) DO UPDATE
                SET church_id  = EXCLUDED.church_id,
                    group_name = EXCLUDED.group_name",
                [ [ "@id",       Sql.uuid   group.Id.Value
                    "@churchId", Sql.uuid   group.ChurchId.Value
                    "@name",     Sql.string group.Name ] ]
                if isNew then
                    "INSERT INTO pt.list_preference (small_group_id) VALUES (@id)",
                    [ [ "@id", Sql.uuid group.Id.Value ] ]
            ]
        ()
    }
    
    /// Save a small group's list preferences
    let savePreferences (pref : ListPreferences) =
        Custom.nonQuery
            "UPDATE pt.list_preference
                SET days_to_keep_new       = @daysToKeepNew,
                    days_to_expire         = @daysToExpire,
                    long_term_update_weeks = @longTermUpdateWeeks,
                    email_from_name        = @emailFromName,
                    email_from_address     = @emailFromAddress,
                    fonts                  = @fonts,
                    heading_color          = @headingColor,
                    line_color             = @lineColor,
                    heading_font_size      = @headingFontSize,
                    text_font_size         = @textFontSize,
                    request_sort           = @requestSort,
                    group_password         = @groupPassword,
                    default_email_type     = @defaultEmailType,
                    is_public              = @isPublic,
                    time_zone_id           = @timeZoneId,
                    page_size              = @pageSize,
                    as_of_date_display     = @asOfDateDisplay
              WHERE small_group_id = @groupId"
            [   "@groupId",             Sql.uuid   pref.SmallGroupId.Value
                "@daysToKeepNew",       Sql.int    pref.DaysToKeepNew
                "@daysToExpire",        Sql.int    pref.DaysToExpire
                "@longTermUpdateWeeks", Sql.int    pref.LongTermUpdateWeeks
                "@emailFromName",       Sql.string pref.EmailFromName
                "@emailFromAddress",    Sql.string pref.EmailFromAddress
                "@fonts",               Sql.string pref.Fonts
                "@headingColor",        Sql.string pref.HeadingColor
                "@lineColor",           Sql.string pref.LineColor
                "@headingFontSize",     Sql.int    pref.HeadingFontSize
                "@textFontSize",        Sql.int    pref.TextFontSize
                "@requestSort",         Sql.string (RequestSort.toCode pref.RequestSort)
                "@groupPassword",       Sql.string pref.GroupPassword
                "@defaultEmailType",    Sql.string (EmailFormat.toCode pref.DefaultEmailType)
                "@isPublic",            Sql.bool   pref.IsPublic
                "@timeZoneId",          Sql.string (TimeZoneId.toString pref.TimeZoneId)
                "@pageSize",            Sql.int    pref.PageSize
                "@asOfDateDisplay",     Sql.string (AsOfDateDisplay.toCode pref.AsOfDateDisplay) ]
    
    /// Get a small group by its ID
    let tryById (groupId : SmallGroupId) =
        Custom.single "SELECT * FROM pt.small_group WHERE id = @id" [ "@id", Sql.uuid groupId.Value ] mapToSmallGroup
    
    /// Get a small group by its ID with its list preferences populated
    let tryByIdWithPreferences (groupId : SmallGroupId) =
        Custom.single
            "SELECT sg.*, lp.*
               FROM pt.small_group sg
                    INNER JOIN pt.list_preference lp ON lp.small_group_id = sg.id
              WHERE sg.id = @id"
            [ "@id", Sql.uuid groupId.Value ] mapToSmallGroupWithPreferences


/// Functions to manipulate users
module Users =
    
    /// Retrieve all PrayerTracker users
    let all () =
        Custom.list "SELECT * FROM pt.pt_user ORDER BY last_name, first_name" [] mapToUser
    
    /// Count the number of users for a church
    let countByChurch (churchId : ChurchId) =
        Custom.scalar
            "SELECT COUNT(u.id) AS user_count
               FROM pt.pt_user u
              WHERE EXISTS (
                    SELECT 1
                      FROM pt.user_small_group usg
                           INNER JOIN pt.small_group sg ON sg.id = usg.small_group_id
                     WHERE usg.user_id = u.id
                       AND sg.church_id = @churchId)"
            [ "@churchId", Sql.uuid churchId.Value ] (fun row -> row.int "user_count")
    
    /// Count the number of users for a small group
    let countByGroup (groupId : SmallGroupId) =
        Custom.scalar "SELECT COUNT(user_id) AS user_count FROM pt.user_small_group WHERE small_group_id = @groupId"
                      [ "@groupId", Sql.uuid groupId.Value ] (fun row -> row.int "user_count")
    
    /// Delete a user by its database ID
    let deleteById (userId : UserId) =
        Custom.nonQuery "DELETE FROM pt.pt_user WHERE id = @id" [ "@id", Sql.uuid userId.Value ]
    
    /// Get the IDs of the small groups for which the given user is authorized
    let groupIdsByUserId (userId : UserId) =
        Custom.list "SELECT small_group_id FROM pt.user_small_group WHERE user_id = @id"
                   [ "@id", Sql.uuid userId.Value ] (fun row -> SmallGroupId (row.uuid "small_group_id"))
    
    /// Get a list of users authorized to administer the given small group
    let listByGroupId (groupId : SmallGroupId) =
        Custom.list
            "SELECT u.*
               FROM pt.pt_user u
                    INNER JOIN pt.user_small_group usg ON usg.user_id = u.id
              WHERE usg.small_group_id = @groupId
              ORDER BY u.last_name, u.first_name"
            [ "@groupId", Sql.uuid groupId.Value ] mapToUser
    
    /// Save a user's information
    let save (user : User) = 
        Custom.nonQuery
            "INSERT INTO pt.pt_user (
                id, first_name, last_name, email, is_admin, password_hash
            ) VALUES (
                @id, @firstName, @lastName, @email, @isAdmin, @passwordHash
            ) ON CONFLICT (id) DO UPDATE
            SET first_name    = EXCLUDED.first_name,
                last_name     = EXCLUDED.last_name,
                email         = EXCLUDED.email,
                is_admin      = EXCLUDED.is_admin,
                password_hash = EXCLUDED.password_hash"
            [   "@id",           Sql.uuid   user.Id.Value
                "@firstName",    Sql.string user.FirstName
                "@lastName",     Sql.string user.LastName
                "@email",        Sql.string user.Email
                "@isAdmin",      Sql.bool   user.IsAdmin
                "@passwordHash", Sql.string user.PasswordHash ]
    
    /// Find a user by its e-mail address and authorized small group
    let tryByEmailAndGroup email (groupId : SmallGroupId) =
        Custom.single
            "SELECT u.*
               FROM pt.pt_user u
                    INNER JOIN pt.user_small_group usg ON usg.user_id = u.id AND usg.small_group_id = @groupId
              WHERE u.email = @email"
            [ "@email", Sql.string email; "@groupId", Sql.uuid groupId.Value ] mapToUser
    
    /// Find a user by their database ID
    let tryById (userId : UserId) =
        Custom.single "SELECT * FROM pt.pt_user WHERE id = @id" [ "@id", Sql.uuid userId.Value ] mapToUser
    
    /// Update a user's last seen date/time
    let updateLastSeen (userId : UserId) (now : Instant) =
        Custom.nonQuery "UPDATE pt.pt_user SET last_seen = @now WHERE id = @id"
                        [ "@id", Sql.uuid userId.Value; "@now", Sql.parameter (NpgsqlParameter ("@now", now)) ]
    
    /// Update a user's password hash
    let updatePassword (user : User) =
        Custom.nonQuery "UPDATE pt.pt_user SET password_hash = @passwordHash WHERE id = @id"
                        [ "@id", Sql.uuid user.Id.Value; "@passwordHash", Sql.string user.PasswordHash ]
    
    /// Update a user's authorized small groups
    let updateSmallGroups (userId : UserId) groupIds = backgroundTask {
        let! existingGroupIds = groupIdsByUserId userId
        let toAdd =
            groupIds |> List.filter (fun it -> existingGroupIds |> List.exists (fun grpId -> grpId = it) |> not)
        let toDelete =
            existingGroupIds |> List.filter (fun it -> groupIds |> List.exists (fun grpId -> grpId = it) |> not)
        let queries = seq {
            if not (List.isEmpty toAdd) then
                "INSERT INTO pt.user_small_group VALUES (@userId, @smallGroupId)",
                toAdd |> List.map (fun it -> [ "@userId", Sql.uuid userId.Value; "@smallGroupId", Sql.uuid it.Value ])
            if not (List.isEmpty toDelete) then
                "DELETE FROM pt.user_small_group WHERE user_id = @userId AND small_group_id = @smallGroupId",
                toDelete
                |> List.map (fun it -> [ "@userId", Sql.uuid userId.Value; "@smallGroupId", Sql.uuid it.Value ])
        }
        if not (Seq.isEmpty queries) then
            let! _ =
                Configuration.dataSource ()
                |> Sql.fromDataSource
                |> Sql.executeTransactionAsync (List.ofSeq queries)
            ()
    }
