module Jotter.Core.Tests.TestHelpers

open System
open System.IO
open Jotter.Core
open Jotter.Core.Config
open Jotter.Core.Document

let sampleConfig: ConfigurationModel =
    { name = "Test Site"
      author = "Test Author"
      authorDescription = "Test author description"
      authorLocation = "Test location"
      url = "http://localhost:5000"
      description = "Test site description"
      language = "en-gb"
      postsPerPage = 2
      sortPosts = Config.PostSort.Descending
      theme = "whisper"
      dateFormat = "dd MMM yyyy"
      github = "test"
      twitter = "test"
      linkedIn = "test"
      email = "test@example.com"
      disqusUrl = "test.disqus.com"
      enableDisqusComments = false
      googleAnalyticsId = ""
      blogIndex = "index.html" }

let sampleFrontMatter (title: string) : FrontMatter =
    { title = title
      description = "A sample description"
      created = DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero)
      tags = [| "sample" |]
      slug = title.ToLowerInvariant().Replace(" ", "-") }

let samplePost (filename: string) : Post =
    { dateCreated = DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero)
      frontmatter = sampleFrontMatter filename
      document = "<html></html>"
      path = $"_public/posts/{filename}"
      filename = filename
      excerpt = "An excerpt"
      config = sampleConfig }

let samplePage (filename: string) : Page =
    { document = "<html></html>"
      path = $"_public/{filename}"
      filename = filename }

let withTempSite (action: SitePaths -> 'a) : 'a =
    let tempDirectory = Path.Combine(Path.GetTempPath(), $"jotter-test-{Guid.NewGuid()}")
    Directory.CreateDirectory(tempDirectory) |> ignore
    try
        let paths = SitePaths.fromRoot tempDirectory
        action { paths with themeCache = Path.Combine(tempDirectory, ".theme-cache") }
    finally
        Directory.Delete(tempDirectory, true)

let private minimalLayoutCshtml = """<html><body>@Raw(Model.content)</body></html>"""
let private minimalPassthroughCshtml = """@Raw(Model.content)"""
let private minimalIndexCshtml = """@foreach(var post in Model.content) { <div class="post">@post.filename</div> }"""

let setupMinimalTheme (paths: SitePaths) (theme: string) =
    let dir = SitePaths.inWorkspace paths $"themes/{theme}/layout"
    Directory.CreateDirectory(dir) |> ignore
    File.WriteAllText($"{dir}/layout.cshtml", minimalLayoutCshtml)
    File.WriteAllText($"{dir}/page.cshtml", minimalPassthroughCshtml)
    File.WriteAllText($"{dir}/post.cshtml", minimalPassthroughCshtml)
    File.WriteAllText($"{dir}/index.cshtml", minimalIndexCshtml)
    Directory.CreateDirectory(SitePaths.inWorkspace paths $"themes/{theme}/assets") |> ignore
    File.WriteAllText(
        SitePaths.inWorkspace paths $"themes/{theme}/theme.json",
        $"""{{ "schemaVersion": 1, "id": "{theme}", "name": "{theme}", "version": "1.0.0", "jotter": ">=1.0.0 <2.0.0", "author": "Tests", "license": "MIT",
              "templates": {{ "base": "layout/layout.cshtml", "index": "layout/index.cshtml", "post": "layout/post.cshtml", "page": "layout/page.cshtml" }},
              "assets": {{ "styles": [], "scripts": [] }}, "features": ["posts", "pages", "tags", "pagination"] }}"""
    )

// The minimum _workspace/_public structure needed to exercise the build pipeline (config.json
// plus a bare-bones theme) without depending on the real themes shipped in Jotter.Console.
let setupWorkspace (paths: SitePaths) (theme: string) =
    Directory.CreateDirectory(paths.workspace) |> ignore
    Directory.CreateDirectory(paths.publicDirectory) |> ignore
    Config.init paths theme
    setupMinimalTheme paths theme

let buildContext (paths: SitePaths) =
    BuildContext.create paths
