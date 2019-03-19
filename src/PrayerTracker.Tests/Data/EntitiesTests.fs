module PrayerTracker.Entities.EntitiesTests

open Expecto
open NodaTime.Testing
open NodaTime
open System

[<Tests>]
let asOfDateDisplayTests =
  testList "AsOfDateDisplay" [
    test "NoDisplay code is correct" {
      Expect.equal NoDisplay.code "N" "The code for NoDisplay should have been \"N\""
      }
    test "ShortDate code is correct" {
      Expect.equal ShortDate.code "S" "The code for ShortDate should have been \"S\""
      }
    test "LongDate code is correct" {
      Expect.equal LongDate.code "L" "The code for LongDate should have been \"N\""
      }
    test "fromCode N should return NoDisplay" {
      Expect.equal (AsOfDateDisplay.fromCode "N") NoDisplay "\"N\" should have been converted to NoDisplay"
      }
    test "fromCode S should return ShortDate" {
      Expect.equal (AsOfDateDisplay.fromCode "S") ShortDate "\"S\" should have been converted to ShortDate"
      }
    test "fromCode L should return LongDate" {
      Expect.equal (AsOfDateDisplay.fromCode "L") LongDate "\"L\" should have been converted to LongDate"
      }
    test "fromCode X should raise" {
      Expect.throws (fun () -> AsOfDateDisplay.fromCode "X" |> ignore)
        "An unknown code should have raised an exception"
      }
    ]

[<Tests>]
let churchTests =
  testList "Church" [
    test "empty is as expected" {
      let mt = Church.empty
      Expect.equal mt.churchId Guid.Empty "The church ID should have been an empty GUID"
      Expect.equal mt.name "" "The name should have been blank"
      Expect.equal mt.city "" "The city should have been blank"
      Expect.equal mt.st "" "The state should have been blank"
      Expect.isFalse mt.hasInterface "The church should not show that it has an interface"
      Expect.isNone mt.interfaceAddress "The interface address should not exist"
      Expect.isNotNull mt.smallGroups "The small groups navigation property should not be null"
      Expect.isEmpty mt.smallGroups "There should be no small groups for an empty church"
      }
    ]

[<Tests>]
let emailFormatTests =
  testList "EmailFormat" [
    test "HtmlFormat code is correct" {
      Expect.equal HtmlFormat.code "H" "The code for HtmlFormat should have been \"H\""
      }
    test "PlainTextFormat code is correct" {
      Expect.equal PlainTextFormat.code "P" "The code for PlainTextFormat should have been \"P\""
      }
    test "fromCode H should return HtmlFormat" {
      Expect.equal (EmailFormat.fromCode "H") HtmlFormat "\"H\" should have been converted to HtmlFormat"
      }
    test "fromCode P should return ShortDate" {
      Expect.equal (EmailFormat.fromCode "P") PlainTextFormat "\"P\" should have been converted to PlainTextFormat"
      }
    test "fromCode Z should raise" {
      Expect.throws (fun () -> EmailFormat.fromCode "Z" |> ignore) "An unknown code should have raised an exception"
      }
    ]

[<Tests>]
let expirationTests =
  testList "Expiration" [
    test "Automatic code is correct" {
      Expect.equal Automatic.code "A" "The code for Automatic should have been \"A\""
      }
    test "Manual code is correct" {
      Expect.equal Manual.code "M" "The code for Manual should have been \"M\""
      }
    test "Forced code is correct" {
      Expect.equal Forced.code "F" "The code for Forced should have been \"F\""
      }
    test "fromCode A should return Automatic" {
      Expect.equal (Expiration.fromCode "A") Automatic "\"A\" should have been converted to Automatic"
      }
    test "fromCode M should return Manual" {
      Expect.equal (Expiration.fromCode "M") Manual "\"M\" should have been converted to Manual"
      }
    test "fromCode F should return Forced" {
      Expect.equal (Expiration.fromCode "F") Forced "\"F\" should have been converted to Forced"
      }
    test "fromCode V should raise" {
      Expect.throws (fun () -> Expiration.fromCode "V" |> ignore) "An unknown code should have raised an exception"
      }
    ]

