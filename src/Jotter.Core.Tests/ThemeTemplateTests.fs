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

// Every file resolveLayout can select as a full page (layout.cshtml plus each theme's
// index_layout/post_layout/tag_layout override) must embed the GA snippet, guarded by the
// id being set, or a theme silently drops Google Analytics the same way all of them used to
// silently drop every tag page before that was fixed.
[<Fact>]
let ``every theme's full-page layout embeds the guarded Google Analytics snippet``() =
    let layoutFiles =
        [ "architect_layout.cshtml"; "beautifuld_layout.cshtml"; "hyde_layout.cshtml"
          "poole_layout.cshtml"; "poole_tag_layout.cshtml"; "pyxill_layout.cshtml"
          "pyxill_index_layout.cshtml"; "uno_layout.cshtml"; "whisper_layout.cshtml"
          "whisper_post_layout.cshtml"; "whisper_tag_layout.cshtml" ]

    for fileName in layoutFiles do
        let path = Path.Combine(AppContext.BaseDirectory, "Fixtures", "layouts", fileName)
        let content = File.ReadAllText(path)

        test <@ content.Contains("Jotter.Core.GA.render(Model.config.googleAnalyticsId)") @>
        test <@ content.Contains("IsNullOrWhiteSpace(Model.config.googleAnalyticsId)") @>
