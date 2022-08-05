[<AutoOpen>]
module PrayerTracker.Utils

open System
open System.Security.Cryptography
open System.Text

open Giraffe

/// Parse a short-GUID-based ID from a string
let idFromShort<'T> (f : Guid -> 'T) strValue =
    (ShortGuid.toGuid >> f) strValue

/// Format a GUID as a short GUID
let shortGuid = ShortGuid.fromGuid

/// An empty short GUID string (used for "add" actions)
let emptyGuid = shortGuid Guid.Empty


/// String helper functions
module String =
  
    /// string.Trim()
    let trim (str: string) = str.Trim ()

    /// string.Replace()
    let replace (find : string) repl (str : string) = str.Replace (find, repl)

    /// Replace the first occurrence of a string with a second string within a given string
    let replaceFirst (needle : string) replacement (haystack : string) =
        match haystack.IndexOf needle with
        | -1 -> haystack
        | idx -> String.concat "" [ haystack[0..idx - 1]; replacement; haystack[idx + needle.Length..] ]


open System.Text.RegularExpressions

/// Strip HTML tags from the given string
// Adapted from http://www.dijksterhuis.org/safely-cleaning-html-with-strip_tags-in-csharp/
let stripTags allowedTags input =
    let stripHtmlExp = Regex @"(<\/?[^>]+>)"
    let mutable output = input
    for tag in stripHtmlExp.Matches input do
        let htmlTag = tag.Value.ToLower ()
        let shouldReplace =
            allowedTags
            |> List.fold (fun acc t ->
                   acc
                || htmlTag.IndexOf $"<{t}>" = 0
                || htmlTag.IndexOf $"<{t} " = 0
                || htmlTag.IndexOf $"</{t}" = 0) false
            |> not
        if shouldReplace then output <- String.replaceFirst tag.Value "" output
    output


/// Wrap a string at the specified number of characters
let wordWrap charPerLine (input : string) =
    match input.Length with
    | len when len <= charPerLine -> input
    | _ ->
        seq {
            for line in input.Replace("\r", "").Split '\n' do
                let mutable remaining = line
                match remaining.Length with
                | 0 -> ()
                | _ ->
                    while charPerLine < remaining.Length do
                        if charPerLine + 1 < remaining.Length && remaining[charPerLine] = ' ' then
                            // Line length is followed by a space; return [charPerLine] as a line
                            yield remaining[0..charPerLine - 1]
                            remaining <- remaining[charPerLine + 1..]
                        else
                            match remaining[0..charPerLine - 1].LastIndexOf ' ' with
                            | -1 ->
                                // No whitespace; just break it at [characters]
                                yield remaining[0..charPerLine - 1]
                                remaining <- remaining[charPerLine..]
                            | spaceIdx ->
                                // Break on the last space in the line
                                yield remaining[0..spaceIdx - 1]
                                remaining <- remaining[spaceIdx + 1..]
                    // Leftovers - yum!
                    match remaining.Length with 0 -> () | _ -> yield remaining
            yield ""
        }
        |> String.concat "\n"

/// Modify the text returned by CKEditor into the format we need for request and announcement text
let ckEditorToText (text : string) =
    [ "\n\t",    ""
      "&nbsp;",  " "
      "  ",      "&#xa0; "
      "</p><p>", "<br><br>"
      "</p>",    ""
      "<p>",     ""
    ]
    |> List.fold (fun (txt : string) (x, y) -> String.replace x y txt) text
    |> String.trim


open System.Net

/// Convert an HTML piece of text to plain text
let htmlToPlainText html =
    match html with
    | null | "" -> ""
    | _ ->
        html.Trim ()
        |> stripTags [ "br" ]
        |> String.replace "<br />" "\n"
        |> String.replace "<br>" "\n"
        |> WebUtility.HtmlDecode
        |> String.replace "\u00a0" " "

/// Get the second portion of a tuple as a string
let sndAsString x = (snd >> string) x


/// Make a URL with query string parameters
let makeUrl url qs =
    if List.isEmpty qs then url
    else $"""{url}?{String.Join('&',  List.map (fun (k, v) -> $"%s{k}={WebUtility.UrlEncode v}") qs)}"""


/// "Magic string" repository
[<RequireQualifiedAccess>]
module Key =
    
    /// The request start time (added via middleware, read when rendering the footer)
    let startTime = "StartTime"
    
    /// This contains constants for session-stored objects within PrayerTracker
    module Session =
        
        /// The currently logged-on small group
        let currentGroup = "CurrentGroup"
        
        /// The currently logged-on user
        let currentUser = "CurrentUser"
        
        /// User messages to be displayed the next time a page is sent
        let userMessages = "UserMessages"
        
        /// The URL to which the user should be redirected once they have logged in
        let redirectUrl = "RedirectUrl"


/// Enumerated values for small group request list visibility (derived from preferences, used in UI)
module GroupVisibility =
    
    /// Requests are publicly accessible
    [<Literal>]
    let PublicList = 1
    
    /// The small group members can enter a password to view the request list
    [<Literal>]
    let HasPassword = 2
    
    /// No one can see the requests for a small group except its administrators ("User" access level)
    [<Literal>]
    let PrivateList = 3


/// Links for help locations
module Help =
    
    /// Help link for small group preference edit page
    let groupPreferences = "small-group/preferences"
    
    /// Help link for send announcement page
    let sendAnnouncement = "small-group/announcement"
    
    /// Help link for maintain group members page
    let maintainGroupMembers = "small-group/members"
    
    /// Help link for request edit page
    let editRequest = "requests/edit"
    
    /// Help link for maintain requests page
    let maintainRequests = "requests/maintain"
    
    /// Help link for view request list page
    let viewRequestList = "requests/view"
    
    /// Help link for user and class login pages
    let logOn = "user/log-on"
    
    /// Help link for user password change page
    let changePassword = "user/password"
    
    /// Create a full link for a help page
    let fullLink lang url = $"https://docs.prayer.bitbadger.solutions/%s{lang}/%s{url}.html"


/// This class serves as a common anchor for resources
type Common () =
    do ()
