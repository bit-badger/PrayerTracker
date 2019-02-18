[<AutoOpen>]
module PrayerTracker.Utils

open System.Net
open System.Security.Cryptography
open System.Text
open System.Text.RegularExpressions
open System

/// Hash a string with a SHA1 hash
let sha1Hash (x : string) =
  use alg = SHA1.Create ()
  alg.ComputeHash (ASCIIEncoding().GetBytes x)
  |> Seq.map (fun chr -> chr.ToString "x2")
  |> Seq.reduce (+)


/// Hash a string using 1,024 rounds of PBKDF2 and a salt
let pbkdf2Hash (salt : Guid) (x : string) =
  use alg = new Rfc2898DeriveBytes (x, Encoding.UTF8.GetBytes (salt.ToString "N"), 1024)
  Convert.ToBase64String(alg.GetBytes 64)


/// Replace the first occurrence of a string with a second string within a given string
let replaceFirst (needle : string) replacement (haystack : string) =
  let i = haystack.IndexOf needle
  match i with
  | -1 -> haystack
  | _ ->
      seq {
        yield haystack.Substring (0, i)
        yield replacement
        yield haystack.Substring (i + needle.Length)
        }
      |> Seq.reduce (+)

/// Strip HTML tags from the given string
// Adapted from http://www.dijksterhuis.org/safely-cleaning-html-with-strip_tags-in-csharp/
let stripTags allowedTags input =
  let stripHtmlExp = Regex @"(<\/?[^>]+>)"
  let mutable output = input
  for tag in stripHtmlExp.Matches input do
    let htmlTag = tag.Value.ToLower ()
    let isAllowed =
      allowedTags
      |> List.fold
          (fun acc t ->
              acc
                || htmlTag.IndexOf (sprintf "<%s>" t) = 0
                || htmlTag.IndexOf (sprintf "<%s " t) = 0
                || htmlTag.IndexOf (sprintf "</%s" t) = 0) false
    match isAllowed with
    | true -> ()
    | false -> output <- replaceFirst tag.Value "" output
  output

/// Wrap a string at the specified number of characters
let wordWrap charPerLine (input : string) =
  match input.Length with
  | len when len <= charPerLine -> input
  | _ ->
      let rec findSpace (inp : string) idx =
        match idx with
        | 0 -> 0
        | _ ->
            match inp.Substring (idx, 1) with
            | null | " " -> idx
            | _ -> findSpace inp (idx - 1)
      seq {
        for line in input.Replace("\r", "").Split '\n' do
          let mutable remaining = line
          match remaining.Length with
          | 0 -> ()
          | _ ->
              while charPerLine < remaining.Length do
                let spaceIdx = findSpace remaining charPerLine
                match spaceIdx with
                | 0 ->
                    // No whitespace; just break it at [characters]
                    yield remaining.Substring (0, charPerLine)
                    remaining <- remaining.Substring charPerLine
                | _ ->
                    yield remaining.Substring (0, spaceIdx)
                    remaining <- remaining.Substring (spaceIdx + 1)
              match remaining.Length with
              | 0 -> ()
              | _ -> yield remaining
        }
      |> Seq.fold (fun (acc : StringBuilder) line -> acc.AppendFormat ("{0}\n", line)) (StringBuilder ())
      |> string

/// Modify the text returned by CKEditor into the format we need for request and announcement text
let ckEditorToText (text : string) =
  text
    .Replace("\n\t", "") // \r
    .Replace("&nbsp;", " ")
    .Replace("  ", "&#xa0; ")
    .Replace("</p><p>", "<br><br>") // \r
    .Replace("</p>", "")
    .Replace("<p>", "")
    .Trim()
        

/// Convert an HTML piece of text to plain text
let htmlToPlainText html =
  match html with
  | null | "" -> ""
  | _ ->
      WebUtility.HtmlDecode((html.Trim() |> stripTags [ "br" ]).Replace("<br />", "\n").Replace("<br>", "\n"))
        .Replace("\u00a0", " ")

/// Get the second portion of a tuple as a string
let sndAsString x = (snd >> string) x

/// "Magic string" repository
[<RequireQualifiedAccess>]
module Key =

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

  /// Names and value names for use with cookies
  module Cookie =
    /// The name of the user cookie
    let user = "LoggedInUser"
    /// The name of the class cookie
    let group = "LoggedInClass"
    /// The name of the culture cookie
    let culture = "CurrentCulture"
    /// The name of the idle timeout cookie
    let timeout = "TimeoutCookie"
    /// The cookies that should be cleared when a user or group logs off
    let logOffCookies = [ user; group; timeout ]


/// Enumerated values for small group request list visibility (derived from preferences, used in UI)
module RequestVisibility =
  /// Requests are publicly accessible
  [<Literal>]
  let ``public`` = 1
  /// The small group members can enter a password to view the request list
  [<Literal>]
  let passwordProtected = 2
  /// No one can see the requests for a small group except its administrators ("User" access level)
  [<Literal>]
  let ``private`` = 3


/// A page with verbose user instructions
type HelpPage =
  { /// The module to which the help page applies
    ``module`` : string
    /// The topic for the help page
    topic : string
    /// The text with which this help page is linked (context help is linked with an icon)
    linkedText : string
    }
  with
    /// A help page that does not exist
    static member None = { ``module`` = null; topic = null; linkedText = null }

    /// The URL fragment for this page (appended to "/help/" for the full URL)
    member this.Url = sprintf "%s/%s" this.``module`` this.topic


/// Links for help locations
module Help =
  /// Help link for small group preference edit page
  let groupPreferences = { ``module`` = "group"; topic = "preferences"; linkedText = "Change Preferences" }
  /// Help link for send announcement page
  let sendAnnouncement = { ``module`` = "group"; topic = "announcement"; linkedText = "Send Announcement" }
  /// Help link for maintain group members page
  let maintainGroupMembers = { ``module`` = "group"; topic = "members"; linkedText = "Maintain Group Members" }
  /// Help link for request edit page
  let editRequest = { ``module`` = "requests"; topic = "edit"; linkedText = "Add / Edit a Request" }
  /// Help link for maintain requests page
  let maintainRequests = { ``module`` = "requests"; topic = "maintain"; linkedText = "Maintain Requests" }
  /// Help link for view request list page
  let viewRequestList = { ``module`` = "requests"; topic = "view"; linkedText = "View Request List" }
  /// Help link for user and class login pages
  let logOn = { ``module`` = "user"; topic = "logon"; linkedText = "Log On" }
  /// Help link for user password change page
  let changePassword = { ``module`` = "user"; topic = "password"; linkedText = "Change Your Password" }
  /// All help pages (the order is the order in which they are displayed on the main help page)
  let all =
    [ logOn
      maintainRequests
      editRequest
      groupPreferences
      maintainGroupMembers
      viewRequestList
      sendAnnouncement
      changePassword
      ]


/// This class serves as a common anchor for resources
type Common () =
  do ()
