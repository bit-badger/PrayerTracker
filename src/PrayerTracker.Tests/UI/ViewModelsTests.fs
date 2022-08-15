module PrayerTracker.UI.ViewModelsTests

open System
open Expecto
open Microsoft.AspNetCore.Html
open NodaTime
open PrayerTracker.Entities
open PrayerTracker.Tests.TestLocalization
open PrayerTracker.Utils
open PrayerTracker.ViewModels


/// Filter function that filters nothing
let countAll _ = true


module ReferenceListTests =
  
    [<Tests>]
    let asOfDateListTests =
        testList "ReferenceList.asOfDateList" [
            test "has all three options listed" {
                let asOf = ReferenceList.asOfDateList _s
                Expect.hasCountOf asOf 3u countAll "There should have been 3 as-of choices returned"
                Expect.exists asOf (fun (x, _) -> x = AsOfDateDisplay.toCode NoDisplay)
                    "The option for no display was not found"
                Expect.exists asOf (fun (x, _) -> x = AsOfDateDisplay.toCode ShortDate)
                    "The option for a short date was not found"
                Expect.exists asOf (fun (x, _) -> x = AsOfDateDisplay.toCode LongDate)
                    "The option for a full date was not found"
            }
        ]

    [<Tests>]
    let emailTypeListTests =
        testList "ReferenceList.emailTypeList" [
            test "includes default type" {
                let typs = ReferenceList.emailTypeList HtmlFormat _s
                Expect.hasCountOf typs 3u countAll "There should have been 3 e-mail type options returned"
                let top = Seq.head typs
                Expect.equal (fst top) "" "The default option should have been blank"
                Expect.equal (snd top).Value "Group Default (HTML Format)" "The default option label was incorrect"
                let nxt = typs |> Seq.skip 1 |> Seq.head
                Expect.equal (fst nxt) (EmailFormat.toCode HtmlFormat) "The 2nd option should have been HTML"
                let lst = typs |> Seq.last
                Expect.equal (fst lst) (EmailFormat.toCode PlainTextFormat) "The 3rd option should have been plain text"
            }
        ]
    
    [<Tests>]
    let expirationListTests =
        testList "ReferenceList.expirationList" [
            test "excludes immediate expiration if not required" {
                let exps = ReferenceList.expirationList _s false
                Expect.hasCountOf exps 2u countAll "There should have been 2 expiration types returned"
                Expect.exists exps (fun (exp, _) -> exp = Expiration.toCode Automatic)
                    "The option for automatic expiration was not found"
                Expect.exists exps (fun (exp, _) -> exp = Expiration.toCode Manual)
                    "The option for manual expiration was not found"
            }
            test "includes immediate expiration if required" {
                let exps = ReferenceList.expirationList _s true
                Expect.hasCountOf exps 3u countAll "There should have been 3 expiration types returned"
                Expect.exists exps (fun (exp, _) -> exp = Expiration.toCode Automatic)
                    "The option for automatic expiration was not found"
                Expect.exists exps (fun (exp, _) -> exp = Expiration.toCode Manual)
                    "The option for manual expiration was not found"
                Expect.exists exps (fun (exp, _) -> exp = Expiration.toCode Forced)
                    "The option for immediate expiration was not found"
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
                yield!
                    [ CurrentRequest; LongTermRequest; PraiseReport; Expecting; Announcement ]
                    |> List.map (fun typ ->
                        $"contains \"%O{typ}\"",
                        fun typs ->
                            Expect.isSome (typs |> List.tryFind (fun x -> fst x = typ))
                                $"""The "%O{typ}" option was not found""")
            ]
        ]


[<Tests>]
let announcementTests =
    let empty = { SendToClass = "N"; Text = "<p>unit testing</p>"; AddToRequestList = None; RequestType = None }
    testList "Announcement" [
        test "plainText strips HTML" {
            let ann = { empty with Text = "<p>unit testing</p>" }
            Expect.equal ann.PlainText "unit testing" "Plain text should have stripped HTML"
        }
        test "plainText wraps at 74 characters" {
            let ann = { empty with Text = String.replicate 80 "x" }
            let txt = ann.PlainText.Split "\n"
            Expect.hasCountOf txt 3u countAll "There should have been two lines of plain text returned"
            Expect.stringHasLength txt[0] 74 "The first line should have been wrapped at 74 characters"
            Expect.stringHasLength txt[1] 6 "The second line should have had the remaining 6 characters"
            Expect.stringHasLength txt[2] 0 "The third line should have been blank"
        }
        test "plainText wraps at 74 characters and strips HTML" {
            let ann = { empty with Text = sprintf "<strong>%s</strong>" (String.replicate 80 "z") }
            let txt = ann.PlainText
            Expect.stringStarts txt "zzz" "HTML should have been stripped from the front of the plain text"
            Expect.equal (txt.ToCharArray ()).[74] '\n' "The text should have been broken at 74 characters"
        }
    ]

