module Jotter.Core.Tests.IndexPagerTests

open Xunit
open Swensen.Unquote
open Jotter.Core

[<Fact>]
let ``withIndexNum returns index.html for the first page when no page name is set`` () =
    test <@ IndexPager.withIndexNum Unchecked.defaultof<string> 1 = (1, "index.html") @>

[<Fact>]
let ``withIndexNum numbers subsequent pages when no page name is set`` () =
    test <@ IndexPager.withIndexNum Unchecked.defaultof<string> 3 = (3, "index3.html") @>

[<Fact>]
let ``withIndexNum returns the page unchanged for page 1 when a page name is set`` () =
    test <@ IndexPager.withIndexNum "index.html" 1 = (1, "index.html") @>

[<Fact>]
let ``withIndexNum inserts the page number before the extension without a duplicate dot`` () =
    test <@ IndexPager.withIndexNum "index.html" 2 = (2, "index2.html") @>

[<Fact>]
let ``withIndexNum works with a nested page path`` () =
    test <@ IndexPager.withIndexNum "tags/foo.html" 2 = (2, "tags/foo2.html") @>

[<Fact>]
let ``lastPage is true for an empty remainder`` () = test <@ IndexPager.lastPage (List.empty<int>) @>

[<Fact>]
let ``lastPage is false when items remain`` () = test <@ not (IndexPager.lastPage [ 1 ]) @>

[<Fact>]
let ``previousPage returns nothing for the first page`` () =
    test <@ IndexPager.previousPage 1 TestHelpers.sampleConfig = (0, System.String.Empty) @>

[<Fact>]
let ``previousPage points to the previous index file`` () =
    test <@ IndexPager.previousPage 2 TestHelpers.sampleConfig = (1, "index.html") @>

[<Fact>]
let ``nextPage returns nothing when on the last page`` () =
    test <@ IndexPager.nextPage 1 true TestHelpers.sampleConfig = (0, System.String.Empty) @>

[<Fact>]
let ``nextPage points to the next index file when more pages remain`` () =
    test <@ IndexPager.nextPage 1 false TestHelpers.sampleConfig = (2, "index2.html") @>
