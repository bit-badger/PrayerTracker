module PrayerTracker.UI.ViewModelsTests

open Expecto
open Microsoft.AspNetCore.Html
open PrayerTracker.Entities
open PrayerTracker.Tests.TestLocalization
open PrayerTracker.Utils
open PrayerTracker.ViewModels
open System


/// Filter function that filters nothing
let countAll _ = true


module ReferenceListTests =
  
  [<Tests>]
  let emailTypeListTests =
    testList "ReferenceList.emailTypeList" [
      test "includes default type" {
        let typs = ReferenceList.emailTypeList EmailType.Html _s
        Expect.hasCountOf typs 3u countAll "There should have been 3 e-mail type options returned"
        let top = Seq.head typs
        Expect.equal (fst top) "" "The default option should have been blank"
        Expect.equal (snd top).Value "Group Default (HTML Format)" "The default option label was incorrect"
        let nxt = typs |> Seq.skip 1 |> Seq.head
        Expect.equal (fst nxt) EmailType.Html "The 2nd option should have been HTML"
        let lst = typs |> Seq.last
        Expect.equal (fst lst) EmailType.PlainText "The 3rd option should have been plain text"
        }
      ]
  
  [<Tests>]
  let expirationListTests =
    testList "ReferenceList.expirationList" [
      test "excludes immediate expiration if not required" {
        let exps = ReferenceList.expirationList _s false
        Expect.hasCountOf exps 2u countAll "There should have been 2 expiration types returned"
        Expect.exists exps (fun exp -> fst exp = "N") "The option for normal expiration was not found"
        Expect.exists exps (fun exp -> fst exp = "Y") "The option for \"never expire\" was not found"
        }
      test "includes immediate expiration if required" {
        let exps = ReferenceList.expirationList _s true
        Expect.hasCountOf exps 3u countAll "There should have been 3 expiration types returned"
        Expect.exists exps (fun exp -> fst exp = "N") "The option for normal expiration was not found"
        Expect.exists exps (fun exp -> fst exp = "Y") "The option for \"never expire\" was not found"
        Expect.exists exps (fun exp -> fst exp = "X") "The option for \"expire immediately\" was not found"
        }
      ]
  
  [<Tests>]
  let requestTypeListTests =
    testList "ReferenceList.requestTypeList" [
      let withList f () =
        (ReferenceList.requestTypeList >> f) _s
      yield! testFixture withList [
        yield "returns 5 types",
          fun typs -> Expect.hasCountOf typs 5u countAll "There should have been 5 request types returned"
        yield! [ RequestType.Current; RequestType.Recurring; RequestType.Praise; RequestType.Expecting;
                 RequestType.Announcement
          ]
        |> List.map (fun typ ->
            sprintf "contains \"%s\"" typ,
              fun typs ->
                  Expect.isSome (typs |> List.tryFind (fun x -> fst x = typ))
                    (sprintf "The \"%s\" option was not found" typ))
        ]
      ]


[<Tests>]
let announcementTests =
  let empty = { sendToClass = "N"; text = "<p>unit testing</p>"; addToRequestList = None; requestType = None }
  testList "Announcement" [
    test "plainText strips HTML" {
      let ann = { empty with text = "<p>unit testing</p>" }
      Expect.equal (ann.plainText ()) "unit testing" "Plain text should have stripped HTML"
      }
    test "plainText wraps at 74 characters" {
      let ann = { empty with text = String.replicate 80 "x" }
      let txt = (ann.plainText ()).Split "\n"
      Expect.hasCountOf txt 3u countAll "There should have been two lines of plain text returned"
      Expect.stringHasLength txt.[0] 74 "The first line should have been wrapped at 74 characters"
      Expect.stringHasLength txt.[1] 6 "The second line should have had the remaining 6 characters"
      Expect.stringHasLength txt.[2] 0 "The third line should have been blank"
      }
    test "plainText wraps at 74 characters and strips HTML" {
      let ann = { empty with text = sprintf "<strong>%s</strong>" (String.replicate 80 "z") }
      let txt = ann.plainText ()
      Expect.stringStarts txt "zzz" "HTML should have been stripped from the front of the plain text"
      Expect.equal (txt.ToCharArray ()).[74] '\n' "The text should have been broken at 74 characters"
      }
    ]