[<Tests>]
let appViewInfoTests =
    testList "AppViewInfo" [
        test "fresh is constructed properly" {
            let vi = AppViewInfo.fresh
            Expect.isEmpty vi.Style "There should have been no styles set"
            Expect.isNone vi.HelpLink "The help link should have been set to none"
            Expect.isEmpty vi.Messages "There should have been no messages set"
            Expect.equal vi.Version "" "The version should have been blank"
            Expect.equal vi.RequestStart Instant.MinValue "The request start time should have been the minimum value"
            Expect.isNone vi.User "There should not have been a user"
            Expect.isNone vi.Group "There should not have been a small group"
        }
    ]

[<Tests>]
let assignGroupsTests =
    testList "AssignGroups" [
        test "fromUser populates correctly" {
            let usr = { User.empty with Id = (Guid.NewGuid >> UserId) (); FirstName = "Alice"; LastName = "Bob" }
            let asg = AssignGroups.fromUser usr
            Expect.equal asg.UserId (shortGuid usr.Id.Value) "The user ID was not filled correctly"
            Expect.equal asg.UserName usr.Name "The user's name was not filled correctly"
            Expect.equal asg.SmallGroups "" "The small group string was not filled correctly"
        }
    ]

[<Tests>]
let editChurchTests =
    testList "EditChurch" [
        test "fromChurch populates correctly when interface exists" {
            let church =
                { Church.empty with
                    Id               = (Guid.NewGuid >> ChurchId) ()
                    Name             = "Unit Test"
                    City             = "Testlandia"
                    State            = "UT"
                    HasVpsInterface  = true
                    InterfaceAddress = Some "https://test-dem-units.test"
                  }
            let edit = EditChurch.fromChurch church
            Expect.equal edit.ChurchId (shortGuid church.Id.Value) "The church ID was not filled correctly"
            Expect.equal edit.Name church.Name "The church name was not filled correctly"
            Expect.equal edit.City church.City "The church's city was not filled correctly"
            Expect.equal edit.State church.State "The church's state was not filled correctly"
            Expect.isSome edit.HasInterface "The church should show that it has an interface"
            Expect.equal edit.HasInterface (Some true) "The HasVpsInterface flag should be true"
            Expect.isSome edit.InterfaceAddress "The interface address should exist"
            Expect.equal edit.InterfaceAddress church.InterfaceAddress "The interface address was not filled correctly"
        }
        test "fromChurch populates correctly when interface does not exist" {
            let edit =
                EditChurch.fromChurch
                    { Church.empty with
                        Id    = (Guid.NewGuid >> ChurchId) ()
                        Name  = "Unit Test"
                        City  = "Testlandia"
                        State = "UT"
                    }
            Expect.isNone edit.HasInterface "The church should not show that it has an interface"
            Expect.isNone edit.InterfaceAddress "The interface address should not exist"
        }
        test "empty is as expected" {
            let edit = EditChurch.empty
            Expect.equal edit.ChurchId emptyGuid "The church ID should be the empty GUID"
            Expect.equal edit.Name "" "The church name should be blank"
            Expect.equal edit.City "" "The church's city should be blank"
            Expect.equal edit.State "" "The church's state should be blank"
            Expect.isNone edit.HasInterface "The church should not show that it has an interface"
            Expect.isNone edit.InterfaceAddress "The interface address should not exist"
        }
        test "isNew works on a new church" {
            Expect.isTrue EditChurch.empty.IsNew "An empty GUID should be flagged as a new church"
        }
        test "isNew works on an existing church" {
            Expect.isFalse { EditChurch.empty with ChurchId = (Guid.NewGuid >> shortGuid) () }.IsNew
                "A non-empty GUID should not be flagged as a new church"
        }
        test "populateChurch works correctly when an interface exists" {
            let edit =
                { EditChurch.empty with
                    ChurchId         = (Guid.NewGuid >> shortGuid) ()
                    Name             = "Test Baptist Church"
                    City             = "Testerville"
                    State               = "TE"
                    HasInterface     = Some true
                    InterfaceAddress = Some "https://test.units"
                  }
            let church = edit.PopulateChurch Church.empty
            Expect.notEqual (shortGuid church.Id.Value) edit.ChurchId "The church ID should not have been modified"
            Expect.equal church.Name edit.Name "The church name was not updated correctly"
            Expect.equal church.City edit.City "The church's city was not updated correctly"
            Expect.equal church.State edit.State "The church's state was not updated correctly"
            Expect.isTrue church.HasVpsInterface "The church should show that it has an interface"
            Expect.isSome church.InterfaceAddress "The interface address should exist"
            Expect.equal church.InterfaceAddress edit.InterfaceAddress "The interface address was not updated correctly"
        }
        test "populateChurch works correctly when an interface does not exist" {
            let church =
                { EditChurch.empty with
                    Name  = "Test Baptist Church"
                    City  = "Testerville"
                    State = "TE"
                  }.PopulateChurch Church.empty
            Expect.isFalse church.HasVpsInterface "The church should show that it has an interface"
            Expect.isNone church.InterfaceAddress "The interface address should exist"
        }
    ]

