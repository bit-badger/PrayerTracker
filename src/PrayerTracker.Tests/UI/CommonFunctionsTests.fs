module PrayerTracker.UI.CommonFunctionsTests

open System.IO
open Expecto
open Giraffe.ViewEngine
open Microsoft.AspNetCore.Mvc.Localization
open Microsoft.Extensions.Localization
open PrayerTracker.Tests.TestLocalization
open PrayerTracker.Views


[<Tests>]
let iconSizedTests =
    testList "iconSized" [
        test "succeeds" {
            let ico = iconSized 18 "tom-&-jerry" |> renderHtmlNode
            Expect.equal ico """<i class="material-icons md-18">tom-&-jerry</i>""" "icon HTML not correct"
        }
    ]

[<Tests>]
let iconTests =
    testList "icon" [
        test "succeeds" {
            let ico = icon "bob-&-tom" |> renderHtmlNode
            Expect.equal ico """<i class="material-icons">bob-&-tom</i>""" "icon HTML not correct"
        }
    ]

[<Tests>]
let locStrTests =
    testList "locStr" [
        test "succeeds" {
            let enc = locStr (LocalizedString ("test", "test&")) |> renderHtmlNode
            Expect.equal enc "test&amp;" "string not encoded correctly"
        }
    ]

[<Tests>]
let namedColorListTests =
    testList "namedColorList" [
        test "succeeds with default values" {
            let expected =
                [ """<select name="the-name">"""
                  """<option value="aqua" style="background-color:aqua;color:black;">aqua</option>"""
                  """<option value="black" style="background-color:black;color:white;">black</option>"""
                  """<option value="blue" style="background-color:blue;color:white;">blue</option>"""
                  """<option value="fuchsia" style="background-color:fuchsia;color:black;">fuchsia</option>"""
                  """<option value="gray" style="background-color:gray;color:white;">gray</option>"""
                  """<option value="green" style="background-color:green;color:white;">green</option>"""
                  """<option value="lime" style="background-color:lime;color:black;">lime</option>"""
                  """<option value="maroon" style="background-color:maroon;color:white;">maroon</option>"""
                  """<option value="navy" style="background-color:navy;color:white;">navy</option>"""
                  """<option value="olive" style="background-color:olive;color:white;">olive</option>"""
                  """<option value="purple" style="background-color:purple;color:white;">purple</option>"""
                  """<option value="red" style="background-color:red;color:black;">red</option>"""
                  """<option value="silver" style="background-color:silver;color:black;">silver</option>"""
                  """<option value="teal" style="background-color:teal;color:white;">teal</option>"""
                  """<option value="white" style="background-color:white;color:black;">white</option>"""
                  """<option value="yellow" style="background-color:yellow;color:black;">yellow</option>"""
                  "</select>"
                ]
                |> String.concat ""
            let selectList = namedColorList "the-name" "" [] _s |> renderHtmlNode
            Expect.equal expected selectList "The default select list was not generated correctly"
        }
        test "succeeds with a selected value" {
            let selectList = namedColorList "the-name" "white" [] _s |> renderHtmlNode
            Expect.stringContains selectList " selected>white</option>" "Selected option not generated correctly"
        }
        test "succeeds with extra attributes" {
            let selectList = namedColorList "the-name" "" [ _id "myId" ] _s |> renderHtmlNode
            Expect.stringStarts selectList """<select name="the-name" id="myId">""" "Attributes not included correctly"
        }
    ]

[<Tests>]
let radioTests =
    testList "radio" [
        test "succeeds when not selected" {
            let rad = radio "a-name" "anId" "test" "unit" |> renderHtmlNode
            Expect.equal rad """<input type="radio" name="a-name" id="anId" value="test">"""
                "Unselected radio button not generated correctly"
        }
        test "succeeds when selected" {
            let rad = radio "a-name" "anId" "unit" "unit" |> renderHtmlNode
            Expect.equal rad """<input type="radio" name="a-name" id="anId" value="unit" checked>"""
                "Selected radio button not generated correctly"
        }
    ]

[<Tests>]
let rawLocTextTests =
    testList "rawLocText" [
        test "succeeds" {
            use sw  = new StringWriter ()
            let raw = rawLocText sw (LocalizedHtmlString ("test", "test&")) |> renderHtmlNode
            Expect.equal raw "test&" "string not written correctly"
        }
    ]

