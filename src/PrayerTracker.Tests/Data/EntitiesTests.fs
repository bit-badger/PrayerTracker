module PrayerTracker.Entities.EntitiesTests

open Expecto
open NodaTime.Testing
open NodaTime
open System

[<Tests>]
let asOfDateDisplayTests =
    testList "AsOfDateDisplay" [
        test "NoDisplay code is correct" {
            Expect.equal (AsOfDateDisplay.toCode NoDisplay) "N" "The code for NoDisplay should have been \"N\""
        }
        test "ShortDate code is correct" {
            Expect.equal (AsOfDateDisplay.toCode ShortDate) "S" "The code for ShortDate should have been \"S\""
        }
        test "LongDate code is correct" {
            Expect.equal (AsOfDateDisplay.toCode LongDate) "L" "The code for LongDate should have been \"N\""
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
            Expect.equal mt.Id.Value Guid.Empty "The church ID should have been an empty GUID"
            Expect.equal mt.Name "" "The name should have been blank"
            Expect.equal mt.City "" "The city should have been blank"
            Expect.equal mt.State "" "The state should have been blank"
            Expect.isFalse mt.HasVpsInterface "The church should not show that it has an interface"
            Expect.isNone mt.InterfaceAddress "The interface address should not exist"
        }
    ]

[<Tests>]
let emailFormatTests =
    testList "EmailFormat" [
        test "HtmlFormat code is correct" {
            Expect.equal (EmailFormat.toCode HtmlFormat) "H" "The code for HtmlFormat should have been \"H\""
        }
        test "PlainTextFormat code is correct" {
            Expect.equal (EmailFormat.toCode PlainTextFormat) "P" "The code for PlainTextFormat should have been \"P\""
        }
        test "fromCode H should return HtmlFormat" {
            Expect.equal (EmailFormat.fromCode "H") HtmlFormat "\"H\" should have been converted to HtmlFormat"
        }
        test "fromCode P should return ShortDate" {
            Expect.equal (EmailFormat.fromCode "P") PlainTextFormat
                "\"P\" should have been converted to PlainTextFormat"
        }
        test "fromCode Z should raise" {
            Expect.throws (fun () -> EmailFormat.fromCode "Z" |> ignore)
                "An unknown code should have raised an exception"
        }
    ]

[<Tests>]
let expirationTests =
    testList "Expiration" [
        test "Automatic code is correct" {
            Expect.equal (Expiration.toCode Automatic) "A" "The code for Automatic should have been \"A\""
        }
        test "Manual code is correct" {
            Expect.equal (Expiration.toCode Manual) "M" "The code for Manual should have been \"M\""
        }
        test "Forced code is correct" {
            Expect.equal (Expiration.toCode Forced) "F" "The code for Forced should have been \"F\""
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
            Expect.throws (fun () -> Expiration.fromCode "V" |> ignore)
                "An unknown code should have raised an exception"
        }
    ]

