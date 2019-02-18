[<AutoOpen>]
module PrayerTracker.Views.CommonFunctions

open Giraffe
open Giraffe.GiraffeViewEngine
open Microsoft.AspNetCore.Antiforgery
open Microsoft.AspNetCore.Http
open Microsoft.AspNetCore.Mvc.Localization
open Microsoft.Extensions.Localization
open System.IO
open System.Text.Encodings.Web

/// Encoded text for a localized string
let encLocText (text : LocalizedString) = encodedText text.Value

/// Raw text for a localized HTML string
let rawLocText (writer : StringWriter) (text : LocalizedHtmlString) =
  text.WriteTo (writer, HtmlEncoder.Default)
  let txt = string writer
  writer.GetStringBuilder().Clear () |> ignore
  rawText txt

/// A space (used for back-to-back localization string breaks)
let space = rawText " "

/// Generate a Material Design icon
let icon name = i [ _class "material-icons" ] [ rawText name ]

/// Generate a Material Design icon, specifying the point size (must be defined in CSS)
let iconSized size name = i [ _class (sprintf "material-icons md-%i" size) ] [ rawText name ]

/// Generate a CSRF prevention token
let csrfToken (ctx : HttpContext) =
  let antiForgery = ctx.GetService<IAntiforgery> ()
  let tokenSet = antiForgery.GetAndStoreTokens ctx
  input [ _type "hidden"; _name tokenSet.FormFieldName; _value tokenSet.RequestToken ]

/// Create a summary for a table of items
let tableSummary itemCount (s : IStringLocalizer) =
  div [ _class "pt-center-text" ] [
    small [] [
      match itemCount with
      | 0 -> s.["No Entries to Display"]
      | 1 -> s.["Displaying {0} Entry", itemCount]
      | _ -> s.["Displaying {0} Entries", itemCount]
      |> encLocText
      ]
    ]
     
/// Generate a list of named HTML colors
let namedColorList name selected attrs (s : IStringLocalizer) =
  /// The list of HTML named colors (name, display, text color)
  seq {
    yield ("aqua",    s.["Aqua"],    "black")
    yield ("black",   s.["Black"],   "white")
    yield ("blue",    s.["Blue"],    "white")
    yield ("fuchsia", s.["Fuchsia"], "black")
    yield ("gray",    s.["Gray"],    "white")
    yield ("green",   s.["Green"],   "white")
    yield ("lime",    s.["Lime"],    "black")
    yield ("maroon",  s.["Maroon"],  "white")
    yield ("navy",    s.["Navy"],    "white")
    yield ("olive",   s.["Olive"],   "white")
    yield ("purple",  s.["Purple"],  "white")
    yield ("red",     s.["Red"],     "black")
    yield ("silver",  s.["Silver"],  "black")
    yield ("teal",    s.["Teal"],    "white")
    yield ("white",   s.["White"],   "black")
    yield ("yellow",  s.["Yellow"],  "black")
    }
  |> Seq.map (fun color ->
      let (colorName, dispText, txtColor) = color
      option [ yield _value colorName
               yield _style (sprintf "background-color:%s;color:%s;" colorName txtColor)
               match colorName = selected with true -> yield _selected | false -> () ] [
        encodedText (dispText.Value.ToLower ())
        ])
  |> List.ofSeq
  |> select (_name name :: attrs)

/// Generate an input[type=radio] that is selected if its value is the current value
let radio name domId value current =
  input [ yield _type "radio"
          yield _name name
          yield _id domId
          yield _value value
          match value = current with true -> yield _checked | false -> () ]

/// Generate a select list with the current value selected
let selectList name selected attrs items =
  items
  |> Seq.map (fun (value, text) ->
      option [ yield _value value
               match value = selected with true -> yield _selected | false -> () ] [ encodedText text ])
  |> List.ofSeq
  |> select (List.concat [ [ _name name; _id name ]; attrs ])

/// Generate the text for a default entry at the top of a select list
let selectDefault text = sprintf "— %s —" text

/// Generate a standard submit button with icon and text
let submit attrs ico text = button (_type "submit" :: attrs) [ icon ico; rawText " &nbsp;"; encLocText text ]

/// An empty GUID string (used for "add" actions)
let emptyGuid = System.Guid.Empty.ToString "N"


/// blockquote tag
let blockquote = tag "blockquote"

/// role attribute
let _role = attr "role"
/// aria-* attribute
let _aria typ = attr (sprintf "aria-%s" typ)
/// onclick attribute
let _onclick = attr "onclick"
/// onsubmit attribute
let _onsubmit = attr "onsubmit"

/// scoped flag (used for <style> tag)
let _scoped = flag "scoped"


/// Utility methods to help with time zones (and localization of their names)
module TimeZones =
  
  open System.Collections.Generic

  /// Cross-reference between time zone Ids and their English names
  let private xref =
    [ "America/Chicago",     "Central"
      "America/Denver",      "Mountain"
      "America/Los_Angeles", "Pacific"
      "America/New_York",    "Eastern"
      "America/Phoenix",     "Mountain (Arizona)"
      "Europe/Berlin",       "Central European"
      ]
    |> Map.ofList

  /// Get the name of a time zone, given its Id
  let name tzId (s : IStringLocalizer) =
    try s.[xref.[tzId]]
    with :? KeyNotFoundException -> LocalizedString (tzId, tzId)