[<Tests>]
let appViewInfoTests =
  testList "AppViewInfo" [
    test "fresh is constructed properly" {
      let vi = AppViewInfo.fresh
      Expect.isEmpty vi.style "There should have been no styles set"
      Expect.isEmpty vi.script "There should have been no scripts set"
      Expect.equal vi.helpLink.Url HelpPage.None.Url "The help link should have been set to none"
      Expect.isEmpty vi.messages "There should have been no messages set"
      Expect.equal vi.version "" "The version should have been blank"
      Expect.isGreaterThan vi.requestStart DateTime.MinValue.Ticks "The request start time should have been set"
      Expect.isNone vi.user "There should not have been a user"
      Expect.isNone vi.group "There should not have been a small group"
      }
    ]

[<Tests>]
let assignGroupsTests =
  testList "AssignGroups" [
    test "fromUser populates correctly" {
      let usr = { User.empty with userId = Guid.NewGuid (); firstName = "Alice"; lastName = "Bob" }
      let asg = AssignGroups.fromUser usr
      Expect.equal asg.userId usr.userId "The user ID was not filled correctly"
      Expect.equal asg.userName usr.fullName "The user name was not filled correctly"
      Expect.equal asg.smallGroups "" "The small group string was not filled correctly"
      }
    ]

[<Tests>]
let editChurchTests =
  testList "EditChurch" [
    test "fromChurch populates correctly when interface exists" {
      let church =
        { Church.empty with
            churchId         = Guid.NewGuid ()
            name             = "Unit Test"
            city             = "Testlandia"
            st               = "UT"
            hasInterface     = true
            interfaceAddress = Some "https://test-dem-units.test"
          }
      let edit = EditChurch.fromChurch church
      Expect.equal edit.churchId church.churchId "The church ID was not filled correctly"
      Expect.equal edit.name church.name "The church name was not filled correctly"
      Expect.equal edit.city church.city "The church's city was not filled correctly"
      Expect.equal edit.st church.st "The church's state was not filled correctly"
      Expect.isSome edit.hasInterface "The church should show that it has an interface"
      Expect.equal edit.hasInterface (Some true) "The hasInterface flag should be true"
      Expect.isSome edit.interfaceAddress "The interface address should exist"
      Expect.equal edit.interfaceAddress church.interfaceAddress "The interface address was not filled correctly"
      }
    test "fromChurch populates correctly when interface does not exist" {
      let edit =
        EditChurch.fromChurch
          { Church.empty with
              churchId = Guid.NewGuid ()
              name     = "Unit Test"
              city     = "Testlandia"
              st       = "UT"
            }
      Expect.isNone edit.hasInterface "The church should not show that it has an interface"
      Expect.isNone edit.interfaceAddress "The interface address should not exist"
      }
    test "empty is as expected" {
      let edit = EditChurch.empty
      Expect.equal edit.churchId Guid.Empty "The church ID should be the empty GUID"
      Expect.equal edit.name "" "The church name should be blank"
      Expect.equal edit.city "" "The church's city should be blank"
      Expect.equal edit.st "" "The church's state should be blank"
      Expect.isNone edit.hasInterface "The church should not show that it has an interface"
      Expect.isNone edit.interfaceAddress "The interface address should not exist"
      }
    test "isNew works on a new church" {
      Expect.isTrue (EditChurch.empty.isNew ()) "An empty GUID should be flagged as a new church"
      }
    test "isNew works on an existing church" {
      Expect.isFalse ({ EditChurch.empty with churchId = Guid.NewGuid () }.isNew ())
        "A non-empty GUID should not be flagged as a new church"
      }
    test "populateChurch works correctly when an interface exists" {
      let edit =
        { EditChurch.empty with
            churchId         = Guid.NewGuid ()
            name             = "Test Baptist Church"
            city             = "Testerville"
            st               = "TE"
            hasInterface     = Some true
            interfaceAddress = Some "https://test.units"
          }
      let church = edit.populateChurch Church.empty
      Expect.notEqual church.churchId edit.churchId "The church ID should not have been modified"
      Expect.equal church.name edit.name "The church name was not updated correctly"
      Expect.equal church.city edit.city "The church's city was not updated correctly"
      Expect.equal church.st edit.st "The church's state was not updated correctly"
      Expect.isTrue church.hasInterface "The church should show that it has an interface"
      Expect.isSome church.interfaceAddress "The interface address should exist"
      Expect.equal church.interfaceAddress edit.interfaceAddress "The interface address was not updated correctly"
      }
    test "populateChurch works correctly when an interface does not exist" {
      let church =
        { EditChurch.empty with
            name = "Test Baptist Church"
            city = "Testerville"
            st   = "TE"
          }.populateChurch Church.empty
      Expect.isFalse church.hasInterface "The church should show that it has an interface"
      Expect.isNone church.interfaceAddress "The interface address should exist"
      }
    ]

