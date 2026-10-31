module Jotter.Core.Tests.IndexTests

open System.IO
open Xunit
open Swensen.Unquote
open Jotter.Core

[<Fact>]
let ``htmlFileName resolves the first page to the configured blog index`` () =
    test <@ Index.htmlFileName 1 TestHelpers.sampleConfig = $"{Config.publicDirectory}/index.html" @>

[<Fact>]
let ``htmlFileName numbers subsequent pages without a duplicate dot`` () =
    test <@ Index.htmlFileName 2 TestHelpers.sampleConfig = $"{Config.publicDirectory}/index2.html" @>

[<Fact>]
let ``create writes a home page even when there are no posts yet`` () =
    TestHelpers.withTempWorkingDirectory (fun () ->
        TestHelpers.setupWorkspace "test"

        Index.create []

        test <@ File.Exists($"{Config.publicDirectory}/index.html") @>
    )

[<Fact>]
let ``create does not write a second page when there are no posts`` () =
    TestHelpers.withTempWorkingDirectory (fun () ->
        TestHelpers.setupWorkspace "test"

        Index.create []

        test <@ not (File.Exists($"{Config.publicDirectory}/index2.html")) @>
    )