[<Tests>]
let selectDefaultTests =
    testList "selectDefault" [
        test "succeeds" {
            Expect.equal (selectDefault "a&b") "— a&b —" "Default selection not generated correctly"
        }
    ]

[<Tests>]
let selectListTests =
    testList "selectList" [
        test "succeeds with minimum options" {
            let theList = selectList "a-list" "" [] [] |> renderHtmlNode
            Expect.equal theList """<select name="a-list" id="a-list"></select>"""
                "Empty select list not generated correctly"
        }
        test "succeeds with all options" {
            let theList =
                [ "tom", "Tom&"
                  "bob", "Bob"
                  "jan", "Jan"
                ]
                |> selectList "the-list" "bob" [ _style "ugly" ]
                |> renderHtmlNode
            let expected =
                [ """<select name="the-list" id="the-list" style="ugly">"""
                  """<option value="tom">Tom&amp;</option>"""
                  """<option value="bob" selected>Bob</option>"""
                  """<option value="jan">Jan</option>"""
                  """</select>"""
                ]
                |> String.concat ""
            Expect.equal theList expected "Filled select list not generated correctly"
        }
    ]

[<Tests>]
let spaceTests =
    testList "space" [
        test "succeeds" {
            Expect.equal (renderHtmlNode space) " " "space literal not correct"
        }
    ]


[<Tests>]
let submitTests =
    testList "submit" [
        test "succeeds" {
            let btn = submit [ _class "slick" ] "file-ico" _s["a&b"] |> renderHtmlNode
            Expect.equal
                btn
                """<button type="submit" class="slick"><i class="material-icons">file-ico</i> &nbsp;a&amp;b</button>"""
                "Submit button not generated correctly"
        }
    ]

[<Tests>]
let tableSummaryTests =
    testList "tableSummary" [
        test "succeeds for no entries" {
            let sum = tableSummary 0 _s |> renderHtmlNode
            Expect.equal sum """<div class="pt-center-text"><small>No Entries to Display</small></div>"""
                "Summary for no items is incorrect"
        }
        test "succeeds for one entry" {
            let sum = tableSummary 1 _s |> renderHtmlNode
            Expect.equal sum """<div class="pt-center-text"><small>Displaying 1 Entry</small></div>"""
                "Summary for one item is incorrect"
        }
        test "succeeds for many entries" {
            let sum = tableSummary 5 _s |> renderHtmlNode
            Expect.equal sum """<div class="pt-center-text"><small>Displaying 5 Entries</small></div>"""
                "Summary for many items is incorrect"
        }
    ]

module TimeZones =
  
    open PrayerTracker.Entities
    open PrayerTracker.Views.CommonFunctions.TimeZones

    [<Tests>]
    let nameTests =
        testList "TimeZones.name" [
            test "succeeds for US Eastern time" {
                Expect.equal (name (TimeZoneId "America/New_York") _s |> string) "Eastern"
                    "US Eastern time zone not returned correctly"
            }
            test "succeeds for US Central time" {
                Expect.equal (name (TimeZoneId "America/Chicago") _s |> string) "Central"
                    "US Central time zone not returned correctly"
            }
            test "succeeds for US Mountain time" {
                Expect.equal (name (TimeZoneId "America/Denver") _s |> string) "Mountain"
                    "US Mountain time zone not returned correctly"
            }
            test "succeeds for US Mountain (AZ) time" {
                Expect.equal (name (TimeZoneId "America/Phoenix") _s |> string) "Mountain (Arizona)"
                    "US Mountain (AZ) time zone not returned correctly"
            }
            test "succeeds for US Pacific time" {
                Expect.equal (name (TimeZoneId "America/Los_Angeles") _s |> string) "Pacific"
                    "US Pacific time zone not returned correctly"
            }
            test "succeeds for Central European time" {
                Expect.equal (name (TimeZoneId "Europe/Berlin") _s |> string) "Central European"
                    "Central European time zone not returned correctly"
            }
            test "fails for unexpected time zone" {
                Expect.equal (name (TimeZoneId "Wakanda") _s |> string) "Wakanda"
                    "Unexpected time zone should have returned the original ID"
            }
        ]