[<Tests>]
let editMemberTests =
    testList "EditMember" [
        test "fromMember populates with group default format" {
            let mbr  =
                { Member.empty with
                    Id    = (Guid.NewGuid >> MemberId) ()
                    Name  = "Test Name"
                    Email = "test_units@example.com"
                  }
            let edit = EditMember.fromMember mbr
            Expect.equal edit.MemberId (shortGuid mbr.Id.Value) "The member ID was not filled correctly"
            Expect.equal edit.Name mbr.Name "The member name was not filled correctly"
            Expect.equal edit.Email mbr.Email "The e-mail address was not filled correctly"
            Expect.equal edit.Format "" "The e-mail format should have been blank for group default"
        }
        test "fromMember populates with specific format" {
            let edit = EditMember.fromMember { Member.empty with Format = Some HtmlFormat }
            Expect.equal edit.Format (EmailFormat.toCode HtmlFormat) "The e-mail format was not filled correctly"
        }
        test "empty is as expected" {
            let edit = EditMember.empty
            Expect.equal edit.MemberId emptyGuid "The member ID should have been an empty GUID"
            Expect.equal edit.Name "" "The member name should have been blank"
            Expect.equal edit.Email "" "The e-mail address should have been blank"
            Expect.equal edit.Format "" "The e-mail format should have been blank"
        }
        test "isNew works for a new member" {
            Expect.isTrue EditMember.empty.IsNew "An empty GUID should be flagged as a new member"
        }
        test "isNew works for an existing member" {
            Expect.isFalse { EditMember.empty with MemberId = (Guid.NewGuid >> shortGuid) () }.IsNew
                "A non-empty GUID should not be flagged as a new member"
        }
    ]

