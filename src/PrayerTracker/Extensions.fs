[<AutoOpen>]
module PrayerTracker.Extensions

open Microsoft.AspNetCore.Http
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

  member this.GetSmallGroup () =
    this.GetObject<SmallGroup> Key.Session.currentGroup |> Option.fromObject
  member this.SetSmallGroup (group : SmallGroup option) =
    match group with
    | Some g -> this.SetObject Key.Session.currentGroup g
    | None -> this.Remove Key.Session.currentGroup

  member this.GetUser () =
    this.GetObject<User> Key.Session.currentUser |> Option.fromObject
  member this.SetUser (user: User option) =
    match user with
    | Some u -> this.SetObject Key.Session.currentUser u
    | None -> this.Remove Key.Session.currentUser

  member this.GetMessages () =
    match box (this.GetObject<UserMessage list> Key.Session.userMessages) with
    | null -> List.empty<UserMessage>
    | msgs -> unbox msgs
  member this.SetMessages (messages : UserMessage list) =
    this.SetObject Key.Session.userMessages messages


type HttpContext with
  /// Get the EF database context from DI
  member this.dbContext () : AppDbContext = downcast this.RequestServices.GetService typeof<AppDbContext>
