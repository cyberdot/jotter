module Jotter.Core.Tests.AssetsTests

open System.IO
open Xunit
open Swensen.Unquote
open Jotter.Core

[<Fact>]
let ``css links only .css files and keeps their subpath`` () =
    TestHelpers.withTempSite (fun paths ->
        let cssDir = SitePaths.inPublic paths "assets/css"
        Directory.CreateDirectory($"{cssDir}/sub") |> ignore
        File.WriteAllText($"{cssDir}/site.css", "")
        File.WriteAllText($"{cssDir}/sub/nested.css", "")
        File.WriteAllText($"{cssDir}/notes.txt", "")

        let links = Assets.css paths.publicDirectory (SitePaths.inPublic paths "index.html")

        test <@ links.Contains("href=\"/assets/css/site.css\"") @>
        test <@ links.Contains("href=\"/assets/css/sub/nested.css\"") @>
        test <@ not (links.Contains("notes.txt")) @>
    )

[<Fact>]
let ``js links only .js files, excluding non-js files in the same directory`` () =
    TestHelpers.withTempSite (fun paths ->
        // Mirrors pyxill's real layout: assets/js/styles/*.css are highlight.js theme
        // stylesheets, not scripts, and must not be linked as <script> tags.
        let jsDir = SitePaths.inPublic paths "assets/js"
        Directory.CreateDirectory($"{jsDir}/styles") |> ignore
        File.WriteAllText($"{jsDir}/site.js", "")
        File.WriteAllText($"{jsDir}/styles/theme.css", "")
        File.WriteAllText($"{jsDir}/readme.txt", "")

        let links = Assets.js paths.publicDirectory (SitePaths.inPublic paths "index.html")

        test <@ links.Contains("src=\"/assets/js/site.js\"") @>
        test <@ not (links.Contains("theme.css")) @>
        test <@ not (links.Contains("readme.txt")) @>
    )

[<Fact>]
let ``js keeps a nested file's subpath instead of flattening it to its bare name`` () =
    TestHelpers.withTempSite (fun paths ->
        let jsDir = SitePaths.inPublic paths "assets/js"
        Directory.CreateDirectory($"{jsDir}/vendor") |> ignore
        File.WriteAllText($"{jsDir}/vendor/highlight.js", "")

        let links = Assets.js paths.publicDirectory (SitePaths.inPublic paths "index.html")

        test <@ links.Contains("src=\"/assets/js/vendor/highlight.js\"") @>
    )