[<Tests>]
let listPreferencesTests =
    testList "ListPreferences" [
        test "FontStack is correct for native fonts" {
            Expect.equal ListPreferences.empty.FontStack
                """system-ui,-apple-system,"Segoe UI",Roboto,Ubuntu,"Liberation Sans",Cantarell,"Helvetica Neue",sans-serif"""
                "The expected native font stack was incorrect"
        }
        test "FontStack is correct for specific fonts" {
            Expect.equal { ListPreferences.empty with Fonts = "Arial,sans-serif" }.FontStack "Arial,sans-serif"
                "The specified fonts were not returned correctly"
        }
        test "empty is as expected" {
            let mt = ListPreferences.empty
            Expect.equal mt.SmallGroupId.Value Guid.Empty "The small group ID should have been an empty GUID"
            Expect.equal mt.DaysToExpire 14 "The default days to expire should have been 14"
            Expect.equal mt.DaysToKeepNew 7 "The default days to keep new should have been 7"
            Expect.equal mt.LongTermUpdateWeeks 4 "The default long term update weeks should have been 4"
            Expect.equal mt.EmailFromName "PrayerTracker" "The default e-mail from name should have been PrayerTracker"
            Expect.equal mt.EmailFromAddress "prayer@bitbadger.solutions"
                "The default e-mail from address should have been prayer@bitbadger.solutions"
            Expect.equal mt.Fonts "native" "The default list fonts were incorrect"
            Expect.equal mt.HeadingColor "maroon" "The default heading text color should have been maroon"
            Expect.equal mt.LineColor "navy" "The default heading line color should have been navy"
            Expect.equal mt.HeadingFontSize 16 "The default heading font size should have been 16"
            Expect.equal mt.TextFontSize 12 "The default text font size should have been 12"
            Expect.equal mt.RequestSort SortByDate "The default request sort should have been by date"
            Expect.equal mt.GroupPassword "" "The default group password should have been blank"
            Expect.equal mt.DefaultEmailType HtmlFormat "The default e-mail type should have been HTML"
            Expect.isFalse mt.IsPublic "The isPublic flag should not have been set"
            Expect.equal (TimeZoneId.toString mt.TimeZoneId) "America/Denver"
                "The default time zone should have been America/Denver"
            Expect.equal mt.PageSize 100 "The default page size should have been 100"
            Expect.equal mt.AsOfDateDisplay NoDisplay "The as-of date display should have been No Display"
        }
    ]

[<Tests>]
let memberTests =
    testList "Member" [
        test "empty is as expected" {
            let mt = Member.empty
            Expect.equal mt.Id.Value Guid.Empty "The member ID should have been an empty GUID"
            Expect.equal mt.SmallGroupId.Value Guid.Empty "The small group ID should have been an empty GUID"
            Expect.equal mt.Name "" "The member name should have been blank"
            Expect.equal mt.Email "" "The member e-mail address should have been blank"
            Expect.isNone mt.Format "The preferred e-mail format should not exist"
        }
    ]

