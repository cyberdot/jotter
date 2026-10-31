module Jotter.Core.Tests.GATests

open Xunit
open Swensen.Unquote
open Jotter.Core

[<Fact>]
let ``render embeds the Google Analytics account id`` () =
    let html = GA.render "UA-12345-1"

    test <@ html.Contains("UA-12345-1") @>
    test <@ html.Contains("Google Analytics") @>
    test <@ html.Contains("google-analytics.com/analytics.js") @>

// accountId is now reachable from config.json (google_analytics_id) in every bundled theme,
// and lands inside a single-quoted JS string literal - a stray quote must not close that
// literal early and corrupt the embedded script.
[<Fact>]
let ``render escapes a single quote in the account id instead of breaking out of the JS string`` () =
    let html = GA.render "UA-12345-1'; alert('x"

    test <@ html.Contains("ga('create', 'UA-12345-1\\'; alert(\\'x', 'auto');") @>
    test <@ not (html.Contains("ga('create', 'UA-12345-1'; alert('x', 'auto');")) @>
