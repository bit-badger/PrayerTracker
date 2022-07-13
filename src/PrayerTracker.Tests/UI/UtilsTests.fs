module PrayerTracker.UI.UtilsTests

open Expecto
open PrayerTracker

[<Tests>]
let ckEditorToTextTests =
    testList "ckEditorToText" [
        test "replaces newline/tab sequence with nothing" {
            Expect.equal (ckEditorToText "Here is some \n\ttext") "Here is some text"
                "Newline/tab sequence should have been removed"
        }
        test "replaces &nbsp; with a space" {
            Expect.equal (ckEditorToText "Test&nbsp;text") "Test text" "&nbsp; should have been replaced with a space"
        }
        test "replaces double space with one non-breaking space and one regular space" {
            Expect.equal (ckEditorToText "Test  text") "Test&#xa0; text"
                "double space should have been replaced with one non-breaking space and one regular space"
        }
        test "replaces paragraph break with two line breaks" {
            Expect.equal (ckEditorToText "some</p><p>text") "some<br><br>text"
                "paragraph break should have been replaced with two line breaks"
        }
        test "removes start and end paragraph tags" {
            Expect.equal (ckEditorToText "<p>something something</p>") "something something"
                "start/end paragraph tags should have been removed"
        }
        test "trims the result" {
            Expect.equal (ckEditorToText " abc ") "abc" "Should have trimmed the resulting text"
        }
        test "does all the replacements and removals at one time" {
            Expect.equal (ckEditorToText " <p>Paragraph&nbsp;1\n\t line two</p><p>Paragraph 2  x</p>")
                "Paragraph 1 line two<br><br>Paragraph 2&#xa0; x"
                "all replacements and removals were not made correctly"
        }
    ]

[<Tests>]
let htmlToPlainTextTests =
    testList "htmlToPlainText" [
        test "decodes HTML-encoded entities" {
            Expect.equal (htmlToPlainText "1 &gt; 0") "1 > 0" "HTML-encoded entities should have been decoded"
        }
        test "trims the input HTML" {
            Expect.equal (htmlToPlainText " howdy ") "howdy" "HTML input string should have been trimmed"
        }
        test "replaces line breaks with new lines" {
            Expect.equal (htmlToPlainText "Lots<br>of<br />new<br>lines") "Lots\nof\nnew\nlines"
                "Break tags should have been converted to newline characters"
        }
        test "replaces non-breaking spaces with spaces" {
            Expect.equal (htmlToPlainText "Here&nbsp;is&#xa0;some&nbsp;more&#xa0;text") "Here is some more text"
                "Non-breaking spaces should have been replaced with spaces"
        }
        test "does all replacements at one time" {
            Expect.equal (htmlToPlainText " &lt;&nbsp;&lt;<br>test") "< <\ntest"
                "All replacements were not made correctly"
        }
        test "does not fail when passed null" {
            Expect.equal (htmlToPlainText null) "" "Should return an empty string for null input"
        }
        test "does not fail when passed an empty string" {
            Expect.equal (htmlToPlainText "") "" "Should return an empty string when given an empty string"
        }
        test "preserves blank lines for two consecutive line breaks" {
            let expected = "Paragraph 1\n\nParagraph 2\n\n...and paragraph 3"
            Expect.equal
                (htmlToPlainText "Paragraph 1<br><br>Paragraph 2<br><br>...and <strong>paragraph</strong> <i>3</i>")
                expected "Blank lines not preserved for consecutive line breaks"
        }
    ]

[<Tests>]
let makeUrlTests =
    testList "makeUrl" [
        test "returns the URL when there are no parameters" {
            Expect.equal (makeUrl "/test" []) "/test" "The URL should not have had any query string parameters added"
        }
        test "returns the URL with one query string parameter" {
            Expect.equal (makeUrl "/test" [ "unit", "true" ]) "/test?unit=true" "The URL was not constructed properly"
        }
        test "returns the URL with multiple encoded query string parameters" {
            let url = makeUrl "/test" [ "space", "a space"; "turkey", "=" ]
            Expect.equal url "/test?space=a+space&turkey=%3D" "The URL was not constructed properly"
        }
    ]

