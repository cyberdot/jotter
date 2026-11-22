module Jotter.Core.Tests.AssetsTests

open System.IO
open Xunit
open Swensen.Unquote
open Jotter.Core

let private setupDeclaredAssets (paths: SitePaths) (styles: string list) (scripts: string list) =
    TestHelpers.setupWorkspace paths "test"
    let root = SitePaths.inWorkspace paths "themes/test"
    for path in styles @ scripts do
        let fullPath = Path.Combine(root, path.Replace('/', Path.DirectorySeparatorChar))
        let directory = Path.GetDirectoryName(fullPath)
        Directory.CreateDirectory(directory |> nonNull) |> ignore
        File.WriteAllText(fullPath, "")
    let styleJson = styles |> List.map Newtonsoft.Json.JsonConvert.SerializeObject |> String.concat ", "
    let scriptJson = scripts |> List.map Newtonsoft.Json.JsonConvert.SerializeObject |> String.concat ", "
    let manifest =
        $"""{{ "schemaVersion": 1, "id": "test", "name": "test", "version": "1.0.0", "jotter": ">=1.0.0 <2.0.0",
              "templates": {{ "base": "layout/layout.cshtml", "index": "layout/index.cshtml", "post": "layout/post.cshtml", "page": "layout/page.cshtml" }},
              "assets": {{ "styles": [{styleJson}], "scripts": [{scriptJson}] }}, "features": ["posts"] }}"""
    File.WriteAllText(Path.Combine(root, "theme.json"), manifest)

[<Fact>]
let ``css links follow manifest declarations and preserve nested paths`` () =
    TestHelpers.withTempSite (fun paths ->
        setupDeclaredAssets paths
            [ "assets/css/site.css"; "assets/css/sub/nested.css" ]
            []

        let links = Assets.css (TestHelpers.buildContext paths)

        test <@ links.Contains("href=\"/assets/css/site.css\"") @>
        test <@ links.Contains("href=\"/assets/css/sub/nested.css\"") @>
    )

[<Fact>]
let ``js links only declared scripts and ignore nearby CSS files`` () =
    TestHelpers.withTempSite (fun paths ->
        setupDeclaredAssets paths [] [ "assets/js/site.js" ]
        let root = SitePaths.inWorkspace paths "themes/test"
        Directory.CreateDirectory(Path.Combine(root, "assets/js/styles")) |> ignore
        File.WriteAllText(Path.Combine(root, "assets/js/styles/theme.css"), "")

        let links = Assets.js (TestHelpers.buildContext paths)

        test <@ links.Contains("src=\"/assets/js/site.js\"") @>
        test <@ not (links.Contains("theme.css")) @>
    )

[<Fact>]
let ``script asset declaration preserves nested paths`` () =
    TestHelpers.withTempSite (fun paths ->
        setupDeclaredAssets paths [] [ "assets/js/vendor/highlight.js" ]

        let links = Assets.js (TestHelpers.buildContext paths)

        test <@ links.Contains("src=\"/assets/js/vendor/highlight.js\"") @>
    )
