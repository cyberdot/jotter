module Jotter.Core.Tests.ConfigTests

open System.IO
open Xunit
open Swensen.Unquote
open Jotter.Core

[<Fact>]
let ``init defaults new sites to descending sort so Older/Newer labels aren't backwards`` () =
    TestHelpers.withTempSite (fun paths ->
        Directory.CreateDirectory(paths.workspace) |> ignore

        Config.init paths "test"

        test <@ (Config.load paths).sortPosts = Config.PostSort.Descending @>
    )

// Index.compileIndex/Tags.compileIndex skip postsPerPage posts per page; postsPerPage <= 0
// never shrinks what's left to page through, so the build would otherwise hang writing
// index pages forever instead of failing with a clear error.
[<Fact>]
let ``data rejects a posts_per_page of zero instead of letting pagination loop forever`` () =
    TestHelpers.withTempSite (fun paths ->
        Directory.CreateDirectory(paths.workspace) |> ignore
        File.WriteAllText(
            paths.configFile,
            """{ "posts_per_page": 0, "sort_posts": "descending", "theme": "test" }"""
        )

        Assert.Throws<BuildException>(fun () -> Config.load paths |> ignore) |> ignore
    )

[<Fact>]
let ``data rejects a missing posts_per_page the same way as zero`` () =
    TestHelpers.withTempSite (fun paths ->
        Directory.CreateDirectory(paths.workspace) |> ignore
        File.WriteAllText(paths.configFile, """{ "sort_posts": "descending", "theme": "test" }""")

        Assert.Throws<BuildException>(fun () -> Config.load paths |> ignore) |> ignore
    )

[<Fact>]
let ``load rejects an unknown sort order instead of silently using source order`` () =
    TestHelpers.withTempSite (fun paths ->
        Config.init paths "test"
        let content = File.ReadAllText(paths.configFile).Replace("\"sort_posts\": \"descending\"", "\"sort_posts\": \"sideways\"")
        File.WriteAllText(paths.configFile, content)

        Assert.Throws<BuildException>(fun () -> Config.load paths |> ignore) |> ignore
    )
