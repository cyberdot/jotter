module Jotter.Core.Tests.ThemeTests

open Xunit
open Swensen.Unquote
open Jotter.Core

[<Fact>]
let ``available lists the bundled theme directories`` () =
    TestHelpers.withTempSite (fun paths ->
        let themes = Theme.available paths

        test <@ Array.contains "poole" themes @>
        test <@ Array.contains "whisper" themes @>
        test <@ Array.contains "architect" themes @>
    )
