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