[<Tests>]
let listPreferencesTests =
  testList "ListPreferences" [
    test "empty is as expected" {
      let mt = ListPreferences.empty
      Expect.equal mt.smallGroupId Guid.Empty "The small group ID should have been an empty GUID"
      Expect.equal mt.daysToExpire 14 "The default days to expire should have been 14"
      Expect.equal mt.daysToKeepNew 7 "The default days to keep new should have been 7"
      Expect.equal mt.longTermUpdateWeeks 4 "The default long term update weeks should have been 4"
      Expect.equal mt.emailFromName "PrayerTracker" "The default e-mail from name should have been PrayerTracker"
      Expect.equal mt.emailFromAddress "prayer@djs-consulting.com"
        "The default e-mail from address should have been prayer@djs-consulting.com"
      Expect.equal mt.listFonts "Century Gothic,Tahoma,Luxi Sans,sans-serif" "The default list fonts were incorrect"
      Expect.equal mt.headingColor "maroon" "The default heading text color should have been maroon"
      Expect.equal mt.lineColor "navy" "The default heding line color should have been navy"
      Expect.equal mt.headingFontSize 16 "The default heading font size should have been 16"
      Expect.equal mt.textFontSize 12 "The default text font size should have been 12"
      Expect.equal mt.requestSort SortByDate "The default request sort should have been by date"
      Expect.equal mt.groupPassword "" "The default group password should have been blank"
      Expect.equal mt.defaultEmailType HtmlFormat "The default e-mail type should have been HTML"
      Expect.isFalse mt.isPublic "The isPublic flag should not have been set"
      Expect.equal mt.timeZoneId "America/Denver" "The default time zone should have been America/Denver"
      Expect.equal mt.timeZone.timeZoneId "" "The default preferences should have included an empty time zone"
      Expect.equal mt.pageSize 100 "The default page size should have been 100"
      Expect.equal mt.asOfDateDisplay NoDisplay "The as-of date display should have been No Display"
      }
    ]

[<Tests>]
let memberTests =
  testList "Member" [
    test "empty is as expected" {
      let mt = Member.empty
      Expect.equal mt.memberId Guid.Empty "The member ID should have been an empty GUID"
      Expect.equal mt.smallGroupId Guid.Empty "The small group ID should have been an empty GUID"
      Expect.equal mt.memberName "" "The member name should have been blank"
      Expect.equal mt.email "" "The member e-mail address should have been blank"
      Expect.isNone mt.format "The preferred e-mail format should not exist"
      Expect.equal mt.smallGroup.smallGroupId Guid.Empty "The small group should have been an empty one"
      }
    ]

[<Tests>]
let prayerRequestTests =
  testList "PrayerRequest" [
    test "empty is as expected" {
      let mt = PrayerRequest.empty
      Expect.equal mt.prayerRequestId Guid.Empty "The request ID should have been an empty GUID"
      Expect.equal mt.requestType CurrentRequest "The request type should have been Current"
      Expect.equal mt.userId Guid.Empty "The user ID should have been an empty GUID"
      Expect.equal mt.smallGroupId Guid.Empty "The small group ID should have been an empty GUID"
      Expect.equal mt.enteredDate DateTime.MinValue "The entered date should have been the minimum"
      Expect.equal mt.updatedDate DateTime.MinValue "The updated date should have been the minimum"
      Expect.isNone mt.requestor "The requestor should not exist"
      Expect.equal mt.text "" "The request text should have been blank"
      Expect.isFalse mt.notifyChaplain "The notify chaplain flag should not have been set"
      Expect.equal mt.expiration Automatic "The expiration should have been Automatic"
      Expect.equal mt.user.userId Guid.Empty "The user should have been an empty one"
      Expect.equal mt.smallGroup.smallGroupId Guid.Empty "The small group should have been an empty one"
      }
    test "isExpired always returns false for expecting requests" {
      let req = { PrayerRequest.empty with requestType = Expecting }
      Expect.isFalse (req.isExpired DateTime.Now 0) "An expecting request should never be considered expired"
      }
    test "isExpired always returns false for manually-expired requests" {
      let req = { PrayerRequest.empty with updatedDate = DateTime.Now.AddMonths -1; expiration = Manual }
      Expect.isFalse (req.isExpired DateTime.Now 4) "A never-expired request should never be considered expired"
      }
    test "isExpired always returns false for long term/recurring requests" {
      let req = { PrayerRequest.empty with requestType = LongTermRequest }
      Expect.isFalse (req.isExpired DateTime.Now 0) "A recurring/long-term request should never be considered expired"
      }
    test "isExpired always returns true for force-expired requests" {
      let req = { PrayerRequest.empty with updatedDate = DateTime.Now; expiration = Forced }
      Expect.isTrue (req.isExpired DateTime.Now 5) "A force-expired request should always be considered expired"
      }
    test "isExpired returns false for non-expired requests" {
      let req = { PrayerRequest.empty with updatedDate = DateTime.Now.AddDays -5. }
      Expect.isFalse (req.isExpired DateTime.Now 7) "A request updated 5 days ago should not be considered expired"
      }
    test "isExpired returns true for expired requests" {
      let req = { PrayerRequest.empty with updatedDate = DateTime.Now.AddDays -8. }
      Expect.isTrue (req.isExpired DateTime.Now 7) "A request updated 8 days ago should be considered expired"
      }
    test "updateRequired returns false for expired requests" {
      let req = { PrayerRequest.empty with expiration = Forced }
      Expect.isFalse (req.updateRequired DateTime.Now 7 4) "An expired request should not require an update"
      }
    test "updateRequired returns false when an update is not required for an active request" {
      let req =
        { PrayerRequest.empty with
            requestType = LongTermRequest
            updatedDate = DateTime.Now.AddDays -14.
          }
      Expect.isFalse (req.updateRequired DateTime.Now 7 4)
        "An active request updated 14 days ago should not require an update until 28 days"
      }
    test "updateRequired returns true when an update is required for an active request" {
      let req =
        { PrayerRequest.empty with
            requestType = LongTermRequest
            updatedDate = DateTime.Now.AddDays -34.
          }
      Expect.isTrue (req.updateRequired DateTime.Now 7 4)
        "An active request updated 34 days ago should require an update (past 28 days)"
      }
    ]

