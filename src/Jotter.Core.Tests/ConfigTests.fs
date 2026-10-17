module Jotter.Core.Tests.ConfigTests

open System.IO
open Xunit
open Swensen.Unquote
open Jotter.Core

[<Fact>]
let ``init defaults new sites to descending sort so Older/Newer labels aren't backwards`` () =
    TestHelpers.withTempWorkingDirectory (fun () ->
        Directory.CreateDirectory(Config.contentDirectory) |> ignore

        Config.init "test"

        test <@ (Config.data()).sortPosts = "descending" @>
    )

// Index.compileIndex/Tags.compileIndex skip postsPerPage posts per page; postsPerPage <= 0
// never shrinks what's left to page through, so the build would otherwise hang writing
// index pages forever instead of failing with a clear error.
[<Fact>]
let ``data rejects a posts_per_page of zero instead of letting pagination loop forever`` () =
    TestHelpers.withTempWorkingDirectory (fun () ->
        Directory.CreateDirectory(Config.contentDirectory) |> ignore
        File.WriteAllText(
            Config.filePath,
            """{ "posts_per_page": 0, "sort_posts": "descending", "theme": "test" }"""
        )

        Assert.Throws<exn>(fun () -> Config.data() |> ignore) |> ignore
    )

[<Fact>]
let ``data rejects a missing posts_per_page the same way as zero`` () =
    TestHelpers.withTempWorkingDirectory (fun () ->
        Directory.CreateDirectory(Config.contentDirectory) |> ignore
        File.WriteAllText(Config.filePath, """{ "sort_posts": "descending", "theme": "test" }""")

        Assert.Throws<exn>(fun () -> Config.data() |> ignore) |> ignore
    )
