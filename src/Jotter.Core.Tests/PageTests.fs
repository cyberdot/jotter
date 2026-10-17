module Jotter.Core.Tests.PageTests

open System.IO
open System.Text.RegularExpressions
open Xunit
open Swensen.Unquote
open Jotter.Core

// File.WriteAllText overwrites silently; without a guard, creating a second page whose
// title slugs to the same filename would destroy the first page's content with no warning.
[<Fact>]
let ``create refuses to overwrite an existing page with the same slug`` () =
    TestHelpers.withTempWorkingDirectory (fun () ->
        Directory.CreateDirectory($"{Config.contentDirectory}/pages") |> ignore
        let path = $"{Config.contentDirectory}/pages/a-new-page.markdown"

        Page.create "A New Page"
        File.WriteAllText(path, "edited by hand")

        Assert.Throws<exn>(fun () -> Page.create "A New Page") |> ignore

        test <@ File.ReadAllText(path) = "edited by hand" @>
    )

[<Fact>]
let ``pageUrl builds an absolute URL at the site root, not a filesystem path`` () =
    test <@ Page.pageUrl TestHelpers.sampleConfig "about.html" = "http://localhost:5000/about.html" @>

[<Fact>]
let ``prepareAll renders markdown to HTML in the final document instead of discarding it`` () =
    TestHelpers.withTempWorkingDirectory (fun () ->
        TestHelpers.setupWorkspace "test"
        let pagesDir = $"{Config.contentDirectory}/pages"
        Directory.CreateDirectory(pagesDir) |> ignore
        let mdPath = $"{pagesDir}/about.markdown"
        File.WriteAllText(
            mdPath,
            "{\"title\":\"About\",\"description\":\"d\",\"created\":\"2024-01-01T00:00:00+00:00\",\"tags\":[\"page\"],\"slug\":\"about\"}\nThis is **bold** text."
        )

        let pages = Page.prepareAll [ mdPath ]

        test <@ pages.Length = 1 @>
        test <@ pages.Head.document.Contains("<strong>bold</strong>") @>
        test <@ not (pages.Head.document.Contains("**bold**")) @>
    )

[<Fact>]
let ``create stamps new pages with an unambiguous round-trip timestamp`` () =
    TestHelpers.withTempWorkingDirectory (fun () ->
        Directory.CreateDirectory($"{Config.contentDirectory}/pages") |> ignore

        Page.create "A New Page"

        let content = File.ReadAllText($"{Config.contentDirectory}/pages/a-new-page.markdown")
        let created = Regex.Match(content, "\"created\":\\s*\"([^\"]+)\"").Groups.[1].Value

        // The old "yyyy-MM-dd hh:mm:ssZ" format is a 12-hour clock with no AM/PM marker and a
        // literal "Z" - it would fail this round-trip ISO 8601 shape regardless of what time of
        // day the test runs, instead of only failing in the afternoon/evening.
        test <@ Regex.IsMatch(created, @"^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}\.\d+[+-]\d{2}:\d{2}$") @>
    )
