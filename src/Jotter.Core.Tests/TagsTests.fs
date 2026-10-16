module Jotter.Core.Tests.TagsTests

open Xunit
open Swensen.Unquote
open Jotter.Core

[<Fact>]
let ``tagIndexFileName slugs a tag with a space`` () =
    test <@ Tags.tagIndexFileName "machine learning" = "tags/machine-learning.html" @>

[<Fact>]
let ``tagIndexFileName slugs a tag containing a hash`` () =
    test <@ Tags.tagIndexFileName "C#" = "tags/csharp.html" @>