[<Tests>]
let prayerRequestTypeTests =
  testList "PrayerRequestType" [
    test "CurrentRequest code is correct" {
      Expect.equal CurrentRequest.code "C" "The code for CurrentRequest should have been \"C\""
      }
    test "LongTermRequest code is correct" {
      Expect.equal LongTermRequest.code "L" "The code for LongTermRequest should have been \"L\""
      }
    test "PraiseReport code is correct" {
      Expect.equal PraiseReport.code "P" "The code for PraiseReport should have been \"P\""
      }
    test "Expecting code is correct" {
      Expect.equal Expecting.code "E" "The code for Expecting should have been \"E\""
      }
    test "Announcement code is correct" {
      Expect.equal Announcement.code "A" "The code for Announcement should have been \"A\""
      }
    test "fromCode C should return CurrentRequest" {
      Expect.equal (PrayerRequestType.fromCode "C") CurrentRequest
        "\"C\" should have been converted to CurrentRequest"
      }
    test "fromCode L should return LongTermRequest" {
      Expect.equal (PrayerRequestType.fromCode "L") LongTermRequest
        "\"L\" should have been converted to LongTermRequest"
      }
    test "fromCode P should return PraiseReport" {
      Expect.equal (PrayerRequestType.fromCode "P") PraiseReport "\"P\" should have been converted to PraiseReport"
      }
    test "fromCode E should return Expecting" {
      Expect.equal (PrayerRequestType.fromCode "E") Expecting "\"E\" should have been converted to Expecting"
      }
    test "fromCode A should return Announcement" {
      Expect.equal (PrayerRequestType.fromCode "A") Announcement "\"A\" should have been converted to Announcement"
      }
    test "fromCode R should raise" {
      Expect.throws (fun () -> PrayerRequestType.fromCode "R" |> ignore)
        "An unknown code should have raised an exception"
      }
    ]

[<Tests>]
let requestSortTests =
  testList "RequestSort" [
    test "SortByDate code is correct" {
      Expect.equal SortByDate.code "D" "The code for SortByDate should have been \"D\""
      }
    test "SortByRequestor code is correct" {
      Expect.equal SortByRequestor.code "R" "The code for SortByRequestor should have been \"R\""
      }
    test "fromCode D should return SortByDate" {
      Expect.equal (RequestSort.fromCode "D") SortByDate "\"D\" should have been converted to SortByDate"
      }
    test "fromCode R should return SortByRequestor" {
      Expect.equal (RequestSort.fromCode "R") SortByRequestor "\"R\" should have been converted to SortByRequestor"
      }
    test "fromCode Q should raise" {
      Expect.throws (fun () -> RequestSort.fromCode "Q" |> ignore) "An unknown code should have raised an exception"
      }
    ]

