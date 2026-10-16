module Jotter.Core.Tests.ConfigTests

open Xunit
open Swensen.Unquote
open Jotter.Core

[<Fact>]
let ``init defaults new sites to descending sort so Older/Newer labels aren't backwards`` () =
    TestHelpers.withTempWorkingDirectory (fun () ->
        System.IO.Directory.CreateDirectory(Config.contentDirectory) |> ignore

        Config.init "test"

        test <@ (Config.data()).sortPosts = "descending" @>
    )
