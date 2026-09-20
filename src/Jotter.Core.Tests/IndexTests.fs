module Jotter.Core.Tests.IndexTests

open Xunit
open Swensen.Unquote
open Jotter.Core

[<Fact>]
let ``htmlFileName resolves the first page to the configured blog index`` () =
    test <@ Index.htmlFileName 1 TestHelpers.sampleConfig = $"{Config.publicDirectory}/index.html" @>

[<Fact>]
let ``htmlFileName numbers subsequent pages without a duplicate dot`` () =
    test <@ Index.htmlFileName 2 TestHelpers.sampleConfig = $"{Config.publicDirectory}/index2.html" @>