[<Tests>]
let editMemberTests =
  testList "EditMember" [
    test "fromMember populates with group default format" {
      let mbr  =
        { Member.empty with
            memberId   = Guid.NewGuid ()
            memberName = "Test Name"
            email      = "test_units@example.com"
          }
      let edit = EditMember.fromMember mbr
      Expect.equal edit.memberId mbr.memberId "The member ID was not filled correctly"
      Expect.equal edit.memberName mbr.memberName "The member name was not filled correctly"
      Expect.equal edit.emailAddress mbr.email "The e-mail address was not filled correctly"
      Expect.equal edit.emailType "" "The e-mail type should have been blank for group default"
      }
    test "fromMember populates with specific format" {
      let edit = EditMember.fromMember { Member.empty with format = Some EmailType.Html }
      Expect.equal edit.emailType EmailType.Html "The e-mail type was not filled correctly"
      }
    test "empty is as expected" {
      let edit = EditMember.empty
      Expect.equal edit.memberId Guid.Empty "The member ID should have been an empty GUID"
      Expect.equal edit.memberName "" "The member name should have been blank"
      Expect.equal edit.emailAddress "" "The e-mail address should have been blank"
      Expect.equal edit.emailType "" "The e-mail type should have been blank"
      }
    test "isNew works for a new member" {
      Expect.isTrue (EditMember.empty.isNew ()) "An empty GUID should be flagged as a new member"
      }
    test "isNew works for an existing member" {
      Expect.isFalse ({ EditMember.empty with memberId = Guid.NewGuid () }.isNew ())
        "A non-empty GUID should not be flagged as a new member"
      }
    ]

