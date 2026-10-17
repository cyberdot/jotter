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

// documentUrl is normally built from the site's own config.url, which is free-form user
// input - a single quote in there would otherwise close the JS string literal early and
// corrupt the embedded script (or worse, let arbitrary script run).
[<Fact>]
let ``render escapes a single quote in the document url instead of breaking out of the JS string`` () =
    let html = DisqusComments.render "http://example.com/posts/o'brien.html" "example.disqus.com"

    test <@ html.Contains("this.page.url = 'http://example.com/posts/o\\'brien.html';") @>
    test <@ not (html.Contains("this.page.url = 'http://example.com/posts/o'brien.html'")) @>