[<Tests>]
let editPreferencesTests =
    testList "EditPreferences" [
        test "fromPreferences succeeds for native fonts, named colors, and private list" {
            let prefs = ListPreferences.empty
            let edit  = EditPreferences.fromPreferences prefs
            Expect.equal edit.ExpireDays prefs.DaysToExpire "The expiration days were not filled correctly"
            Expect.equal edit.DaysToKeepNew prefs.DaysToKeepNew "The days to keep new were not filled correctly"
            Expect.equal edit.LongTermUpdateWeeks prefs.LongTermUpdateWeeks
                "The weeks for update were not filled correctly"
            Expect.equal edit.RequestSort (RequestSort.toCode prefs.RequestSort)
                "The request sort was not filled correctly"
            Expect.equal edit.EmailFromName prefs.EmailFromName "The e-mail from name was not filled correctly"
            Expect.equal edit.EmailFromAddress prefs.EmailFromAddress "The e-mail from address was not filled correctly"
            Expect.equal edit.DefaultEmailType (EmailFormat.toCode prefs.DefaultEmailType)
                "The default e-mail type was not filled correctly"
            Expect.equal edit.LineColorType "Name" "The heading line color type was not derived correctly"
            Expect.equal edit.LineColor prefs.LineColor "The heading line color was not filled correctly"
            Expect.equal edit.HeadingColorType "Name" "The heading text color type was not derived correctly"
            Expect.equal edit.HeadingColor prefs.HeadingColor "The heading text color was not filled correctly"
            Expect.isTrue edit.IsNative "The IsNative flag should have been true (default value)"
            Expect.isNone edit.Fonts "The list fonts should not exist for native font stack"
            Expect.equal edit.HeadingFontSize prefs.HeadingFontSize "The heading font size was not filled correctly"
            Expect.equal edit.ListFontSize prefs.TextFontSize "The list text font size was not filled correctly"
            Expect.equal edit.TimeZone (TimeZoneId.toString prefs.TimeZoneId) "The time zone was not filled correctly"
            Expect.isSome edit.GroupPassword "The group password should have been set"
            Expect.equal edit.GroupPassword (Some prefs.GroupPassword) "The group password was not filled correctly"
            Expect.equal edit.Visibility GroupVisibility.PrivateList
                "The list visibility was not derived correctly"
            Expect.equal edit.PageSize prefs.PageSize "The page size was not filled correctly"
            Expect.equal edit.AsOfDate (AsOfDateDisplay.toCode prefs.AsOfDateDisplay)
                "The as-of date display was not filled correctly"
        }
        test "fromPreferences succeeds for RGB line color and password-protected list" {
            let prefs = { ListPreferences.empty with LineColor = "#ff0000"; GroupPassword = "pw" }
            let edit  = EditPreferences.fromPreferences prefs
            Expect.equal edit.LineColorType "RGB" "The heading line color type was not derived correctly"
            Expect.equal edit.LineColor prefs.LineColor "The heading line color was not filled correctly"
            Expect.isSome edit.GroupPassword "The group password should have been set"
            Expect.equal edit.GroupPassword (Some prefs.GroupPassword) "The group password was not filled correctly"
            Expect.equal edit.Visibility GroupVisibility.HasPassword
                "The list visibility was not derived correctly"
        }
        test "fromPreferences succeeds for RGB text color and public list" {
            let prefs = { ListPreferences.empty with HeadingColor = "#0000ff"; IsPublic = true }
            let edit  = EditPreferences.fromPreferences prefs
            Expect.equal edit.HeadingColorType "RGB" "The heading text color type was not derived correctly"
            Expect.equal edit.HeadingColor prefs.HeadingColor "The heading text color was not filled correctly"
            Expect.isSome edit.GroupPassword "The group password should have been set"
            Expect.equal edit.GroupPassword (Some "") "The group password was not filled correctly"
            Expect.equal edit.Visibility GroupVisibility.PublicList
                "The list visibility was not derived correctly"
        }
        test "fromPreferences succeeds for non-native fonts" {
            let prefs = { ListPreferences.empty with Fonts = "Arial,sans-serif" }
            let edit  = EditPreferences.fromPreferences prefs
            Expect.isFalse edit.IsNative "The IsNative flag should have been false"
            Expect.isSome edit.Fonts "The fonts should have been filled for non-native fonts"
            Expect.equal edit.Fonts.Value prefs.Fonts "The fonts were not filled correctly"
        }
    ]

[<Tests>]
let editRequestTests =
    testList "EditRequest" [
        test "empty is as expected" {
            let mt = EditRequest.empty
            Expect.equal mt.RequestId emptyGuid "The request ID should be an empty GUID"
            Expect.equal mt.RequestType (PrayerRequestType.toCode CurrentRequest)
                "The request type should have been \"Current\""
            Expect.isNone mt.EnteredDate "The entered date should have been None"
            Expect.isNone mt.SkipDateUpdate """The "skip date update" flag should have been None"""
            Expect.isNone mt.Requestor "The requestor should have been None"
            Expect.equal mt.Expiration (Expiration.toCode Automatic)
                """The expiration should have been "A" (Automatic)"""
            Expect.equal mt.Text "" "The text should have been blank"
        }
        test "fromRequest succeeds" {
            let req =
                { PrayerRequest.empty with
                    Id          = (Guid.NewGuid >> PrayerRequestId) ()
                    RequestType = CurrentRequest
                    Requestor   = Some "Me"
                    Expiration  = Manual
                    Text        = "the text"
                }
            let edit = EditRequest.fromRequest req
            Expect.equal edit.RequestId (shortGuid req.Id.Value) "The request ID was not filled correctly"
            Expect.equal edit.RequestType (PrayerRequestType.toCode req.RequestType)
                "The request type was not filled correctly"
            Expect.equal edit.Requestor req.Requestor "The requestor was not filled correctly"
            Expect.equal edit.Expiration (Expiration.toCode Manual) "The expiration was not filled correctly"
            Expect.equal edit.Text req.Text "The text was not filled correctly"
        }
        test "isNew works for a new request" {
            Expect.isTrue EditRequest.empty.IsNew "An empty GUID should be flagged as a new request"
        }
        test "isNew works for an existing request" {
            Expect.isFalse { EditRequest.empty with RequestId = (Guid.NewGuid >> shortGuid) () }.IsNew
                "A non-empty GUID should not be flagged as a new request"
        }
    ]

