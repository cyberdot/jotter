module Jotter.Core.Tests.DisqusCommentsTests

open Xunit
open Swensen.Unquote
open Jotter.Core

[<Fact>]
let ``render embeds the document url and disqus url`` () =
    let html = DisqusComments.render "http://example.com/posts/hello.html" "example.disqus.com"

    test <@ html.Contains("http://example.com/posts/hello.html") @>
    test <@ html.Contains("//example.disqus.com/embed.js") @>
    test <@ html.Contains("disqus_thread") @>
