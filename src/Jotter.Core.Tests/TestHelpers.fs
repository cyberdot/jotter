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
      sortPosts = "descending"
      theme = "whisper"
      dateFormat = "dd MMM yyyy"
      github = "test"
      twitter = "test"
      linkedIn = "test"
      email = "test@example.com"
      disqusUrl = "test.disqus.com"
      enableDisqusComments = false
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

// Config.contentDirectory/publicDirectory are hardcoded relative paths ("./_workspace",
// "./_public"), so any test touching them must run with the process's current directory
// pointed at an isolated temp folder. Collection parallelization is disabled in
// xunit.runner.json so this process-wide mutation is safe between tests.
let withTempWorkingDirectory (action: unit -> 'a) : 'a =
    let originalDirectory = Directory.GetCurrentDirectory()
    let tempDirectory = Path.Combine(Path.GetTempPath(), $"jotter-test-{Guid.NewGuid()}")
    Directory.CreateDirectory(tempDirectory) |> ignore
    Directory.SetCurrentDirectory(tempDirectory)
    try
        action()
    finally
        Directory.SetCurrentDirectory(originalDirectory)
        Directory.Delete(tempDirectory, true)

let private minimalLayoutCshtml = """<html><body>@Raw(Model.content)</body></html>"""
let private minimalPassthroughCshtml = """@Raw(Model.content)"""
let private minimalIndexCshtml = """@foreach(var post in Model.content) { <div class="post">@post.filename</div> }"""

let setupMinimalTheme (theme: string) =
    let dir = $"{Config.contentDirectory}/themes/{theme}/layout"
    Directory.CreateDirectory(dir) |> ignore
    File.WriteAllText($"{dir}/layout.cshtml", minimalLayoutCshtml)
    File.WriteAllText($"{dir}/page.cshtml", minimalPassthroughCshtml)
    File.WriteAllText($"{dir}/post.cshtml", minimalPassthroughCshtml)
    File.WriteAllText($"{dir}/index.cshtml", minimalIndexCshtml)

// The minimum _workspace/_public structure needed to exercise the build pipeline (config.json
// plus a bare-bones theme) without depending on the real themes shipped in Jotter.Console.
let setupWorkspace (theme: string) =
    Directory.CreateDirectory(Config.contentDirectory) |> ignore
    Directory.CreateDirectory(Config.publicDirectory) |> ignore
    Config.init theme
    setupMinimalTheme theme
