module Jotter.Core.Tests.RssTests

open System
open System.IO
open Xunit
open Swensen.Unquote
open Jotter.Core

let private postDated (daysAgo: int) (name: string) =
    { TestHelpers.samplePost name with dateCreated = DateTimeOffset.UtcNow.AddDays(-(float daysAgo)) }

[<Fact>]
let ``selectFeedPosts orders newest first regardless of the site's own sort order`` () =
    let oldest = postDated 10 "oldest.html"
    let newest = postDated 1 "newest.html"
    let middle = postDated 5 "middle.html"

    let result = Rss.selectFeedPosts [ oldest; newest; middle ]

    test <@ (result |> List.map (fun p -> p.filename)) = [ "newest.html"; "middle.html"; "oldest.html" ] @>

[<Fact>]
let ``selectFeedPosts caps the feed instead of growing unbounded`` () =
    let posts = [ for i in 1 .. 30 -> postDated i $"post{i}.html" ]

    let result = Rss.selectFeedPosts posts

    test <@ List.length result = Rss.maxItems @>

[<Fact>]
let ``buildFeed uses the configured language instead of a hardcoded one`` () =
    TestHelpers.withTempWorkingDirectory (fun () ->
        Directory.CreateDirectory(Config.contentDirectory) |> ignore
        File.WriteAllText(
            $"{Config.contentDirectory}/config.json",
            """{ "name": "Site", "author": "a", "author_description": "d", "author_location": "l",
                 "url": "http://example.com", "description": "d", "language": "fr-fr",
                 "posts_per_page": 10, "sort_posts": "descending", "theme": "test",
                 "date_format": "dd MMM yyyy", "github": "g", "twitter": "t", "linked_in": "li",
                 "email": "e@example.com", "disqus_url": "d.disqus.com", "enable_disqus_comments": false }"""
        )

        let feed = Rss.buildFeed []

        test <@ feed.Language = "fr-fr" @>
    )

[<Fact>]
let ``buildFeed includes a feed-level link, not just per-item links`` () =
    TestHelpers.withTempWorkingDirectory (fun () ->
        Directory.CreateDirectory(Config.contentDirectory) |> ignore
        Config.init "test"

        let feed = Rss.buildFeed []

        test <@ feed.Links.Count > 0 @>
    )

// writeToFile used to Close() the XmlWriter only after WriteTo returned, so a failure while
// writing (e.g. a title containing a character XML forbids) left the file handle open for
// the rest of the process. `use` releases it regardless of how WriteTo exits.
[<Fact>]
let ``create releases the rss file even when writing the feed fails`` () =
    TestHelpers.withTempWorkingDirectory (fun () ->
        Directory.CreateDirectory(Config.contentDirectory) |> ignore
        Directory.CreateDirectory(Config.publicDirectory) |> ignore
        Config.init "test"

        let invalidPost =
            { TestHelpers.samplePost "bad.html" with
                frontmatter = { TestHelpers.sampleFrontMatter "Bad\u0001Title" with title = "Bad\u0001Title" } }

        Assert.Throws<ArgumentException>(fun () -> Rss.create [ invalidPost ]) |> ignore

        // If the file handle were still open from the failed write, this would throw
        // IOException instead of succeeding.
        File.WriteAllText($"{Config.publicDirectory}/{Rss.rssFileName}", "can overwrite")
    )
