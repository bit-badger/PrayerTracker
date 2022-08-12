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
            Name             = row.string       "name"
            City             = row.string       "city"
            State            = row.string       "state"
            HasVpsInterface  = row.bool         "has_vps_interface"
            InterfaceAddress = row.stringOrNone "interface_address"
        }
    
    /// Map a row to a ListPreferences instance
    let mapToListPreferences (row : RowReader) =
        {   SmallGroupId = SmallGroupId (row.uuid "small_group_id")
            DaysToKeepNew = row.int "days_to_keep_new"
            DaysToExpire  = row.int "days_to_expire"
            LongTermUpdateWeeks = row.int "long_term_update_weeks"
            EmailFromName       = row.string "email_from_name"
            EmailFromAddress    = row.string "email_from_address"
            Fonts               = row.string "fonts"
            HeadingColor        = row.string "heading_color"
            LineColor           = row.string "line_color"
            HeadingFontSize     = row.int    "heading_font_size"
            TextFontSize        = row.int    "text_font_size"
            RequestSort         = RequestSort.fromCode (row.string "request_sort")
            GroupPassword       = row.string "group_password"
            DefaultEmailType    = EmailFormat.fromCode (row.string "default_email_type")
            IsPublic            = row.bool "is_public"
            TimeZoneId          = TimeZoneId (row.string "time_zone_id")
            PageSize            = row.int "page_size"
            AsOfDateDisplay     = AsOfDateDisplay.fromCode (row.string "as_of_date_display")
            TimeZone = TimeZone.empty
        }
    
    /// Map a row to a Small Group instance
    let mapToSmallGroup (row : RowReader) =
        {   Id          = SmallGroupId (row.uuid "id")
            ChurchId    = ChurchId     (row.uuid "church_id")
            Name        = row.string   "group_name"
            Preferences = ListPreferences.empty
            Church = Church.empty
            Members = ResizeArray ()
            PrayerRequests = ResizeArray ()
            Users = ResizeArray ()
        }
    
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
            Salt         = None
            LastSeen     = row.fieldValueOrNone<Instant> "last_seen"
            SmallGroups = ResizeArray ()
        }


module Churches =
    let tryById (churchId : ChurchId) conn = backgroundTask {
        let! church =
            conn
            |> Sql.existingConnection
            |> Sql.query "SELECT * FROM pt.church WHERE id = @id"
            |> Sql.parameters [ "@id", Sql.uuid churchId.Value ]
            |> Sql.executeAsync mapToChurch
        return List.tryHead church
    }


/// Functions to retrieve small group information
module SmallGroups =
    
    /// Get a list of small group IDs along with a description that includes the church name
    let listAll conn =
        conn
        |> Sql.existingConnection
        |> Sql.query """
            SELECT g.group_name, g.id, c.church_name
              FROM pt.small_group g
                   INNER JOIN pt.church c ON c.id = g.church_id
             ORDER BY c.church_name, g.group_name"""
        |> Sql.executeAsync (fun row ->
            Giraffe.ShortGuid.fromGuid (row.uuid "id"), $"""{row.string "church_name"} | {row.string "group_name"}""")
    
    let tryByIdWithPreferences (groupId : SmallGroupId) conn = backgroundTask {
        let! group =
            conn
            |> Sql.existingConnection
            |> Sql.query """
                SELECT sg.*, lp.*
                  FROM pt.small_group sg
                       INNER JOIN list_preference lp ON lp.small_group_id = sg.id
                 WHERE sg.id = @id"""
            |> Sql.parameters [ "@id", Sql.uuid groupId.Value ]
            |> Sql.executeAsync mapToSmallGroupWithPreferences
        return List.tryHead group
    }


/// Functions to manipulate users
module Users =
    
    /// Retrieve all PrayerTracker users
    let all conn =
        conn
        |> Sql.existingConnection
        |> Sql.query "SELECT * FROM pt.pt_user ORDER BY last_name, first_name"
        |> Sql.executeAsync mapToUser
    
    /// Delete a user by its database ID
    let deleteById (userId : UserId) conn = backgroundTask {
        let! _ =
            conn
            |> Sql.existingConnection
            |> Sql.query "DELETE FROM pt.pt_user WHERE id = @id"
            |> Sql.parameters [ "@id", Sql.uuid userId.Value ]
            |> Sql.executeNonQueryAsync
        return ()
    }
    
    /// Save a user's information
    let save user conn = backgroundTask {
        let! _ =
            conn
            |> Sql.existingConnection
            |> Sql.query """
                INSERT INTO pt.pt_user (
                    id, first_name, last_name, email, is_admin, password_hash
                ) VALUES (
                    @id, @firstName, @lastName, @email, @isAdmin, @passwordHash
                ) ON CONFLICT (id) DO UPDATE
                SET first_name    = EXCLUDED.first_name,
                    last_name     = EXCLUDED.last_name,
                    email         = EXCLUDED.email,
                    is_admin      = EXCLUDED.is_admin,
                    password_hash = EXCLUDED.password_hash"""
            |> Sql.parameters
                [   "@id",           Sql.uuid   user.Id.Value
                    "@firstName",    Sql.string user.FirstName
                    "@lastName",     Sql.string user.LastName
                    "@email",        Sql.string user.Email
                    "@isAdmin",      Sql.bool   user.IsAdmin
                    "@passwordHash", Sql.string user.PasswordHash
                ]
            |> Sql.executeNonQueryAsync
        return ()
    }
    
    /// Find a user by its e-mail address and authorized small group
    let tryByEmailAndGroup email (groupId : SmallGroupId) conn = backgroundTask {
        let! user =
            conn
            |> Sql.existingConnection
            |> Sql.query """
                SELECT u.*
                  FROM pt.pt_user u
                       INNER JOIN pt.user_small_group usg ON usg.user_id = u.id AND usg.small_group_id = @groupId
                 WHERE u.email = @email"""
            |> Sql.parameters [ "@email", Sql.string email; "@groupId", Sql.uuid groupId.Value ]
            |> Sql.executeAsync mapToUser
        return List.tryHead user
    }
    
    /// Find a user by their database ID
    let tryById (userId : UserId) conn = backgroundTask {
        let! user =
            conn
            |> Sql.existingConnection
            |> Sql.query "SELECT * FROM pt.pt_user WHERE id = @id"
            |> Sql.parameters [ "@id", Sql.uuid userId.Value ]
            |> Sql.executeAsync mapToUser
        return List.tryHead user
    }
    
    /// Update a user's last seen date/time
    let updateLastSeen (userId : UserId) (now : Instant) conn = backgroundTask {
        let! _ =
            conn
            |> Sql.existingConnection
            |> Sql.query "UPDATE pt.pt_user SET last_seen = @now WHERE id = @id"
            |> Sql.parameters [ "@id", Sql.uuid userId.Value; "@now", Sql.parameter (NpgsqlParameter ("@now", now)) ]
            |> Sql.executeNonQueryAsync
        return ()            
    }
    
    /// Update a user's password hash
    let updatePassword user conn = backgroundTask {
        let! _ =
            conn
            |> Sql.existingConnection
            |> Sql.query "UPDATE pt.pt_user SET password_hash = @passwordHash WHERE id = @id"
            |> Sql.parameters [ "@id", Sql.uuid user.Id.Value; "@passwordHash", Sql.string user.PasswordHash ]
            |> Sql.executeNonQueryAsync
        return ()
    }
