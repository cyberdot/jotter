module Jotter.Core.Tests.SiteTests

open System.IO
open Xunit
open Swensen.Unquote
open Jotter.Core

[<Fact>]
let ``init refuses to overwrite an existing site without --force`` () =
    TestHelpers.withTempWorkingDirectory (fun () ->
        Directory.CreateDirectory(Config.contentDirectory) |> ignore
        let sentinelPath = $"{Config.contentDirectory}/sentinel.txt"
        File.WriteAllText(sentinelPath, "existing site content")

        Assert.Throws<exn>(fun () -> Site.init "poole" false) |> ignore

        // The guard must trip before clean() runs, or the existing site is gone either way.
        test <@ File.Exists(sentinelPath) @>
    )