[<Tests>]
let editSmallGroupTests =
    testList "EditSmallGroup" [
        test "fromGroup succeeds" {
            let grp =
                { SmallGroup.empty with
                    Id       = (Guid.NewGuid >> SmallGroupId) ()
                    Name     = "test group"
                    ChurchId = (Guid.NewGuid >> ChurchId) ()
                  }
            let edit = EditSmallGroup.fromGroup grp
            Expect.equal edit.SmallGroupId (shortGuid grp.Id.Value) "The small group ID was not filled correctly"
            Expect.equal edit.Name grp.Name "The name was not filled correctly"
            Expect.equal edit.ChurchId (shortGuid grp.ChurchId.Value) "The church ID was not filled correctly"
        }
        test "empty is as expected" {
            let mt = EditSmallGroup.empty
            Expect.equal mt.SmallGroupId emptyGuid "The small group ID should be an empty GUID"
            Expect.equal mt.Name "" "The name should be blank"
            Expect.equal mt.ChurchId emptyGuid "The church ID should be an empty GUID"
        }
        test "isNew works for a new small group" {
            Expect.isTrue EditSmallGroup.empty.IsNew "An empty GUID should be flagged as a new small group"
        }
        test "isNew works for an existing small group" {
            Expect.isFalse { EditSmallGroup.empty with SmallGroupId = (Guid.NewGuid >> shortGuid) () }.IsNew
                "A non-empty GUID should not be flagged as a new small group"
        }
        test "populateGroup succeeds" {
            let edit =
                { EditSmallGroup.empty with
                    Name     = "test name"
                    ChurchId = (Guid.NewGuid >> shortGuid) ()
                  }
            let grp = edit.populateGroup SmallGroup.empty
            Expect.equal grp.Name edit.Name "The name was not populated correctly"
            Expect.equal grp.ChurchId (idFromShort ChurchId edit.ChurchId) "The church ID was not populated correctly"
        }
    ]

[<Tests>]
let editUserTests =
    testList "EditUser" [
        test "empty is as expected" {
            let mt = EditUser.empty
            Expect.equal mt.UserId emptyGuid "The user ID should be an empty GUID"
            Expect.equal mt.FirstName "" "The first name should be blank"
            Expect.equal mt.LastName "" "The last name should be blank"
            Expect.equal mt.Email "" "The e-mail address should be blank"
            Expect.equal mt.Password "" "The password should be blank"
            Expect.equal mt.PasswordConfirm "" "The confirmed password should be blank"
            Expect.isNone mt.IsAdmin "The IsAdmin flag should be None"
        }
        test "fromUser succeeds" {
            let usr =
                { User.empty with
                    Id        = (Guid.NewGuid >> UserId) ()
                    FirstName = "user"
                    LastName  = "test"
                    Email     = "a@b.c"
                  }
            let edit = EditUser.fromUser usr
            Expect.equal edit.UserId (shortGuid usr.Id.Value) "The user ID was not filled correctly"
            Expect.equal edit.FirstName usr.FirstName "The first name was not filled correctly"
            Expect.equal edit.LastName usr.LastName "The last name was not filled correctly"
            Expect.equal edit.Email usr.Email "The e-mail address was not filled correctly"
            Expect.isNone edit.IsAdmin "The IsAdmin flag was not filled correctly"
        }
        test "isNew works for a new user" {
            Expect.isTrue EditUser.empty.IsNew "An empty GUID should be flagged as a new user"
        }
        test "isNew works for an existing user" {
            Expect.isFalse { EditUser.empty with UserId = (Guid.NewGuid >> shortGuid) () }.IsNew
                "A non-empty GUID should not be flagged as a new user"
        }
        test "populateUser succeeds" {
            let edit =
                { EditUser.empty with
                    FirstName = "name"
                    LastName  = "eman"
                    Email     = "n@m.e"
                    IsAdmin   = Some true
                    Password  = "testpw"
                  }
            let hasher = fun x -> x + "+"
            let usr = edit.PopulateUser User.empty hasher
            Expect.equal usr.FirstName edit.FirstName "The first name was not populated correctly"
            Expect.equal usr.LastName edit.LastName "The last name was not populated correctly"
            Expect.equal usr.Email edit.Email "The e-mail address was not populated correctly"
            Expect.isTrue usr.IsAdmin "The isAdmin flag was not populated correctly"
            Expect.equal usr.PasswordHash (hasher edit.Password) "The password hash was not populated correctly"
        }
    ]

