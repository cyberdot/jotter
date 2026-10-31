module Jotter.Core.Tests.SiteMapTests

open Xunit
open Swensen.Unquote
open Jotter.Core

[<Fact>]
let ``homeEntry gets the top priority`` () =
    test <@ SiteMap.homeEntry(TestHelpers.sampleConfig).priority = "1.0" @>

[<Fact>]
let ``postEntry uses the post's own created date, not the build date`` () =
    let post = TestHelpers.samplePost "hello.html"

    let entry = SiteMap.postEntry TestHelpers.sampleConfig post

    test <@ entry.lastModify = "2024-01-01" @>
    test <@ entry.loc = "http://localhost:5000/posts/hello.html" @>
    test <@ entry.priority = "0.8" @>

[<Fact>]
let ``pageEntry gets a lower priority than the home page and than posts`` () =
    let entry = SiteMap.pageEntry TestHelpers.sampleConfig "about.html"
    test <@ entry.priority = "0.5" @>

[<Fact>]
let ``isIndexFile recognises the home page and numbered pagination pages`` () =
    test <@ SiteMap.isIndexFile "index.html" @>
    test <@ SiteMap.isIndexFile "index2.html" @>
    test <@ not (SiteMap.isIndexFile "about.html") @>
