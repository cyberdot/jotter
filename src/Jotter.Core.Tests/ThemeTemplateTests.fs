module Jotter.Core.Tests.ThemeTemplateTests

open System
open System.IO
open Xunit
open Swensen.Unquote

// Index pages (index.html, index2.html, ...) are written at the site root, not under
// posts/, so pagination links must be root-relative. Reads the real shipped file via the
// Fixtures link in Jotter.Core.Tests.fsproj, not a copy that could drift.
[<Fact>]
let ``poole's index pagination links are root-relative, not prefixed with posts``() =
    let path = Path.Combine(AppContext.BaseDirectory, "Fixtures", "poole_index.cshtml")
    let content = File.ReadAllText(path)

    test <@ not (content.Contains("posts/@Model.prevFileName")) @>
    test <@ not (content.Contains("posts/@Model.nextFileName")) @>
    test <@ content.Contains("/@Model.prevFileName") @>
    test <@ content.Contains("/@Model.nextFileName") @>
