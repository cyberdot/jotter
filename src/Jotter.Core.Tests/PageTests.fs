module Jotter.Core.Tests.PageTests

open Xunit
open Swensen.Unquote
open Jotter.Core

[<Fact>]
let ``pageUrl builds an absolute URL at the site root, not a filesystem path`` () =
    test <@ Page.pageUrl TestHelpers.sampleConfig "about.html" = "http://localhost:5000/about.html" @>
