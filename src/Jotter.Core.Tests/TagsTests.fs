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
    TestHelpers.withTempWorkingDirectory (fun () ->
        TestHelpers.setupWorkspace "test"
        Tags.setup()

        let post =
            { TestHelpers.samplePost "hello.html" with
                frontmatter = { TestHelpers.sampleFrontMatter "Hello" with tags = [| "fsharp" |] } }

        Tags.create [ post ]

        test <@ File.Exists($"{Config.publicDirectory}/tags/fsharp.html") @>
        test <@ not (File.Exists($"{Config.publicDirectory}/tags/index.html")) @>
    )
