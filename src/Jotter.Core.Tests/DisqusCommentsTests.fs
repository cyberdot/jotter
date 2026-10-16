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

[<Fact>]
let ``render does not leak literal backslash escapes into the markup`` () =
    // A triple-quoted F# string does not process \" the way a regular string does, so
    // escaping quotes the way a C#/JS string literal would produces literal backslashes
    // in the HTML (e.g. id=\"disqus_thread\") instead of id="disqus_thread".
    let html = DisqusComments.render "http://example.com/posts/hello.html" "example.disqus.com"

    test <@ not (html.Contains("\\")) @>
    test <@ html.Contains("""<div id="disqus_thread"></div>""") @>
    test <@ html.Contains("this.page.url = 'http://example.com/posts/hello.html';") @>