[<Tests>]
let smallGroupTests =
  testList "SmallGroup" [
    let now = DateTime (2017, 5, 12, 12, 15, 0, DateTimeKind.Utc)
    let withFakeClock f () =
      FakeClock (Instant.FromDateTimeUtc now) |> f
    yield test "empty is as expected" {
      let mt = SmallGroup.empty
      Expect.equal mt.smallGroupId Guid.Empty "The small group ID should have been an empty GUID"
      Expect.equal mt.churchId Guid.Empty "The church ID should have been an empty GUID"
      Expect.equal mt.name "" "The name should have been blank"
      Expect.equal mt.church.churchId Guid.Empty "The church should have been an empty one"
      Expect.isNotNull mt.members "The members navigation property should not be null"
      Expect.isEmpty mt.members "There should be no members for an empty small group"
      Expect.isNotNull mt.prayerRequests "The prayer requests navigation property should not be null"
      Expect.isEmpty mt.prayerRequests "There should be no prayer requests for an empty small group"
      Expect.isNotNull mt.users "The users navigation property should not be null"
      Expect.isEmpty mt.users "There should be no users for an empty small group"
      }
    yield! testFixture withFakeClock [
      "localTimeNow adjusts the time ahead of UTC",
      fun clock ->
          let grp = { SmallGroup.empty with preferences = { ListPreferences.empty with timeZoneId = "Europe/Berlin" } }
          Expect.isGreaterThan (grp.localTimeNow clock) now "UTC to Europe/Berlin should have added hours"
      "localTimeNow adjusts the time behind UTC",
      fun clock ->
          Expect.isLessThan (SmallGroup.empty.localTimeNow clock) now
            "UTC to America/Denver should have subtracted hours"
      "localTimeNow returns UTC when the time zone is invalid",
      fun clock ->
          let grp = { SmallGroup.empty with preferences = { ListPreferences.empty with timeZoneId = "garbage" } }
          Expect.equal (grp.localTimeNow clock) now "UTC should have been returned for an invalid time zone"
      ]
    yield test "localTimeNow fails when clock is not passed" {
      Expect.throws (fun () -> (SmallGroup.empty.localTimeNow >> ignore) null)
        "Should have raised an exception for null clock"
      }
    yield test "localDateNow returns the date portion" {
      let now'  = DateTime (2017, 5, 12, 1, 15, 0, DateTimeKind.Utc)
      let clock = FakeClock (Instant.FromDateTimeUtc now')
      Expect.isLessThan (SmallGroup.empty.localDateNow clock) now.Date "The date should have been a day earlier"
      }
    ]

[<Tests>]
let timeZoneTests =
  testList "TimeZone" [
    test "empty is as expected" {
      let mt = TimeZone.empty
      Expect.equal mt.timeZoneId "" "The time zone ID should have been blank"
      Expect.equal mt.description "" "The description should have been blank"
      Expect.equal mt.sortOrder 0 "The sort order should have been zero"
      Expect.isFalse mt.isActive "The is-active flag should not have been set"
      }
    ]

[<Tests>]
let userTests =
  testList "User" [
    test "empty is as expected" {
      let mt = User.empty
      Expect.equal mt.userId Guid.Empty "The user ID should have been an empty GUID"
      Expect.equal mt.firstName "" "The first name should have been blank"
      Expect.equal mt.lastName "" "The last name should have been blank"
      Expect.equal mt.emailAddress "" "The e-mail address should have been blank"
      Expect.isFalse mt.isAdmin "The is admin flag should not have been set"
      Expect.equal mt.passwordHash "" "The password hash should have been blank"
      Expect.isNone mt.salt "The password salt should not exist"
      Expect.isNotNull mt.smallGroups "The small groups navigation property should not have been null"
      Expect.isEmpty mt.smallGroups "There should be no small groups for an empty user"
      }
    test "fullName concatenates first and last names" {
      let user = { User.empty with firstName = "Unit"; lastName = "Test" }
      Expect.equal user.fullName "Unit Test" "The full name should be the first and last, separated by a space"
      }
    ]

[<Tests>]
let userSmallGroupTests =
  testList "UserSmallGroup" [
    test "empty is as expected" {
      let mt = UserSmallGroup.empty
      Expect.equal mt.userId Guid.Empty "The user ID should have been an empty GUID"
      Expect.equal mt.smallGroupId Guid.Empty "The small group ID should have been an empty GUID"
      Expect.equal mt.user.userId Guid.Empty "The user should have been an empty one"
      Expect.equal mt.smallGroup.smallGroupId Guid.Empty "The small group should have been an empty one"
      }
    ]