[<Tests>]
let groupLogOnTests =
    testList "GroupLogOn" [
        test "empty is as expected" {
            let mt = GroupLogOn.empty
            Expect.equal mt.SmallGroupId emptyGuid "The small group ID should be an empty GUID"
            Expect.equal mt.Password "" "The password should be blank"
            Expect.isNone mt.RememberMe "Remember Me should be None"
        }
    ]

[<Tests>]
let maintainRequestsTests =
    testList "MaintainRequests" [
        test "empty is as expected" {
            let mt = MaintainRequests.empty
            Expect.isEmpty mt.Requests "The requests for the model should have been empty"
            Expect.equal mt.SmallGroup.Id.Value Guid.Empty "The small group should have been an empty one"
            Expect.isNone mt.OnlyActive "The only active flag should have been None"
            Expect.isNone mt.SearchTerm "The search term should have been None"
            Expect.isNone mt.PageNbr "The page number should have been None"
        }
    ]

[<Tests>]
let messageLevelTests =
    testList "MessageLevel" [
        test "toString for Info is as expected" {
            Expect.equal (MessageLevel.toString Info) "Info" """The string value of "Info" is incorrect"""
        }
        test "toString for Warning is as expected" {
            Expect.equal (MessageLevel.toString Warning) "WARNING" """The string value of "Warning" is incorrect"""
        }
        test "toString for Error is as expected" {
            Expect.equal (MessageLevel.toString Error) "ERROR" """The string value of "Error" is incorrect"""
        }
        test "toCssClass for Info is as expected" {
            Expect.equal (MessageLevel.toCssClass Info) "info" """The string value of "Info" is incorrect"""
        }
        test "toCssClass for Warning is as expected" {
            Expect.equal (MessageLevel.toCssClass Warning) "warning" """The string value of "Warning" is incorrect"""
        }
        test "toCssClass for Error is as expected" {
            Expect.equal (MessageLevel.toCssClass Error) "error" """The string value of "Error" is incorrect"""
        }
    ]

