[<AutoOpen>]
module PrayerTracker.Views.CommonFunctions

open System.IO
open System.Text.Encodings.Web
open Giraffe
open Giraffe.ViewEngine
open Microsoft.AspNetCore.Antiforgery
open Microsoft.AspNetCore.Http
open Microsoft.AspNetCore.Mvc.Localization
open Microsoft.Extensions.Localization

/// Encoded text for a localized string
let locStr (text : LocalizedString) = str text.Value

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
let iconSized size name = i [ _class $"material-icons md-%i{size}" ] [ rawText name ]

/// Generate a CSRF prevention token
let csrfToken (ctx : HttpContext) =
    let antiForgery = ctx.GetService<IAntiforgery> ()
    let tokenSet    = antiForgery.GetAndStoreTokens ctx
    input [ _type "hidden"; _name tokenSet.FormFieldName; _value tokenSet.RequestToken ]

/// Create a summary for a table of items
let tableSummary itemCount (s : IStringLocalizer) =
    div [ _class "pt-center-text" ] [
        small [] [
            match itemCount with
            | 0 -> s["No Entries to Display"]
            | 1 -> s["Displaying {0} Entry", itemCount]
            | _ -> s["Displaying {0} Entries", itemCount]
            |> locStr
        ]
    ]
     
/// Generate a list of named HTML colors
let namedColorList name selected attrs (s : IStringLocalizer) =
    // The list of HTML named colors (name, display, text color)
    seq {
        ("aqua",    s["Aqua"],    "black")
        ("black",   s["Black"],   "white")
        ("blue",    s["Blue"],    "white")
        ("fuchsia", s["Fuchsia"], "black")
        ("gray",    s["Gray"],    "white")
        ("green",   s["Green"],   "white")
        ("lime",    s["Lime"],    "black")
        ("maroon",  s["Maroon"],  "white")
        ("navy",    s["Navy"],    "white")
        ("olive",   s["Olive"],   "white")
        ("purple",  s["Purple"],  "white")
        ("red",     s["Red"],     "black")
        ("silver",  s["Silver"],  "black")
        ("teal",    s["Teal"],    "white")
        ("white",   s["White"],   "black")
        ("yellow",  s["Yellow"],  "black")
    }
    |> Seq.map (fun color ->
        let colorName, text, txtColor = color
        option
            [ _value colorName
              _style $"background-color:{colorName};color:{txtColor};"
              if colorName = selected then _selected
            ] [ encodedText (text.Value.ToLower ()) ])
    |> List.ofSeq
    |> select (_name name :: attrs)

/// Generate an input[type=radio] that is selected if its value is the current value
let radio name domId value current =
    input [ _type "radio"
            _name name
            _id domId
            _value value
            if value = current then _checked ]

/// Generate a select list with the current value selected
let selectList name selected attrs items =
    items
    |> Seq.map (fun (value, text) ->
        option
            [ _value value
              if value = selected then _selected
            ] [ encodedText text ])
    |> List.ofSeq
    |> select (List.concat [ [ _name name; _id name ]; attrs ])

/// Generate the text for a default entry at the top of a select list
let selectDefault text = $"— %s{text} —"

/// Generate a standard submit button with icon and text
let submit attrs ico text = button (_type "submit" :: attrs) [ icon ico; rawText " &nbsp;"; locStr text ]


open System

// TODO: this is where to implement issue #1
/// Format a GUID with no dashes (used for URLs and forms)
let flatGuid (x : Guid) = x.ToString "N"

/// An empty GUID string (used for "add" actions)
let emptyGuid = flatGuid Guid.Empty

/// Create an HTML onsubmit event handler
let _onsubmit = attr "onsubmit"

/// A "rel='noopener'" attribute
let _relNoOpener = _rel "noopener"

/// A class attribute that designates a row of fields, with the additional classes passed
let _fieldRowWith classes =
    let extraClasses = if List.isEmpty classes then "" else $""" {classes |> String.concat " "}"""
    _class $"pt-field-row{extraClasses}"

/// The class that designates a row of fields
let _fieldRow = _fieldRowWith []

/// A class attribute that designates an input field, with the additional classes passed
let _inputFieldWith classes =
    let extraClasses = if List.isEmpty classes then "" else $""" {classes |> String.concat " "}"""
    _class $"pt-field{extraClasses}"

/// The class that designates an input field / label pair
let _inputField = _inputFieldWith []

/// The class that designates a checkbox / label pair
let _checkboxField = _class "pt-checkbox-field"

/// The name this function used to have when the view engine was part of Giraffe
let renderHtmlNode = RenderView.AsString.htmlNode


open Microsoft.AspNetCore.Html

/// Render an HTML node, then return the value as an HTML string
let renderHtmlString = renderHtmlNode >> HtmlString


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
        try s[xref[tzId]]
        with :? KeyNotFoundException -> LocalizedString (tzId, tzId)


open Giraffe.ViewEngine.Htmx

/// Known htmx targets
module Target =
    
    /// htmx links target the body element
    let body = _hxTarget "body"
    
    /// htmx links target the #pt-body element
    let content = _hxTarget "#pt-body"
