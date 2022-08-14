[<AutoOpen>]
module PrayerTracker.Extensions

open Microsoft.AspNetCore.Http
open Microsoft.FSharpLu
open Newtonsoft.Json
open NodaTime
open NodaTime.Serialization.JsonNet
open PrayerTracker.Data
open PrayerTracker.Entities
open PrayerTracker.ViewModels

/// JSON.NET serializer settings for NodaTime
let private jsonSettings = JsonSerializerSettings().ConfigureForNodaTime DateTimeZoneProviders.Tzdb

/// Extensions on the .NET session object
type ISession with
    
    /// Set an object in the session
    member this.SetObject key value =
        this.SetString (key, JsonConvert.SerializeObject (value, jsonSettings))
    
    /// Get an object from the session
    member this.GetObject<'T> key =
        match this.GetString key with
        | null -> Unchecked.defaultof<'T>
        | v -> JsonConvert.DeserializeObject<'T> (v, jsonSettings)

    /// The currently logged on small group
    member this.CurrentGroup
      with get () = this.GetObject<SmallGroup> Key.Session.currentGroup |> Option.fromObject
       and set (v : SmallGroup option) = 
          match v with
          | Some group -> this.SetObject Key.Session.currentGroup group
          | None -> this.Remove Key.Session.currentGroup

    /// The currently logged on user
    member this.CurrentUser
      with get () = this.GetObject<User> Key.Session.currentUser |> Option.fromObject
       and set (v : User option) =
          match v with
          | Some user -> this.SetObject Key.Session.currentUser { user with PasswordHash = "" }
          | None -> this.Remove Key.Session.currentUser
    
    /// Current messages for the session
    member this.Messages
      with get () =
          match box (this.GetObject<UserMessage list> Key.Session.userMessages) with
          | null -> List.empty<UserMessage>
          | msgs -> unbox msgs
       and set (v : UserMessage list) = this.SetObject Key.Session.userMessages v


open System.Security.Claims

/// Extensions on the claims principal
type ClaimsPrincipal with
    
    /// The ID of the currently logged on small group    
    member this.SmallGroupId =
        if this.HasClaim (fun c -> c.Type = ClaimTypes.GroupSid) then
            Some (idFromShort SmallGroupId (this.FindFirst(fun c -> c.Type = ClaimTypes.GroupSid).Value))
        else None
    
    /// The ID of the currently signed in user    
    member this.UserId =
        if this.HasClaim (fun c -> c.Type = ClaimTypes.NameIdentifier) then
            Some (idFromShort UserId (this.FindFirst(fun c -> c.Type = ClaimTypes.NameIdentifier).Value))
        else None


open System.Threading.Tasks
open Giraffe
open Microsoft.Extensions.Configuration
open Npgsql

/// Extensions on the ASP.NET Core HTTP context
type HttpContext with
    
    // TODO: is this disposed?
    member private this.LazyConn : Lazy<Task<NpgsqlConnection>> = lazy (backgroundTask {
        let cfg  = this.GetService<IConfiguration> ()
        let conn = new NpgsqlConnection (cfg.GetConnectionString "PrayerTracker")
        do! conn.OpenAsync ()
        return conn
    })
    
    /// The PostgreSQL connection (configured via DI)
    member this.Conn = backgroundTask {
        return! this.LazyConn.Force ()
    }
    
    /// The system clock (via DI)
    member this.Clock = this.GetService<IClock> ()
    
    /// The current instant
    member this.Now = this.Clock.GetCurrentInstant ()
    
    /// The currently logged on small group (sets the value in the session if it is missing)
    member this.CurrentGroup () = task {
        match this.Session.CurrentGroup with
        | Some group -> return Some group
        | None ->
            match this.User.SmallGroupId with
            | Some groupId ->
                let! conn = this.Conn
                match! SmallGroups.tryByIdWithPreferences groupId conn with
                | Some group ->
                    this.Session.CurrentGroup <- Some group
                    return Some group
                | None -> return None
            | None -> return None
    }

    /// The currently logged on user (sets the value in the session if it is missing)
    member this.CurrentUser () = task {
        match this.Session.CurrentUser with
        | Some user -> return Some user
        | None ->
            match this.User.UserId with
            | Some userId ->
                let! conn = this.Conn
                match! Users.tryById userId conn with
                | Some user ->
                    // Set last seen for user
                    do! Users.updateLastSeen userId this.Now conn
                    this.Session.CurrentUser <- Some user
                    return Some user
                | None -> return None
            | None -> return None
    }