[<Tests>]
let requestListTests =
    testList "RequestList" [
        let withRequestList f () =
            let today = SystemClock.Instance.GetCurrentInstant ()
            {   Requests   = [
                    { PrayerRequest.empty with
                        RequestType = CurrentRequest
                        Requestor   = Some "Zeb"
                        Text        = "zyx"
                        UpdatedDate = today
                    }
                    { PrayerRequest.empty with
                        RequestType = CurrentRequest
                        Requestor   = Some "Aaron"
                        Text        = "abc"
                        UpdatedDate = today - Duration.FromDays 9
                    }
                    { PrayerRequest.empty with
                        RequestType = PraiseReport
                        Text        = "nmo"
                        UpdatedDate = today
                    }
                ]
                Date       = today.InUtc().Date
                SmallGroup = SmallGroup.empty
                ShowHeader = false
                Recipients = []
                CanEmail   = false
            }
            |> f
        yield! testFixture withRequestList [
            "AsHtml succeeds without header or as-of date",
            fun reqList ->
                let htmlList = { reqList with SmallGroup = { reqList.SmallGroup with Name = "Test HTML Group" } }
                let html     = htmlList.AsHtml _s
                let fonts    = reqList.SmallGroup.Preferences.FontStack.Replace ("\"", "&quot;")
                Expect.equal -1 (html.IndexOf "Test HTML Group")
                    "The small group name should not have existed (no header)"
                let curReqHeading =
                    [ $"""<table style="font-family:{fonts};page-break-inside:avoid;">"""
                      "<tr>"
                      """<td style="font-size:16pt;color:maroon;padding:3px 0;border-top:solid 3px navy;border-bottom:solid 3px navy;font-weight:bold;">"""
                      "&nbsp; &nbsp; Current Requests&nbsp; &nbsp; </td></tr></table>"
                    ]
                    |> String.concat ""
                Expect.stringContains html curReqHeading """Heading for category "Current Requests" not found"""
                let curReqHtml =
                    [ $"""<ul style="font-family:{fonts};font-size:12pt">"""
                      """<li style="list-style-type:circle;padding-bottom:.25em;">"""
                      "<strong>Zeb</strong> &ndash; zyx</li>"
                      """<li style="list-style-type:disc;padding-bottom:.25em;">"""
                      "<strong>Aaron</strong> &ndash; abc</li></ul>"
                    ]
                    |> String.concat ""
                Expect.stringContains html curReqHtml """Expected HTML for "Current Requests" requests not found"""
                let praiseHeading =
                    [ $"""<table style="font-family:{fonts};page-break-inside:avoid;">"""
                      "<tr>"
                      """<td style="font-size:16pt;color:maroon;padding:3px 0;border-top:solid 3px navy;border-bottom:solid 3px navy;font-weight:bold;">"""
                      "&nbsp; &nbsp; Praise Reports&nbsp; &nbsp; </td></tr></table>"
                    ]
                    |> String.concat ""
                Expect.stringContains html praiseHeading """Heading for category "Praise Reports" not found"""
                let praiseHtml =
                    [ $"""<ul style="font-family:{fonts};font-size:12pt">"""
                      """<li style="list-style-type:circle;padding-bottom:.25em;">"""
                      "nmo</li></ul>"
                    ]
                    |> String.concat ""
                Expect.stringContains html praiseHtml """Expected HTML for "Praise Reports" requests not found"""
            "AsHtml succeeds with header",
            fun reqList ->
                let htmlList =
                    { reqList with
                        SmallGroup  = { reqList.SmallGroup with Name = "Test HTML Group" }
                        ShowHeader = true
                    }
                let html  = htmlList.AsHtml _s
                let fonts = reqList.SmallGroup.Preferences.FontStack.Replace ("\"", "&quot;")
                let lstHeading =
                    [ $"""<div style="text-align:center;font-family:{fonts}">"""
                      """<span style="font-size:16pt;"><strong>Prayer Requests</strong></span><br>"""
                      """<span style="font-size:12pt;"><strong>Test HTML Group</strong><br>"""
                      htmlList.Date.ToString ("MMMM d, yyyy", null)
                      "</span></div><br>"
                    ]
                    |> String.concat ""
                Expect.stringContains html lstHeading "Expected HTML for the list heading not found"
                // spot check; without header test tests this exhaustively
                Expect.stringContains html "<strong>Zeb</strong> &ndash; zyx</li>" "Expected requests not found"
            "AsHtml succeeds with short as-of date",
            fun reqList ->
                let htmlList =
                    { reqList with
                        SmallGroup =
                            { reqList.SmallGroup with
                                Preferences = { reqList.SmallGroup.Preferences with AsOfDateDisplay = ShortDate }
                            }
                    }
                let html     = htmlList.AsHtml _s
                let expected =
                    htmlList.Requests[0].UpdatedDate.InUtc().Date.ToString ("d", null)
                    |> sprintf """<strong>Zeb</strong> &ndash; zyx<i style="font-size:9.60pt">&nbsp; (as of %s)</i>"""
                // spot check; if one request has it, they all should
                Expect.stringContains html expected "Expected short as-of date not found"    
            "AsHtml succeeds with long as-of date",
            fun reqList ->
                let htmlList =
                    { reqList with
                        SmallGroup =
                            { reqList.SmallGroup with
                                Preferences = { reqList.SmallGroup.Preferences with AsOfDateDisplay = LongDate }
                            }
                    }
                let html     = htmlList.AsHtml _s
                let expected =
                    htmlList.Requests[0].UpdatedDate.InUtc().Date.ToString ("D", null)
                    |> sprintf """<strong>Zeb</strong> &ndash; zyx<i style="font-size:9.60pt">&nbsp; (as of %s)</i>"""
                // spot check; if one request has it, they all should
                Expect.stringContains html expected "Expected long as-of date not found"    
            "AsText succeeds with no as-of date",
            fun reqList ->
                let textList = { reqList with SmallGroup = { reqList.SmallGroup with Name = "Test Group" } }
                let text = textList.AsText _s
                Expect.stringContains text $"{textList.SmallGroup.Name}\n" "Small group name not found"
                Expect.stringContains text "Prayer Requests\n" "List heading not found"
                Expect.stringContains text ((textList.Date.ToString ("MMMM d, yyyy", null)) + "\n \n")
                    "List date not found"
                Expect.stringContains text "--------------------\n  CURRENT REQUESTS\n--------------------\n"
                    """Heading for category "Current Requests" not found"""
                Expect.stringContains text "  + Zeb - zyx\n" "First request not found"
                Expect.stringContains text "  - Aaron - abc\n \n"
                    "Second request not found; should have been end of category"
                Expect.stringContains text "------------------\n  PRAISE REPORTS\n------------------\n"
                    """Heading for category "Praise Reports" not found"""
                Expect.stringContains text "  + nmo\n \n" "Last request not found"
            "AsText succeeds with short as-of date",
            fun reqList ->
                let textList =
                    { reqList with
                        SmallGroup =
                            { reqList.SmallGroup with
                                Preferences = { reqList.SmallGroup.Preferences with AsOfDateDisplay = ShortDate }
                            }
                    }
                let text     = textList.AsText _s
                let expected =
                    textList.Requests[0].UpdatedDate.InUtc().Date.ToString ("d", null)
                    |> sprintf " + Zeb - zyx  (as of %s)"
                // spot check; if one request has it, they all should
                Expect.stringContains text expected "Expected short as-of date not found"    
            "AsText succeeds with long as-of date",
            fun reqList ->
                let textList =
                    { reqList with
                        SmallGroup =
                            { reqList.SmallGroup with
                                Preferences = { reqList.SmallGroup.Preferences with AsOfDateDisplay = LongDate }
                            }
                    }
                let text     = textList.AsText _s
                let expected =
                    textList.Requests[0].UpdatedDate.InUtc().Date.ToString ("D", null)
                    |> sprintf " + Zeb - zyx  (as of %s)"
                // spot check; if one request has it, they all should
                Expect.stringContains text expected "Expected long as-of date not found"    
            "IsNew succeeds for both old and new requests",
            fun reqList ->
                let allReqs = reqList.RequestsByType _s
                let _, _, reqs = allReqs |> List.find (fun (typ, _, _) -> typ = CurrentRequest)
                Expect.hasCountOf reqs 2u countAll "There should have been two requests"
                Expect.isTrue (reqList.IsNew (List.head reqs)) "The first request should have been new"
                Expect.isFalse (reqList.IsNew (List.last reqs)) "The second request should not have been new"
            "RequestsByType succeeds",
            fun reqList ->
                let allReqs = reqList.RequestsByType _s
                Expect.hasLength allReqs 2 "There should have been two types of request groupings"
                let maybeCurrent = allReqs |> List.tryFind (fun (typ, _, _) -> typ = CurrentRequest)
                Expect.isSome maybeCurrent "There should have been current requests"
                let _, _, reqs = Option.get maybeCurrent
                Expect.hasCountOf reqs 2u countAll "There should have been two requests"
                let first = List.head reqs
                Expect.equal first.Text "zyx" "The requests should be sorted by updated date descending"
                Expect.isTrue (allReqs |> List.exists (fun (typ, _, _) -> typ = PraiseReport))
                    "There should have been praise reports"
                Expect.isFalse (allReqs |> List.exists (fun (typ, _, _) -> typ = Announcement))
                    "There should not have been announcements"
            "RequestsByType succeeds and sorts by requestor",
            fun reqList ->
                let newList =
                    { reqList with
                        SmallGroup =
                            { reqList.SmallGroup with
                                Preferences = { reqList.SmallGroup.Preferences with RequestSort = SortByRequestor }
                            }
                    }
                let allReqs = newList.RequestsByType _s
                let _, _, reqs = allReqs |> List.find (fun (typ, _, _) -> typ = CurrentRequest)
                Expect.hasCountOf reqs 2u countAll "There should have been two requests"
                let first = List.head reqs
                Expect.equal first.Text "abc" "The requests should be sorted by requestor"
          ]
    ]

