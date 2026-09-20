module Jotter.Core.Tests.StoreTests

open Xunit
open Swensen.Unquote
open Jotter.Core

[<Fact>]
let ``addPosts appends new posts after the existing ones`` () =
    let existing = [ TestHelpers.samplePost "a.html" ]
    let toAdd = [ TestHelpers.samplePost "b.html" ]

    let result = Store.addPosts existing toAdd

    test <@ (result |> List.map (fun p -> p.filename)) = [ "a.html"; "b.html" ] @>

[<Fact>]
let ``addPosts leaves the inputs untouched`` () =
    let existing = [ TestHelpers.samplePost "a.html" ]
    let toAdd = [ TestHelpers.samplePost "b.html" ]

    Store.addPosts existing toAdd |> ignore

    test <@ List.length existing = 1 @>
    test <@ List.length toAdd = 1 @>

[<Fact>]
let ``addPages appends new pages after the existing ones`` () =
    let existing = [ TestHelpers.samplePage "about.html" ]
    let toAdd = [ TestHelpers.samplePage "contact.html" ]

    let result = Store.addPages existing toAdd

    test <@ (result |> List.map (fun p -> p.filename)) = [ "about.html"; "contact.html" ] @>
