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

/// Create an input field of the given type, with matching name and ID and the given value
let inputField typ name value attrs =
    List.concat [ [ _type typ; _name name; _id name; if value <> "" then _value value ]; attrs ] |> input

/// Generate a table heading with the given localized column names
let tableHeadings (s : IStringLocalizer) (headings : string list) =
    headings
    |> List.map (fun heading -> th [ _scope "col" ] [ locStr s[heading] ])
    |> tr []
    |> List.singleton
    |> thead []

/// For a list of strings, prepend a pound sign and string them together with commas (CSS selector by ID)
let toHtmlIds it = it |> List.map (fun x -> $"#%s{x}") |> String.concat ", "

/// The name this function used to have when the view engine was part of Giraffe
let renderHtmlNode = RenderView.AsString.htmlNode


open Microsoft.AspNetCore.Html

/// Render an HTML node, then return the value as an HTML string
let renderHtmlString = renderHtmlNode >> HtmlString


/// Utility methods to help with time zones (and localization of their names)
module TimeZones =
  
    open PrayerTracker.Entities

    /// Cross-reference between time zone Ids and their English names
    let private xref = [
        TimeZoneId "America/Chicago",     "Central"
        TimeZoneId "America/Denver",      "Mountain"
        TimeZoneId "America/Los_Angeles", "Pacific"
        TimeZoneId "America/New_York",    "Eastern"
        TimeZoneId "America/Phoenix",     "Mountain (Arizona)"
        TimeZoneId "Europe/Berlin",       "Central European"
    ]

    /// Get the name of a time zone, given its Id
    let name timeZoneId (s : IStringLocalizer) =
        match xref |> List.tryFind (fun it -> fst it = timeZoneId) with
        | Some tz -> s[snd tz]
        | None ->
            let tzId = TimeZoneId.toString timeZoneId
            LocalizedString (tzId, tzId)
    
    /// All known time zones in their defined order
    let all = xref |> List.map fst


open Giraffe.ViewEngine.Htmx

/// Known htmx targets
module Target =
    
    /// htmx links target the body element
    let body = _hxTarget "body"
    
    /// htmx links target the #pt-body element
    let content = _hxTarget "#pt-body"