[<Tests>]
let userLogOnTests =
    testList "UserLogOn" [
        test "empty is as expected" {
            let mt = UserLogOn.empty
            Expect.equal mt.Email "" "The e-mail address should be blank"
            Expect.equal mt.Password "" "The password should be blank"
            Expect.equal mt.SmallGroupId emptyGuid "The small group ID should be an empty GUID"
            Expect.isNone mt.RememberMe "Remember Me should be None"
            Expect.isNone mt.RedirectUrl "Redirect URL should be None"
        }
    ]

[<Tests>]
let userMessageTests =
    testList "UserMessage" [
        test "Error is constructed properly" {
            let msg = UserMessage.error
            Expect.equal msg.Level Error "Incorrect message level"
            Expect.equal msg.Text HtmlString.Empty "Text should have been blank"
            Expect.isNone msg.Description "Description should have been None"
        }
        test "Warning is constructed properly" {
            let msg = UserMessage.warning
            Expect.equal msg.Level Warning "Incorrect message level"
            Expect.equal msg.Text HtmlString.Empty "Text should have been blank"
            Expect.isNone msg.Description "Description should have been None"
        }
        test "Info is constructed properly" {
            let msg = UserMessage.info
            Expect.equal msg.Level Info "Incorrect message level"
            Expect.equal msg.Text HtmlString.Empty "Text should have been blank"
            Expect.isNone msg.Description "Description should have been None"
        }
    ]
