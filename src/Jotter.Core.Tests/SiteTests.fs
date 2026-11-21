module Jotter.Core.Tests.SiteTests

open System.IO
open Xunit
open Swensen.Unquote
open Jotter.Core

[<Fact>]
let ``init refuses to overwrite an existing site without --force`` () =
    TestHelpers.withTempSite (fun paths ->
        Directory.CreateDirectory(paths.workspace) |> ignore
        let sentinelPath = SitePaths.inWorkspace paths "sentinel.txt"
        File.WriteAllText(sentinelPath, "existing site content")

        Assert.Throws<BuildException>(fun () -> Site.init paths "poole" false) |> ignore

        // The guard must trip before clean() runs, or the existing site is gone either way.
        test <@ File.Exists(sentinelPath) @>
    )

// Without this, a typo'd theme name succeeded silently at init and only failed later,
// confusingly, when build tried to read a theme directory that was never copied.
[<Fact>]
let ``init rejects an unknown theme before touching the filesystem`` () =
    TestHelpers.withTempSite (fun paths ->
        Assert.Throws<BuildException>(fun () -> Site.init paths "no-such-theme" true) |> ignore

        test <@ not (Directory.Exists(paths.workspace)) @>
    )
