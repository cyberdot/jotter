module Jotter.Core.Tests.TagsTests

open System.IO
open Xunit
open Swensen.Unquote
open Jotter.Core

[<Fact>]
let ``tagIndexFileName slugs a tag with a space`` () =
    test <@ Tags.tagIndexFileName "machine learning" = "tags/machine-learning.html" @>

[<Fact>]
let ``tagIndexFileName slugs a tag containing a hash`` () =
    test <@ Tags.tagIndexFileName "C#" = "tags/csharp.html" @>

// Only poole ships a tag_index template; the other six bundled themes have none, so this
// mirrors their setup via TestHelpers' minimal theme (layout/page/post/index only).
// Per-tag archive pages must still render through tag_layout's fallback to the plain layout.
[<Fact>]
let ``create writes a per-tag archive page even when the theme has no tags-index template`` () =
    TestHelpers.withTempSite (fun paths ->
        TestHelpers.setupWorkspace paths "test"
        Tags.setup paths

        let post =
            { TestHelpers.samplePost "hello.html" with
                frontmatter = { TestHelpers.sampleFrontMatter "Hello" with tags = [| "fsharp" |] } }

        Tags.create (TestHelpers.buildContext paths) [ post ]

        test <@ File.Exists(SitePaths.inPublic paths "tags/fsharp.html") @>
        test <@ File.Exists(SitePaths.inPublic paths "tags/index.html") @>
    )

[<Fact>]
let ``create renders multiple pages for a tag with the shared index model`` () =
    TestHelpers.withTempSite (fun paths ->
        TestHelpers.setupWorkspace paths "test"
        let configPath = paths.configFile
        File.WriteAllText(
            configPath,
            File.ReadAllText(configPath).Replace("\"posts_per_page\": 10", "\"posts_per_page\": 1"))
        File.WriteAllText(
            SitePaths.inWorkspace paths "themes/test/layout/index.cshtml",
            """@if(Model.next > 0) { <a href="/@Model.nextFileName">Next</a> } @foreach(var post in Model.content) { <div>@post.filename</div> }""")
        Tags.setup paths

        let posts =
            [ for i in 1..2 ->
                { TestHelpers.samplePost $"post{i}.html" with
                    frontmatter = { TestHelpers.sampleFrontMatter $"Post {i}" with tags = [| "fsharp" |] } } ]

        Tags.create (TestHelpers.buildContext paths) posts

        test <@ File.Exists(SitePaths.inPublic paths "tags/fsharp.html") @>
        test <@ File.Exists(SitePaths.inPublic paths "tags/fsharp2.html") @>
        test <@ File.ReadAllText(SitePaths.inPublic paths "tags/fsharp.html").Contains("/tags/fsharp2.html") @>
    )

[<Fact>]
let ``create resolves tag asset links from the tags directory`` () =
    TestHelpers.withTempSite (fun paths ->
        TestHelpers.setupWorkspace paths "test"
        Directory.CreateDirectory(SitePaths.inWorkspace paths "themes/test/assets/css") |> ignore
        File.WriteAllText(SitePaths.inWorkspace paths "themes/test/assets/css/site.css", "")
        let manifestPath = SitePaths.inWorkspace paths "themes/test/theme.json"
        File.WriteAllText(
            manifestPath,
            File.ReadAllText(manifestPath).Replace("\"styles\": []", "\"styles\": [\"assets/css/site.css\"]"))
        File.WriteAllText(
            SitePaths.inWorkspace paths "themes/test/layout/layout.cshtml",
            """<html><head>@Raw(Model.css)</head><body>@Raw(Model.content)</body></html>""")
        Tags.setup paths

        Tags.create (TestHelpers.buildContext paths) [
            { TestHelpers.samplePost "hello.html" with
                frontmatter = { TestHelpers.sampleFrontMatter "Hello" with tags = [| "fsharp" |] } }
        ]

        let html = File.ReadAllText(SitePaths.inPublic paths "tags/fsharp.html")

        test <@ html.Contains("href=\"/assets/css/site.css\"") @>
        test <@ not (html.Contains("href=\"./assets/css/site.css\"")) @>
    )
