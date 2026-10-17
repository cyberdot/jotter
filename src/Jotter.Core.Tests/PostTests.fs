module Jotter.Core.Tests.PostTests

open System.IO
open System.Text.RegularExpressions
open Xunit
open Swensen.Unquote
open Jotter.Core

// File.WriteAllText overwrites silently; without a guard, creating a second post whose
// title slugs to the same filename (even a plain re-run of the same title) would destroy
// the first post's content with no warning.
[<Fact>]
let ``create refuses to overwrite an existing post with the same slug`` () =
    TestHelpers.withTempWorkingDirectory (fun () ->
        Directory.CreateDirectory($"{Config.contentDirectory}/posts") |> ignore
        let path = $"{Config.contentDirectory}/posts/a-new-post.markdown"

        Post.create "A New Post" false
        File.WriteAllText(path, "edited by hand")

        Assert.Throws<exn>(fun () -> Post.create "A New Post" false) |> ignore

        test <@ File.ReadAllText(path) = "edited by hand" @>
    )

[<Fact>]
let ``postUrl builds an absolute URL under posts, not a filesystem path`` () =
    test <@ Post.postUrl TestHelpers.sampleConfig "hello.html" = "http://localhost:5000/posts/hello.html" @>

[<Fact>]
let ``create stamps new posts with an unambiguous round-trip timestamp`` () =
    TestHelpers.withTempWorkingDirectory (fun () ->
        Directory.CreateDirectory($"{Config.contentDirectory}/posts") |> ignore

        Post.create "A New Post" false

        let content = File.ReadAllText($"{Config.contentDirectory}/posts/a-new-post.markdown")
        let created = Regex.Match(content, "\"created\":\\s*\"([^\"]+)\"").Groups.[1].Value

        test <@ Regex.IsMatch(created, @"^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}\.\d+[+-]\d{2}:\d{2}$") @>
    )