[<Tests>]
let editPreferencesTests =
  testList "EditPreferences" [
    test "fromPreferences succeeds for named colors and private list" {
      let prefs = ListPreferences.empty
      let edit  = EditPreferences.fromPreferences prefs
      Expect.equal edit.expireDays prefs.daysToExpire "The expiration days were not filled correctly"
      Expect.equal edit.daysToKeepNew prefs.daysToKeepNew "The days to keep new were not filled correctly"
      Expect.equal edit.longTermUpdateWeeks prefs.longTermUpdateWeeks "The weeks for update were not filled correctly"
      Expect.equal edit.requestSort prefs.requestSort "The request sort was not filled correctly"
      Expect.equal edit.emailFromName prefs.emailFromName "The e-mail from name was not filled correctly"
      Expect.equal edit.emailFromAddress prefs.emailFromAddress "The e-mail from address was not filled correctly"
      Expect.equal edit.defaultEmailType prefs.defaultEmailType "The default e-mail type was not filled correctly"
      Expect.equal edit.headingLineType "Name" "The heading line color type was not derived correctly"
      Expect.equal edit.headingLineColor prefs.lineColor "The heading line color was not filled correctly"
      Expect.equal edit.headingTextType "Name" "The heading text color type was not derived correctly"
      Expect.equal edit.headingTextColor prefs.headingColor "The heading text color was not filled correctly"
      Expect.equal edit.listFonts prefs.listFonts "The list fonts were not filled correctly"
      Expect.equal edit.headingFontSize prefs.headingFontSize "The heading font size was not filled correctly"
      Expect.equal edit.listFontSize prefs.textFontSize "The list text font size was not filled correctly"
      Expect.equal edit.timeZone prefs.timeZoneId "The time zone was not filled correctly"
      Expect.isSome edit.groupPassword "The group password should have been set"
      Expect.equal edit.groupPassword (Some prefs.groupPassword) "The group password was not filled correctly"
      Expect.equal edit.listVisibility RequestVisibility.``private`` "The list visibility was not derived correctly"
      }
    test "fromPreferences succeeds for RGB line color and password-protected list" {
      let prefs = { ListPreferences.empty with lineColor = "#ff0000"; groupPassword = "pw" }
      let edit  = EditPreferences.fromPreferences prefs
      Expect.equal edit.headingLineType "RGB" "The heading line color type was not derived correctly"
      Expect.equal edit.headingLineColor prefs.lineColor "The heading line color was not filled correctly"
      Expect.isSome edit.groupPassword "The group password should have been set"
      Expect.equal edit.groupPassword (Some prefs.groupPassword) "The group password was not filled correctly"
      Expect.equal edit.listVisibility RequestVisibility.passwordProtected
        "The list visibility was not derived correctly"
      }
    test "fromPreferences succeeds for RGB text color and public list" {
      let prefs = { ListPreferences.empty with headingColor = "#0000ff"; isPublic = true }
      let edit  = EditPreferences.fromPreferences prefs
      Expect.equal edit.headingTextType "RGB" "The heading text color type was not derived correctly"
      Expect.equal edit.headingTextColor prefs.headingColor "The heading text color was not filled correctly"
      Expect.isSome edit.groupPassword "The group password should have been set"
      Expect.equal edit.groupPassword (Some "") "The group password was not filled correctly"
      Expect.equal edit.listVisibility RequestVisibility.``public`` "The list visibility was not derived correctly"
      }
    ]

[<Tests>]
let editRequestTests =
  testList "EditRequest" [
    test "empty is as expected" {
      let mt = EditRequest.empty
      Expect.equal mt.requestId Guid.Empty "The request ID should be an empty GUID"
      Expect.equal mt.requestType "" "The request type should have been blank"
      Expect.isNone mt.enteredDate "The entered date should have been None"
      Expect.isNone mt.skipDateUpdate "The \"skip date update\" flag should have been None"
      Expect.isNone mt.requestor "The requestor should have been None"
      Expect.equal mt.expiration "N" "The expiration should have been \"N\""
      Expect.equal mt.text "" "The text should have been blank"
      }
    test "fromRequest succeeds when a request has the do-not-expire flag set" {
      let req =
        { PrayerRequest.empty with
            prayerRequestId = Guid.NewGuid ()
            requestType     = RequestType.Current
            requestor       = Some "Me"
            doNotExpire     = true
            text            = "the text"
          }
      let edit = EditRequest.fromRequest req
      Expect.equal edit.requestId req.prayerRequestId "The request ID was not filled correctly"
      Expect.equal edit.requestType req.requestType "The request type was not filled correctly"
      Expect.equal edit.requestor req.requestor "The requestor was not filled correctly"
      Expect.equal edit.expiration "Y" "The expiration should have been \"Y\" since the do-not-expire flag was set"
      Expect.equal edit.text req.text "The text was not filled correctly"
      }
    test "fromRequest succeeds when a request has the do-not-expire flag unset" {
      let req =
        { PrayerRequest.empty with
            requestor       = None
            doNotExpire     = false
          }
      let edit = EditRequest.fromRequest req
      Expect.equal edit.requestor req.requestor "The requestor was not filled correctly"
      Expect.equal edit.expiration "N" "The expiration should have been \"N\" since the do-not-expire flag was not set"
      }
    test "isNew works for a new request" {
      Expect.isTrue (EditRequest.empty.isNew ()) "An empty GUID should be flagged as a new request"
      }
    test "isNew works for an existing request" {
      Expect.isFalse ({ EditRequest.empty with requestId = Guid.NewGuid () }.isNew ())
        "A non-empty GUID should not be flagged as a new request"
      }
    ]