[<Tests>]
let prayerRequestTests =
    let instantNow      = SystemClock.Instance.GetCurrentInstant
    let localDateNow () = (instantNow ()).InUtc().Date
    testList "PrayerRequest" [
        test "empty is as expected" {
            let mt = PrayerRequest.empty
            Expect.equal mt.Id.Value Guid.Empty "The request ID should have been an empty GUID"
            Expect.equal mt.RequestType CurrentRequest "The request type should have been Current"
            Expect.equal mt.UserId.Value Guid.Empty "The user ID should have been an empty GUID"
            Expect.equal mt.SmallGroupId.Value Guid.Empty "The small group ID should have been an empty GUID"
            Expect.equal mt.EnteredDate Instant.MinValue "The entered date should have been the minimum"
            Expect.equal mt.UpdatedDate Instant.MinValue "The updated date should have been the minimum"
            Expect.isNone mt.Requestor "The requestor should not exist"
            Expect.equal mt.Text "" "The request text should have been blank"
            Expect.isFalse mt.NotifyChaplain "The notify chaplain flag should not have been set"
            Expect.equal mt.Expiration Automatic "The expiration should have been Automatic"
        }
        test "isExpired always returns false for expecting requests" {
            PrayerRequest.isExpired (localDateNow ()) SmallGroup.empty
                { PrayerRequest.empty with RequestType = Expecting }
            |> Flip.Expect.isFalse "An expecting request should never be considered expired"
        }
        test "isExpired always returns false for manually-expired requests" {
            PrayerRequest.isExpired (localDateNow ()) SmallGroup.empty 
                { PrayerRequest.empty with UpdatedDate = (instantNow ()) - Duration.FromDays 1; Expiration = Manual }
            |> Flip.Expect.isFalse "A never-expired request should never be considered expired"
        }
        test "isExpired always returns false for long term/recurring requests" {
            PrayerRequest.isExpired (localDateNow ()) SmallGroup.empty
                { PrayerRequest.empty with RequestType = LongTermRequest }
            |> Flip.Expect.isFalse "A recurring/long-term request should never be considered expired"
        }
        test "isExpired always returns true for force-expired requests" {
            PrayerRequest.isExpired (localDateNow ()) SmallGroup.empty
                { PrayerRequest.empty with UpdatedDate = (instantNow ()); Expiration = Forced }
            |> Flip.Expect.isTrue "A force-expired request should always be considered expired"
        }
        test "isExpired returns false for non-expired requests" {
            let now = instantNow ()
            PrayerRequest.isExpired (now.InUtc().Date) SmallGroup.empty
                { PrayerRequest.empty with UpdatedDate = now - Duration.FromDays 5 }
            |> Flip.Expect.isFalse "A request updated 5 days ago should not be considered expired"
        }
        test "isExpired returns true for expired requests" {
            let now = instantNow ()
            PrayerRequest.isExpired (now.InUtc().Date) SmallGroup.empty
                { PrayerRequest.empty with UpdatedDate = now - Duration.FromDays 15 }
            |> Flip.Expect.isTrue "A request updated 15 days ago should be considered expired"
        }
        test "isExpired returns true for same-day expired requests" {
            let now = instantNow ()
            PrayerRequest.isExpired (now.InUtc().Date) SmallGroup.empty
                { PrayerRequest.empty with UpdatedDate = now - (Duration.FromDays 14) - (Duration.FromSeconds 1L) }
            |> Flip.Expect.isTrue  "A request entered a second before midnight should be considered expired"
        }
        test "updateRequired returns false for expired requests" {
            PrayerRequest.updateRequired (localDateNow ()) SmallGroup.empty
                { PrayerRequest.empty with Expiration = Forced }
            |> Flip.Expect.isFalse "An expired request should not require an update"
        }
        test "updateRequired returns false when an update is not required for an active request" {
            let now = instantNow ()
            PrayerRequest.updateRequired (localDateNow ()) SmallGroup.empty
                { PrayerRequest.empty with RequestType = LongTermRequest; UpdatedDate = now - Duration.FromDays 14 }
            |> Flip.Expect.isFalse "An active request updated 14 days ago should not require an update until 28 days"
        }
        test "UpdateRequired returns true when an update is required for an active request" {
            let now = instantNow ()
            PrayerRequest.updateRequired (localDateNow ()) SmallGroup.empty
                { PrayerRequest.empty with RequestType = LongTermRequest; UpdatedDate = now - Duration.FromDays 34 }
            |> Flip.Expect.isTrue "An active request updated 34 days ago should require an update (past 28 days)"
        }
    ]

[<Tests>]
let prayerRequestTypeTests =
    testList "PrayerRequestType" [
        test "CurrentRequest code is correct" {
            Expect.equal (PrayerRequestType.toCode CurrentRequest) "C"
                "The code for CurrentRequest should have been \"C\""
        }
        test "LongTermRequest code is correct" {
            Expect.equal (PrayerRequestType.toCode LongTermRequest) "L"
                "The code for LongTermRequest should have been \"L\""
        }
        test "PraiseReport code is correct" {
            Expect.equal (PrayerRequestType.toCode PraiseReport) "P" "The code for PraiseReport should have been \"P\""
        }
        test "Expecting code is correct" {
            Expect.equal (PrayerRequestType.toCode Expecting) "E" "The code for Expecting should have been \"E\""
        }
        test "Announcement code is correct" {
            Expect.equal (PrayerRequestType.toCode Announcement) "A" "The code for Announcement should have been \"A\""
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
            Expect.equal (PrayerRequestType.fromCode "P") PraiseReport
                "\"P\" should have been converted to PraiseReport"
        }
        test "fromCode E should return Expecting" {
            Expect.equal (PrayerRequestType.fromCode "E") Expecting "\"E\" should have been converted to Expecting"
        }
        test "fromCode A should return Announcement" {
            Expect.equal (PrayerRequestType.fromCode "A") Announcement
                "\"A\" should have been converted to Announcement"
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
            Expect.equal (RequestSort.toCode SortByDate) "D" "The code for SortByDate should have been \"D\""
        }
        test "SortByRequestor code is correct" {
            Expect.equal (RequestSort.toCode SortByRequestor) "R" "The code for SortByRequestor should have been \"R\""
        }
        test "fromCode D should return SortByDate" {
            Expect.equal (RequestSort.fromCode "D") SortByDate "\"D\" should have been converted to SortByDate"
        }
        test "fromCode R should return SortByRequestor" {
            Expect.equal (RequestSort.fromCode "R") SortByRequestor
                "\"R\" should have been converted to SortByRequestor"
        }
        test "fromCode Q should raise" {
            Expect.throws (fun () -> RequestSort.fromCode "Q" |> ignore)
                "An unknown code should have raised an exception"
        }
    ]

