module Jotter.Core.Tests.DocumentTests

open Xunit
open Swensen.Unquote
open Jotter.Core

[<Fact>]
let ``fileName replaces the markdown extension with html`` () =
    test <@ Document.fileName "_workspace/posts/post.markdown" = "post.html" @>

[<Fact>]
let ``htmlFilename builds a path under posts for posts`` () =
    test <@ Document.htmlFilename true "_workspace/posts/post.markdown" = $"{Config.publicDirectory}/posts/post.html" @>

[<Fact>]
let ``htmlFilename builds a path at the site root for pages`` () =
    test <@ Document.htmlFilename false "_workspace/pages/about.markdown" = $"{Config.publicDirectory}/about.html" @>

[<Fact>]
let ``splitIntoParts parses the JSON front matter and returns the remaining body`` () =
    let content =
        "{\"title\":\"T\",\"description\":\"D\",\"created\":\"2024-01-01T00:00:00+00:00\",\"tags\":[\"a\",\"b\"],\"slug\":\"t\"}\nBody content here."

    let (frontMatter, body) = Document.splitIntoParts content

    test <@ frontMatter.title = "T" @>
    test <@ frontMatter.description = "D" @>
    test <@ frontMatter.tags = [| "a"; "b" |] @>
    test <@ frontMatter.slug = "t" @>
    test <@ body = "\nBody content here." @>

[<Fact>]
let ``createExcerpt appends an ellipsis to short content unchanged`` () =
    test <@ Document.createExcerpt "short text" = "short text ..." @>

[<Fact>]
let ``createExcerpt truncates long content to 1000 characters before the ellipsis`` () =
    let longContent = String.replicate 1_200 "a"

    let excerpt = Document.createExcerpt longContent

    test <@ excerpt.Length = 1_004 @>
    test <@ excerpt.EndsWith(" ...") @>
