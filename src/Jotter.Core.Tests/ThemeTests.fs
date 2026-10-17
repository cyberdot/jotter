module Jotter.Core.Tests.ThemeTests

open Xunit
open Swensen.Unquote
open Jotter.Core

[<Fact>]
let ``available lists the bundled theme directories`` () =
    let themes = Theme.available ()

    test <@ Array.contains "poole" themes @>
    test <@ Array.contains "whisper" themes @>
    test <@ Array.contains "architect" themes @>
