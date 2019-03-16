module PrayerTracker.Entities.EntitiesTests

open Expecto
open NodaTime.Testing
open NodaTime
open System

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
      Expect.equal mt.requestSort "D" "The default request sort should have been D (date)"
      Expect.equal mt.groupPassword "" "The default group password should have been blank"
      Expect.equal mt.defaultEmailType EmailType.Html "The default e-mail type should have been HTML"
      Expect.isFalse mt.isPublic "The isPublic flag should not have been set"
      Expect.equal mt.timeZoneId "America/Denver" "The default time zone should have been America/Denver"
      Expect.equal mt.timeZone.timeZoneId "" "The default preferences should have included an empty time zone"
      Expect.equal mt.pageSize 100 "The default page size should have been 100"
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
      Expect.equal mt.requestType RequestType.Current "The request type should have been Current"
      Expect.equal mt.userId Guid.Empty "The user ID should have been an empty GUID"
      Expect.equal mt.smallGroupId Guid.Empty "The small group ID should have been an empty GUID"
      Expect.equal mt.enteredDate DateTime.MinValue "The entered date should have been the minimum"
      Expect.equal mt.updatedDate DateTime.MinValue "The updated date should have been the minimum"
      Expect.isNone mt.requestor "The requestor should not exist"
      Expect.equal mt.text "" "The request text should have been blank"
      Expect.isFalse mt.doNotExpire "The do not expire flag should not have been set"
      Expect.isFalse mt.notifyChaplain "The notify chaplain flag should not have been set"
      Expect.isFalse mt.isManuallyExpired "The is manually expired flag should not have been set"
      Expect.equal mt.user.userId Guid.Empty "The user should have been an empty one"
      Expect.equal mt.smallGroup.smallGroupId Guid.Empty "The small group should have been an empty one"
      }
    test "isExpired always returns false for expecting requests" {
      let req = { PrayerRequest.empty with requestType = RequestType.Expecting }
      Expect.isFalse (req.isExpired DateTime.Now 0) "An expecting request should never be considered expired"
      }
    test "isExpired always returns false for never-expired requests" {
      let req = { PrayerRequest.empty with updatedDate = DateTime.Now.AddMonths -1; doNotExpire = true }
      Expect.isFalse (req.isExpired DateTime.Now 4) "A never-expired request should never be considered expired"
      }
    test "isExpired always returns false for recurring requests" {
      let req = { PrayerRequest.empty with requestType = RequestType.Recurring }
      Expect.isFalse (req.isExpired DateTime.Now 0) "A recurring/long-term request should never be considered expired"
      }
    test "isExpired always returns true for manually expired requests" {
      let req = { PrayerRequest.empty with updatedDate = DateTime.Now; isManuallyExpired = true }
      Expect.isTrue (req.isExpired DateTime.Now 5) "A manually expired request should always be considered expired"
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
      let req = { PrayerRequest.empty with isManuallyExpired = true }
      Expect.isFalse (req.updateRequired DateTime.Now 7 4) "An expired request should not require an update"
      }
    test "updateRequired returns false when an update is not required for an active request" {
      let req =
        { PrayerRequest.empty with
            requestType = RequestType.Recurring
            updatedDate = DateTime.Now.AddDays -14.
          }
      Expect.isFalse (req.updateRequired DateTime.Now 7 4)
        "An active request updated 14 days ago should not require an update until 28 days"
      }
    test "updateRequired returns true when an update is required for an active request" {
      let req =
        { PrayerRequest.empty with
            requestType = RequestType.Recurring
            updatedDate = DateTime.Now.AddDays -34.
          }
      Expect.isTrue (req.updateRequired DateTime.Now 7 4)
        "An active request updated 34 days ago should require an update (past 28 days)"
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