[<Tests>]
let sndAsStringTests =
    testList "sndAsString" [
        test "converts the second item to a string" {
            Expect.equal (sndAsString ("a", 5)) "5"
                "The second part of the tuple should have been converted to a string"
        }
    ]

module StringTests =
  
    open PrayerTracker.Utils.String

    [<Tests>]
    let replaceFirstTests =
        testList "String.replaceFirst" [
            test "replaces the first occurrence when it is found at the beginning of the string" {
                let testString = "unit unit unit"
                Expect.equal (replaceFirst "unit" "test" testString) "test unit unit"
                    "First occurrence of a substring was not replaced properly at the beginning of the string"
            }
            test "replaces the first occurrence when it is found in the center of the string" {
                let testString = "test unit test"
                Expect.equal (replaceFirst "unit" "test" testString) "test test test"
                    "First occurrence of a substring was not replaced properly when it is in the center of the string"
            }
            test "returns the original string if the replacement isn't found" {
                let testString = "unit tests"
                Expect.equal (replaceFirst "tested" "testing" testString) "unit tests"
                    "String which did not have the target substring was not returned properly"
            }
        ]
   
    [<Tests>]
    let replaceTests =
        testList "String.replace" [
            test "succeeds" {
                Expect.equal (replace "a" "b" "abacab") "bbbcbb" "String did not replace properly"
            }
        ]

    [<Tests>]
    let trimTests =
        testList "String.trim" [
            test "succeeds" {
                Expect.equal (trim " abc ") "abc" "Space not trimmed from string properly"
            }
        ]

[<Tests>]
let stripTagsTests =
    let testString = "<p class=\"testing\">Here is some text<br> <br />and some more</p>"
    testList "stripTags" [
        test "does nothing if all tags are allowed" {
            Expect.equal (stripTags [ "p"; "br" ] testString) testString
                "There should have been no replacements in the target string"
        }
        test "strips the start/end tag for non allowed tag" {
            Expect.equal (stripTags [ "br" ] testString) "Here is some text<br> <br />and some more"
                "There should have been no \"p\" tag, but all \"br\" tags, in the returned string"
        }
        test "strips void/self-closing tags" {
            Expect.equal (stripTags [] testString) "Here is some text and some more"
                "There should have been no tags; all void and self-closing tags should have been stripped"
        }
    ]

[<Tests>]
let wordWrapTests =
    testList "wordWrap" [
        test "breaks where it is supposed to" {
            let testString = "The quick brown fox jumps over the lazy dog\nIt does!"
            Expect.equal (wordWrap 20 testString) "The quick brown fox\njumps over the lazy\ndog\nIt does!\n"
                "Line not broken correctly"
        }
        test "wraps long line without a space" {
            let testString = "Asamatteroffact, the dog does too"
            Expect.equal (wordWrap 10 testString) "Asamattero\nffact, the\ndog does\ntoo\n"
                "Longer line not broken correctly"
        }
        test "preserves blank lines" {
            let testString = "Here is\n\na string with blank lines"
            Expect.equal (wordWrap 80 testString) testString "Blank lines were not preserved"
        }
    ]

[<Tests>]
let wordWrapBTests =
    testList "wordWrapB" [
        test "breaks where it is supposed to" {
            let testString = "The quick brown fox jumps over the lazy dog\nIt does!"
            Expect.equal (wordWrap 20 testString) "The quick brown fox\njumps over the lazy\ndog\nIt does!\n"
                "Line not broken correctly"
        }
        test "wraps long line without a space and a line with exact length" {
            let testString = "Asamatteroffact, the dog does too"
            Expect.equal (wordWrap 10 testString) "Asamattero\nffact, the\ndog does\ntoo\n"
                "Longer line not broken correctly"
        }
        test "wraps long line without a space and a line with non-exact length" {
            let testString = "Asamatteroffact, that dog does too"
            Expect.equal (wordWrap 10 testString) "Asamattero\nffact,\nthat dog\ndoes too\n"
                "Longer line not broken correctly"
        }
        test "preserves blank lines" {
            let testString = "Here is\n\na string with blank lines"
            Expect.equal (wordWrap 80 testString) testString "Blank lines were not preserved"
        }
    ]
