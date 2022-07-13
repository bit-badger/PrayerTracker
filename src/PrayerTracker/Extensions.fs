[<AutoOpen>]
module PrayerTracker.Extensions

open Microsoft.AspNetCore.Http
open Microsoft.Extensions.DependencyInjection
open Microsoft.FSharpLu
open Newtonsoft.Json
open PrayerTracker.Entities
open PrayerTracker.ViewModels

// fsharplint:disable MemberNames

type ISession with
    /// Set an object in the session
    member this.SetObject key value =
        this.SetString (key, JsonConvert.SerializeObject value)
    
    /// Get an object from the session
    member this.GetObject<'T> key =
        match this.GetString key with
        | null -> Unchecked.defaultof<'T>
        | v -> JsonConvert.DeserializeObject<'T> v

    /// The current small group for the session
    member this.smallGroup
      with get () = this.GetObject<SmallGroup> Key.Session.currentGroup |> Option.fromObject
       and set (v : SmallGroup option) = 
          match v with
          | Some group -> this.SetObject Key.Session.currentGroup group
          | None -> this.Remove Key.Session.currentGroup

    /// The current user for the session
    member this.user
      with get () = this.GetObject<User> Key.Session.currentUser |> Option.fromObject
       and set (v : User option) =
          match v with
          | Some user -> this.SetObject Key.Session.currentUser user
          | None -> this.Remove Key.Session.currentUser

    /// Current messages for the session
    member this.messages
      with get () =
          match box (this.GetObject<UserMessage list> Key.Session.userMessages) with
          | null -> List.empty<UserMessage>
          | msgs -> unbox msgs
       and set (v : UserMessage list) = this.SetObject Key.Session.userMessages v


type HttpContext with
    /// The EF Core database context (via DI)
    member this.db
      with get () = this.RequestServices.GetRequiredService<AppDbContext> ()
