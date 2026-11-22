module Jotter.Core.Tests.IndexTests

open System.IO
open Xunit
open Swensen.Unquote
open Jotter.Core

[<Fact>]
let ``htmlFileName resolves the first page to the configured blog index`` () =
    TestHelpers.withTempSite (fun paths ->
        test <@ Index.htmlFileName paths 1 TestHelpers.sampleConfig = Path.Combine(paths.publicDirectory, "index.html") @>
    )

[<Fact>]
let ``htmlFileName numbers subsequent pages without a duplicate dot`` () =
    TestHelpers.withTempSite (fun paths ->
        test <@ Index.htmlFileName paths 2 TestHelpers.sampleConfig = Path.Combine(paths.publicDirectory, "index2.html") @>
    )

[<Fact>]
let ``create writes a home page even when there are no posts yet`` () =
    TestHelpers.withTempSite (fun paths ->
        TestHelpers.setupWorkspace paths "test"

        Index.create (TestHelpers.buildContext paths) []

        test <@ File.Exists(SitePaths.inPublic paths "index.html") @>
    )

[<Fact>]
let ``create does not write a second page when there are no posts`` () =
    TestHelpers.withTempSite (fun paths ->
        TestHelpers.setupWorkspace paths "test"

        Index.create (TestHelpers.buildContext paths) []

        test <@ not (File.Exists(SitePaths.inPublic paths "index2.html")) @>
    )
