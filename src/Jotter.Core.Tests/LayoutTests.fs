module Jotter.Core.Tests.LayoutTests

open System.IO
open Xunit
open Swensen.Unquote
open Jotter.Core

// Layout's theme directory used to be a module-level value, computed once on the first
// call into the module and cached as a field for the rest of the process. The second block
// below runs in a brand new temp workspace with a different theme; under the old code it
// would still resolve paths under the first block's (by now deleted) temp directory and
// throw DirectoryNotFoundException instead of reading the second theme's template.
[<Fact>]
let ``page resolves the theme from the current workspace, not one cached from an earlier call`` () =
    TestHelpers.withTempWorkingDirectory (fun () ->
        TestHelpers.setupWorkspace "first"
        File.WriteAllText($"{Config.contentDirectory}/themes/first/layout/page.cshtml", "first theme content")

        let (content, _) = Layout.page ()

        test <@ content = "first theme content" @>
    )

    TestHelpers.withTempWorkingDirectory (fun () ->
        TestHelpers.setupWorkspace "second"
        File.WriteAllText($"{Config.contentDirectory}/themes/second/layout/page.cshtml", "second theme content")

        let (content, _) = Layout.page ()

        test <@ content = "second theme content" @>
    )
