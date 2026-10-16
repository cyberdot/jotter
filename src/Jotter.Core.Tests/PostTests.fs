module Jotter.Core.Tests.PostTests

open Xunit
open Swensen.Unquote
open Jotter.Core

[<Fact>]
let ``postUrl builds an absolute URL under posts, not a filesystem path`` () =
    test <@ Post.postUrl TestHelpers.sampleConfig "hello.html" = "http://localhost:5000/posts/hello.html" @>
