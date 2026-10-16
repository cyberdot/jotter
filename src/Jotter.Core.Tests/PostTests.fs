module Jotter.Core.Tests.PostTests

open System.IO
open System.Text.RegularExpressions
open Xunit
open Swensen.Unquote
open Jotter.Core

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
