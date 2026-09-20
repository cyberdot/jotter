module Jotter.Core.Tests.IOUtilsTests

open Xunit
open Swensen.Unquote
open Jotter.Core

[<Fact>]
let ``urlSlug lowercases the title`` () = test <@ IOUtils.urlSlug "Hello World" = "hello-world" @>

[<Fact>]
let ``urlSlug replaces spaces with dashes`` () = test <@ IOUtils.urlSlug "a b c" = "a-b-c" @>

[<Fact>]
let ``urlSlug leaves already-slugified text unchanged`` () =
    test <@ IOUtils.urlSlug "already-a-slug" = "already-a-slug" @>

[<Fact>]
let ``filenameFromTitle builds a markdown path from directory and title`` () =
    test <@ IOUtils.filenameFromTitle "posts" "My First Post" = "posts/my-first-post.markdown" @>

[<Fact>]
let ``filenameFromTitle uses the given directory as-is`` () =
    test <@ IOUtils.filenameFromTitle "_workspace/drafts" "Draft Title" = "_workspace/drafts/draft-title.markdown" @>
