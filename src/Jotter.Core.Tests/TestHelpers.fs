module Jotter.Core.Tests.TestHelpers

open System
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