[<Tests>]
let editSmallGroupTests =
  testList "EditSmallGroup" [
    test "fromGroup succeeds" {
      let grp =
        { SmallGroup.empty with
            smallGroupId = Guid.NewGuid ()
            name         = "test group"
            churchId     = Guid.NewGuid ()
          }
      let edit = EditSmallGroup.fromGroup grp
      Expect.equal edit.smallGroupId grp.smallGroupId "The small group ID was not filled correctly"
      Expect.equal edit.name grp.name "The name was not filled correctly"
      Expect.equal edit.churchId grp.churchId "The church ID was not filled correctly"
      }
    test "empty is as expected" {
      let mt = EditSmallGroup.empty
      Expect.equal mt.smallGroupId Guid.Empty "The small group ID should be an empty GUID"
      Expect.equal mt.name "" "The name should be blank"
      Expect.equal mt.churchId Guid.Empty "The church ID should be an empty GUID"
      }
    test "isNew works for a new small group" {
      Expect.isTrue (EditSmallGroup.empty.isNew ()) "An empty GUID should be flagged as a new small group"
      }
    test "isNew works for an existing small group" {
      Expect.isFalse ({ EditSmallGroup.empty with smallGroupId = Guid.NewGuid () }.isNew ())
        "A non-empty GUID should not be flagged as a new small group"
      }
    test "populateGroup succeeds" {
      let edit =
        { EditSmallGroup.empty with
            name     = "test name"
            churchId = Guid.NewGuid ()
          }
      let grp = edit.populateGroup SmallGroup.empty
      Expect.equal grp.name edit.name "The name was not populated correctly"
      Expect.equal grp.churchId edit.churchId "The church ID was not populated correctly"
      }
    ]

[<Tests>]
let editUserTests =
  testList "EditUser" [
    test "empty is as expected" {
      let mt = EditUser.empty
      Expect.equal mt.userId Guid.Empty "The user ID should be an empty GUID"
      Expect.equal mt.firstName "" "The first name should be blank"
      Expect.equal mt.lastName "" "The last name should be blank"
      Expect.equal mt.emailAddress "" "The e-mail address should be blank"
      Expect.equal mt.password "" "The password should be blank"
      Expect.equal mt.passwordConfirm "" "The confirmed password should be blank"
      Expect.isNone mt.isAdmin "The isAdmin flag should be None"
      }
    test "fromUser succeeds" {
      let usr =
        { User.empty with
            userId       = Guid.NewGuid ()
            firstName    = "user"
            lastName     = "test"
            emailAddress = "a@b.c"
          }
      let edit = EditUser.fromUser usr
      Expect.equal edit.userId usr.userId "The user ID was not filled correctly"
      Expect.equal edit.firstName usr.firstName "The first name was not filled correctly"
      Expect.equal edit.lastName usr.lastName "The last name was not filled correctly"
      Expect.equal edit.emailAddress usr.emailAddress "The e-mail address was not filled correctly"
      Expect.isNone edit.isAdmin "The isAdmin flag was not filled correctly"
      }
    test "isNew works for a new user" {
      Expect.isTrue (EditUser.empty.isNew ()) "An empty GUID should be flagged as a new user"
      }
    test "isNew works for an existing user" {
      Expect.isFalse ({ EditUser.empty with userId = Guid.NewGuid () }.isNew ())
        "A non-empty GUID should not be flagged as a new user"
      }
    test "populateUser succeeds" {
      let edit =
        { EditUser.empty with
            firstName    = "name"
            lastName     = "eman"
            emailAddress = "n@m.e"
            isAdmin      = Some true
            password     = "testpw"
          }
      let hasher = fun x -> x + "+"
      let usr = edit.populateUser User.empty hasher
      Expect.equal usr.firstName edit.firstName "The first name was not populated correctly"
      Expect.equal usr.lastName edit.lastName "The last name was not populated correctly"
      Expect.equal usr.emailAddress edit.emailAddress "The e-mail address was not populated correctly"
      Expect.isTrue usr.isAdmin "The isAdmin flag was not populated correctly"
      Expect.equal usr.passwordHash (hasher edit.password) "The password hash was not populated correctly"
      }
    ]

[<Tests>]
let groupLogOnTests =
  testList "GroupLogOn" [
    test "empty is as expected" {
      let mt = GroupLogOn.empty
      Expect.equal mt.smallGroupId Guid.Empty "The small group ID should be an empty GUID"
      Expect.equal mt.password "" "The password should be blank"
      Expect.isNone mt.rememberMe "Remember Me should be None"
      }
    ]

