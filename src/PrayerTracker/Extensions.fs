[<AutoOpen>]
module PrayerTracker.Extensions

open System
open Microsoft.AspNetCore.Http
open Microsoft.FSharpLu
open Newtonsoft.Json
open PrayerTracker.Entities
open PrayerTracker.ViewModels

/// Extensions on the .NET session object
type ISession with
    
    /// Set an object in the session
    member this.SetObject key value =
        this.SetString (key, JsonConvert.SerializeObject value)
    
    /// Get an object from the session
    member this.GetObject<'T> key =
        match this.GetString key with null -> Unchecked.defaultof<'T> | v -> JsonConvert.DeserializeObject<'T> v

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
          | Some user ->
              this.SetObject Key.Session.currentUser { user with PasswordHash = ""; SmallGroups = ResizeArray() }
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


open Giraffe
open NodaTime
open PrayerTracker

/// Extensions on the ASP.NET Core HTTP context
type HttpContext with
    
    /// The EF Core database context (via DI)
    member this.Db = this.GetService<AppDbContext> ()
    
    /// The system clock (via DI)
    member this.Clock = this.GetService<IClock> ()
    
    /// The currently logged on small group (sets the value in the session if it is missing)
    member this.CurrentGroup () = task {
        match this.Session.CurrentGroup with
        | Some group -> return Some group
        | None ->
            match this.User.SmallGroupId with
            | Some groupId ->
                match! this.Db.TryGroupById groupId with
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
                match! this.Db.TryUserById userId with
                | Some user ->
                    // Set last seen for user
                    this.Db.UpdateEntry { user with LastSeen = Some DateTime.UtcNow }
                    let! _ = this.Db.SaveChangesAsync ()
                    this.Session.CurrentUser <- Some user
                    return Some user
                | None -> return None
            | None -> return None
    }