[<Tests>]
let smallGroupTests =
    testList "SmallGroup" [
        let now = Instant.FromDateTimeUtc (DateTime (2017, 5, 12, 12, 15, 0, DateTimeKind.Utc))
        let withFakeClock f () =
            FakeClock now |> f
        yield test "empty is as expected" {
            let mt = SmallGroup.empty
            Expect.equal mt.Id.Value Guid.Empty "The small group ID should have been an empty GUID"
            Expect.equal mt.ChurchId.Value Guid.Empty "The church ID should have been an empty GUID"
            Expect.equal mt.Name "" "The name should have been blank"
        }
        yield! testFixture withFakeClock [
            "LocalTimeNow adjusts the time ahead of UTC",
            fun clock ->
                let grp =
                    { SmallGroup.empty with
                        Preferences = { ListPreferences.empty with TimeZoneId = TimeZoneId "Europe/Berlin" }
                    }
                Expect.isGreaterThan (SmallGroup.localTimeNow clock grp) (now.InUtc().LocalDateTime)
                    "UTC to Europe/Berlin should have added hours"
            "LocalTimeNow adjusts the time behind UTC",
            fun clock ->
                Expect.isLessThan (SmallGroup.localTimeNow clock SmallGroup.empty) (now.InUtc().LocalDateTime)
                    "UTC to America/Denver should have subtracted hours"
            "LocalTimeNow returns UTC when the time zone is invalid",
            fun clock ->
                let grp =
                    { SmallGroup.empty with
                        Preferences = { ListPreferences.empty with TimeZoneId = TimeZoneId "garbage" }
                    }
                Expect.equal (SmallGroup.localTimeNow clock grp) (now.InUtc().LocalDateTime)
                    "UTC should have been returned for an invalid time zone"
        ]
        yield test "localTimeNow fails when clock is not passed" {
            Expect.throws (fun () -> (SmallGroup.localTimeNow null SmallGroup.empty |> ignore))
                "Should have raised an exception for null clock"
        }
        yield test "LocalDateNow returns the date portion" {
            let clock = FakeClock (Instant.FromDateTimeUtc (DateTime (2017, 5, 12, 1, 15, 0, DateTimeKind.Utc)))
            Expect.isLessThan (SmallGroup.localDateNow clock SmallGroup.empty) (now.InUtc().Date)
                "The date should have been a day earlier"
        }
    ]

[<Tests>]
let userTests =
    testList "User" [
        test "empty is as expected" {
            let mt = User.empty
            Expect.equal mt.Id.Value Guid.Empty "The user ID should have been an empty GUID"
            Expect.equal mt.FirstName "" "The first name should have been blank"
            Expect.equal mt.LastName "" "The last name should have been blank"
            Expect.equal mt.Email "" "The e-mail address should have been blank"
            Expect.isFalse mt.IsAdmin "The is admin flag should not have been set"
            Expect.equal mt.PasswordHash "" "The password hash should have been blank"
        }
        test "Name concatenates first and last names" {
            let user = { User.empty with FirstName = "Unit"; LastName = "Test" }
            Expect.equal user.Name "Unit Test" "The full name should be the first and last, separated by a space"
        }
    ]

[<Tests>]
let userSmallGroupTests =
    testList "UserSmallGroup" [
        test "empty is as expected" {
            let mt = UserSmallGroup.empty
            Expect.equal mt.UserId.Value Guid.Empty "The user ID should have been an empty GUID"
            Expect.equal mt.SmallGroupId.Value Guid.Empty "The small group ID should have been an empty GUID"
        }
    ]