[<Tests>]
let requestListTests =
  testList "RequestList" [
    let withRequestList f () =
      { requests   = [
          { PrayerRequest.empty with
              requestType = RequestType.Current
              requestor   = Some "Zeb"
              text        = "zyx"
              updatedDate = DateTime.Today
            }
          { PrayerRequest.empty with
              requestType = RequestType.Current
              requestor   = Some "Aaron"
              text        = "abc"
              updatedDate = DateTime.Today - TimeSpan.FromDays 9.
            }
          { PrayerRequest.empty with
              requestType = RequestType.Praise
              text        = "nmo"
              updatedDate = DateTime.Today
            }
          ]
        date       = DateTime.Today
        listGroup  = SmallGroup.empty
        showHeader = false
        recipients = []
        canEmail   = false
        }
      |> f
    yield! testFixture withRequestList [
      "asHtml succeeds without header",
      fun reqList ->
          let htmlList = { reqList with listGroup = { reqList.listGroup with name = "Test HTML Group" } }
          let html = htmlList.asHtml _s
          Expect.equal -1 (html.IndexOf "Test HTML Group") "The small group name should not have existed (no header)"
          let curReqHeading =
            [ "<table style=\"font-family:Century Gothic,Tahoma,Luxi Sans,sans-serif;page-break-inside:avoid;\">"
              "<tr>"
              "<td style=\"font-size:16pt;color:maroon;padding:3px 0;border-top:solid 3px navy;border-bottom:solid 3px navy;font-weight:bold;\">"
              "&nbsp; &nbsp; Current Requests&nbsp; &nbsp; </td></tr></table>"
              ]
            |> String.concat ""
          Expect.stringContains html curReqHeading "Heading for category \"Current Requests\" not found"
          let curReqHtml =
            [ "<ul>"
              "<li style=\"list-style-type:circle;font-family:Century Gothic,Tahoma,Luxi Sans,sans-serif;font-size:12pt;padding-bottom:.25em;\">"
              "<strong>Zeb</strong> &mdash; zyx</li>"
              "<li style=\"list-style-type:disc;font-family:Century Gothic,Tahoma,Luxi Sans,sans-serif;font-size:12pt;padding-bottom:.25em;\">"
              "<strong>Aaron</strong> &mdash; abc</li></ul>"
              ]
            |> String.concat ""
          Expect.stringContains html curReqHtml "Expected HTML for \"Current Requests\" requests not found"
          let praiseHeading =
            [ "<table style=\"font-family:Century Gothic,Tahoma,Luxi Sans,sans-serif;page-break-inside:avoid;\">"
              "<tr>"
              "<td style=\"font-size:16pt;color:maroon;padding:3px 0;border-top:solid 3px navy;border-bottom:solid 3px navy;font-weight:bold;\">"
              "&nbsp; &nbsp; Praise Reports&nbsp; &nbsp; </td></tr></table>"
              ]
            |> String.concat ""
          Expect.stringContains html praiseHeading "Heading for category \"Praise Reports\" not found"
          let praiseHtml =
            [ "<ul>"
              "<li style=\"list-style-type:circle;font-family:Century Gothic,Tahoma,Luxi Sans,sans-serif;font-size:12pt;padding-bottom:.25em;\">"
              "nmo</li></ul>"
              ]
            |> String.concat ""
          Expect.stringContains html praiseHtml "Expected HTML for \"Praise Reports\" requests not found"
      "asHtml succeeds with header",
      fun reqList ->
          let htmlList =
            { reqList with
                listGroup  = { reqList.listGroup with name = "Test HTML Group" }
                showHeader = true
              }
          let html = htmlList.asHtml _s
          let lstHeading =
            [ "<div style=\"text-align:center;font-family:Century Gothic,Tahoma,Luxi Sans,sans-serif\">"
              "<span style=\"font-size:16pt;\"><strong>Prayer Requests</strong></span><br>"
              "<span style=\"font-size:12pt;\"><strong>Test HTML Group</strong><br>"
              htmlList.date.ToString "MMMM d, yyyy"
              "</span></div><br>"
              ]
            |> String.concat ""
          Expect.stringContains html lstHeading "Expected HTML for the list heading not found"
          // spot check; without header test tests this exhaustively
          Expect.stringContains html "<strong>Zeb</strong> &mdash; zyx</li>" "Expected requests not found"
      "asText succeeds",
      fun reqList ->
          let textList = { reqList with listGroup = { reqList.listGroup with name = "Test Group" } }
          let text = textList.asText _s
          Expect.stringContains text (textList.listGroup.name + "\n") "Small group name not found"
          Expect.stringContains text "Prayer Requests\n" "List heading not found"
          Expect.stringContains text ((textList.date.ToString "MMMM d, yyyy") + "\n \n") "List date not found"
          Expect.stringContains text "--------------------\n  CURRENT REQUESTS\n--------------------\n"
            "Heading for category \"Current Requests\" not found"
          Expect.stringContains text "  + Zeb - zyx\n" "First request not found"
          Expect.stringContains text "  - Aaron - abc\n \n" "Second request not found; should have been end of category"
          Expect.stringContains text "------------------\n  PRAISE REPORTS\n------------------\n"
            "Heading for category \"Praise Reports\" not found"
          Expect.stringContains text "  + nmo\n \n" "Last request not found"
      "isNew succeeds for both old and new requests",
      fun reqList ->
          let reqs = reqList.requestsInCategory RequestType.Current
          Expect.hasCountOf reqs 2u countAll "There should have been two requests"
          Expect.isTrue (reqList.isNew (List.head reqs)) "The first request should have been new"
          Expect.isFalse (reqList.isNew (List.last reqs)) "The second request should not have been new"
      "requestsInCategory succeeds when requests exist",
      fun reqList ->
          let reqs = reqList.requestsInCategory RequestType.Current
          Expect.hasCountOf reqs 2u countAll "There should have been two requests"
          let first = List.head reqs
          Expect.equal first.text "zyx" "The requests should be sorted by updated date descending"
      "requestsInCategory succeeds when requests do not exist",
      fun reqList ->
          Expect.isEmpty (reqList.requestsInCategory "ABC") "There should have been no category \"ABC\" requests"
      "requestsInCategory succeeds and sorts by requestor",
      fun reqList ->
          let newList =
            { reqList with
                listGroup =
                  { reqList.listGroup with preferences = { reqList.listGroup.preferences with requestSort = "R" } }
              }
          let reqs = newList.requestsInCategory RequestType.Current
          Expect.hasCountOf reqs 2u countAll "There should have been two requests"
          let first = List.head reqs
          Expect.equal first.text "abc" "The requests should be sorted by requestor"
      ]
    ]

