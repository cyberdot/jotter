module Jotter.Core.Tests.BuildTaskTests

open System
open System.IO
open Xunit
open Swensen.Unquote
open Jotter.Core

let private prepareBuildInputs (paths: SitePaths) =
    TestHelpers.setupWorkspace paths "test"
    Directory.CreateDirectory(SitePaths.inWorkspace paths "posts") |> ignore
    Directory.CreateDirectory(SitePaths.inWorkspace paths "themes/test/assets/css") |> ignore
    File.WriteAllText(SitePaths.inWorkspace paths "robots.txt", "User-agent: *")
    File.WriteAllText(SitePaths.inWorkspace paths "CNAME", "example.test")
    File.WriteAllText(SitePaths.inPublic paths "sentinel.txt", "previous successful build")

[<Fact>]
let ``failed build preserves the previously published output`` () =
    TestHelpers.withTempSite (fun paths ->
        prepareBuildInputs paths
        File.WriteAllText(
            SitePaths.inWorkspace paths "posts/bad.markdown",
            """{"description":"missing title","created":"2024-01-01T00:00:00+00:00","tags":[],"slug":"bad"}
Body.""")

        Assert.Throws<BuildException>(fun () -> BuildTask.run paths) |> ignore

        test <@ File.ReadAllText(SitePaths.inPublic paths "sentinel.txt") = "previous successful build" @>
        test <@ Directory.GetDirectories(paths.root, ".jotter-build-*") |> Array.isEmpty @>
    )

[<Fact>]
let ``successful build replaces the previous output only after completion`` () =
    TestHelpers.withTempSite (fun paths ->
        prepareBuildInputs paths
        File.WriteAllText(
            SitePaths.inWorkspace paths "posts/hello.markdown",
            """{"title":"Hello","description":"A post","created":"2024-01-01T00:00:00+00:00","tags":["hello"],"slug":"hello"}
Hello **world**.""")

        BuildTask.run paths

        test <@ not (File.Exists(SitePaths.inPublic paths "sentinel.txt")) @>
        test <@ File.Exists(SitePaths.inPublic paths "index.html") @>
        test <@ File.Exists(SitePaths.inPublic paths "posts/hello.html") @>
        test <@ File.Exists(SitePaths.inPublic paths "tags/hello.html") @>
    )

[<Fact>]
let ``output validation rejects broken local asset references`` () =
    TestHelpers.withTempSite (fun paths ->
        Directory.CreateDirectory(paths.publicDirectory) |> ignore
        File.WriteAllText(SitePaths.inPublic paths "index.html", """<link href="./assets/missing.css">""")
        File.WriteAllText(SitePaths.inPublic paths Rss.rssFileName, "")
        File.WriteAllText(SitePaths.inPublic paths "sitemap.xml", "")

        match OutputValidation.validate paths with
        | Ok() -> Assert.Fail("Expected broken output validation to fail")
        | Error errors -> test <@ errors |> List.exists (fun error -> error.Contains("missing.css")) @>
    )

[<Fact>]
let ``every bundled catalog theme renders a complete multi-page site`` () =
    TestHelpers.withTempSite (fun catalogPaths ->
        for theme in Theme.available catalogPaths do
            TestHelpers.withTempSite (fun paths ->
                Site.init paths theme false
                let config = File.ReadAllText(paths.configFile)
                File.WriteAllText(paths.configFile, config.Replace("\"posts_per_page\": 10", "\"posts_per_page\": 1"))
                Post.create paths "Another Post" false

                BuildTask.run paths

                test <@ File.Exists(SitePaths.inPublic paths "index.html") @>
                test <@ File.Exists(SitePaths.inPublic paths "index2.html") @>
                test <@ File.Exists(SitePaths.inPublic paths "tags/new.html") @>
                test <@ File.Exists(SitePaths.inPublic paths "tags/new2.html") @>)
    )

