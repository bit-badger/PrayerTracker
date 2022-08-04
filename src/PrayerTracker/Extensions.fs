[<AutoOpen>]
module PrayerTracker.Extensions

open Microsoft.AspNetCore.Http
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

    /// Current messages for the session
    member this.Messages
      with get () =
          match box (this.GetObject<UserMessage list> Key.Session.userMessages) with
          | null -> List.empty<UserMessage>
          | msgs -> unbox msgs
       and set (v : UserMessage list) = this.SetObject Key.Session.userMessages v


open Giraffe
open Microsoft.FSharpLu

/// Extensions on the ASP.NET Core HTTP context
type HttpContext with
    
    /// The currently logged on small group
    member this.CurrentGroup
      with get () = this.Session.GetObject<SmallGroup> Key.Session.currentGroup |> Option.fromObject
       and set (v : SmallGroup option) = 
          match v with
          | Some group -> this.Session.SetObject Key.Session.currentGroup group
          | None -> this.Session.Remove Key.Session.currentGroup

    /// The currently logged on user
    member this.CurrentUser
      with get () = this.Session.GetObject<User> Key.Session.currentUser |> Option.fromObject
       and set (v : User option) =
          match v with
          | Some user -> this.Session.SetObject Key.Session.currentUser user
          | None -> this.Session.Remove Key.Session.currentUser

    /// The EF Core database context (via DI)
    member this.Db = this.GetService<AppDbContext> ()