[<Tests>]
let userLogOnTests =
  testList "UserLogOn" [
    test "empty is as expected" {
      let mt = UserLogOn.empty
      Expect.equal mt.emailAddress "" "The e-mail address should be blank"
      Expect.equal mt.password "" "The password should be blank"
      Expect.equal mt.smallGroupId Guid.Empty "The small group ID should be an empty GUID"
      Expect.isNone mt.rememberMe "Remember Me should be None"
      Expect.isNone mt.redirectUrl "Redirect URL should be None"
      }
    ]

[<Tests>]
let userMessageTests =
  testList "UserMessage" [
    test "Error is constructed properly" {
      let msg = UserMessage.Error
      Expect.equal msg.level "ERROR" "Incorrect message level"
      Expect.equal msg.text HtmlString.Empty "Text should have been blank"
      Expect.isNone msg.description "Description should have been None"
      }
    test "Warning is constructed properly" {
      let msg = UserMessage.Warning
      Expect.equal msg.level "WARNING" "Incorrect message level"
      Expect.equal msg.text HtmlString.Empty "Text should have been blank"
      Expect.isNone msg.description "Description should have been None"
      }
    test "Info is constructed properly" {
      let msg = UserMessage.Info
      Expect.equal msg.level "Info" "Incorrect message level"
      Expect.equal msg.text HtmlString.Empty "Text should have been blank"
      Expect.isNone msg.description "Description should have been None"
      }
    ]
