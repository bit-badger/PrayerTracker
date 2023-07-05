[<AutoOpen>]
module PrayerTracker.Extensions

open Microsoft.AspNetCore.Http
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
    member this.SetObject<'T> key (value : 'T) =
        this.SetString (key, JsonConvert.SerializeObject (value, jsonSettings))
    
    /// Get an object from the session
    member this.TryGetObject<'T> key =
        match this.GetString key with
        | null -> None
        | v -> Some (JsonConvert.DeserializeObject<'T> (v, jsonSettings))

    /// The currently logged on small group
    member this.CurrentGroup
      with get () = this.TryGetObject<SmallGroup> Key.Session.currentGroup
       and set (v : SmallGroup option) = 
          match v with
          | Some group -> this.SetObject Key.Session.currentGroup group
          | None -> this.Remove Key.Session.currentGroup

    /// The currently logged on user
    member this.CurrentUser
      with get () = this.TryGetObject<User> Key.Session.currentUser
       and set (v : User option) =
          match v with
          | Some user -> this.SetObject Key.Session.currentUser { user with PasswordHash = "" }
          | None -> this.Remove Key.Session.currentUser
    
    /// Current messages for the session
    member this.Messages
      with get () =
          this.TryGetObject<UserMessage list> Key.Session.userMessages
          |> Option.defaultValue List.empty<UserMessage>
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


open Giraffe
open Npgsql

/// Extensions on the ASP.NET Core HTTP context
type HttpContext with
    
    /// The system clock (via DI)
    member this.Clock = this.GetService<IClock> ()
    
    /// The current instant
    member this.Now = this.Clock.GetCurrentInstant ()
    
    /// The common string localizer
    member _.Strings = Views.I18N.localizer.Force ()
    
    /// The currently logged on small group (sets the value in the session if it is missing)
    member this.CurrentGroup () = task {
        match this.Session.CurrentGroup with
        | Some group -> return Some group
        | None ->
            match this.User.SmallGroupId with
            | Some groupId ->
                match! SmallGroups.tryByIdWithPreferences groupId with
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
                match! Users.tryById userId with
                | Some user ->
                    // Set last seen for user
                    do! Users.updateLastSeen userId this.Now
                    this.Session.CurrentUser <- Some user
                    return Some user
                | None -> return None
            | None -> return None
    }
