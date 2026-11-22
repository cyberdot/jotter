module Jotter.Core.Tests.ThemeTests

open System.IO
open System
open System.Reflection
open Newtonsoft.Json
open Xunit
open Swensen.Unquote
open Jotter.Core

[<Fact>]
let ``available lists the bundled theme directories`` () =
    TestHelpers.withTempSite (fun paths ->
        let themes = Theme.available paths

        test <@ Array.contains "poole" themes @>
        test <@ Array.contains "whisper" themes @>
        test <@ Array.contains "architect" themes @>
    )

[<Fact>]
let ``catalog validation loads every registered theme manifest`` () =
    TestHelpers.withTempSite (fun paths ->
        let entries = Theme.loadCatalog paths

        for entry in entries do
            let source = Theme.resolveBundled paths entry.id
            test <@ source.manifest.id = entry.id @>
    )

[<Fact>]
let ``init copies only the selected theme`` () =
    TestHelpers.withTempSite (fun paths ->
        Site.init paths "poole" false

        let installed = Directory.GetDirectories(SitePaths.inWorkspace paths "themes")

        test <@ installed.Length = 1 @>
        test <@ File.Exists(SitePaths.inWorkspace paths "themes/poole/theme.json") @>
        test <@ not (Directory.Exists(SitePaths.inWorkspace paths "themes/whisper")) @>
    )

[<Fact>]
let ``createProjectTheme creates a valid local manifest and templates`` () =
    TestHelpers.withTempSite (fun paths ->
        Directory.CreateDirectory(paths.workspace) |> ignore
        Theme.createProjectTheme paths "custom"
        let source = Theme.resolveProject paths "custom"

        test <@ source.manifest.id = "custom" @>
        test <@ File.Exists(Path.Combine(source.root, source.manifest.templates.post)) @>
        test <@ source.manifest.styles = [ "assets/css/theme.css" ] @>
    )

[<Fact>]
let ``manifest rejects template paths escaping the theme root`` () =
    TestHelpers.withTempSite (fun paths ->
        TestHelpers.setupWorkspace paths "test"
        let manifestPath = SitePaths.inWorkspace paths "themes/test/theme.json"
        File.WriteAllText(
            manifestPath,
            File.ReadAllText(manifestPath).Replace("layout/layout.cshtml", "../layout.cshtml"))

        match Theme.tryLoadManifest (SitePaths.inWorkspace paths "themes/test") (Some "test") with
        | Ok _ -> Assert.Fail("Expected the unsafe template path to be rejected")
        | Error errors -> test <@ errors |> List.exists (fun error -> error.Contains("escapes")) @>
    )

[<Fact>]
let ``validate checks project-local themes`` () =
    TestHelpers.withTempSite (fun paths ->
        TestHelpers.setupWorkspace paths "test"
        Theme.createProjectTheme paths "custom"

        Theme.validate paths None

        File.Delete(SitePaths.inWorkspace paths "themes/custom/layout/post.cshtml")
        Assert.Throws<BuildException>(fun () -> Theme.validate paths (Some "custom")) |> ignore
    )

[<Fact>]
let ``useProjectTheme updates configuration to an installed local theme`` () =
    TestHelpers.withTempSite (fun paths ->
        TestHelpers.setupWorkspace paths "test"
        Theme.createProjectTheme paths "custom"

        Theme.useProjectTheme paths "custom"

        test <@ (Config.load paths).theme = "custom" @>
    )

[<Fact>]
let ``catalog rejects unsafe ids before they can be used as project paths`` () =
    TestHelpers.withTempSite (fun paths ->
        let root = SitePaths.inDirectory paths "unsafe-catalog"
        Directory.CreateDirectory(root) |> ignore
        File.WriteAllText(
            Path.Combine(root, "themes.json"),
            """{"schemaVersion":1,"themes":[{"id":"../outside","name":"Unsafe","path":"safe","description":"unsafe"}]}""")

        let paths = { paths with bundledThemes = root }

        let result =
            try
                Theme.loadCatalog paths |> ignore
                false
            with :? BuildException as ex ->
                ex.Message.Contains("invalid")

        test <@ result @>
    )

[<Fact>]
let ``theme compatibility uses the informational product version`` () =
    let attribute =
        typeof<ThemeManifest>.Assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
            |> nonNull
    let informational =
        attribute
            .InformationalVersion
            .Split([| '+'; '-' |], 2, StringSplitOptions.RemoveEmptyEntries).[0]

    test <@ Theme.compatibilityVersion() = Version.Parse(informational).ToString() @>

[<Fact>]
let ``cached remote catalog and one theme resolve without a local development catalog`` () =
    TestHelpers.withTempSite (fun paths ->
        let paths =
            { paths with bundledThemes = SitePaths.inDirectory paths "published-no-themes" }
        let cacheRoot = ThemeRepository.cacheRoot paths
        Directory.CreateDirectory(cacheRoot) |> ignore
        File.Copy(Path.Combine(AppContext.BaseDirectory, "themes", "themes.json"), Path.Combine(cacheRoot, "themes.json"))
        let themeRoot = Path.Combine(cacheRoot, "whisper")
        IOUtils.copyDirectory (Path.Combine(AppContext.BaseDirectory, "themes", "whisper")) themeRoot
        File.WriteAllText(
            themeRoot + ".complete.json",
            JsonConvert.SerializeObject(
                { ThemeCacheMetadata.repository = ThemeRepository.repository()
                  reference = ThemeRepository.reference()
                  theme = "whisper"
                  downloadedAt = DateTimeOffset.UtcNow }))

        let available = Theme.available paths
        let source = Theme.resolveBundled paths "whisper"

        test <@ Array.contains "whisper" available @>
        test <@ source.manifest.id = "whisper" @>
    )

[<Fact>]
let ``init copies a cached remote theme without downloading other themes`` () =
    TestHelpers.withTempSite (fun paths ->
        let paths =
            { paths with bundledThemes = SitePaths.inDirectory paths "published-no-themes" }
        let cacheRoot = ThemeRepository.cacheRoot paths
        Directory.CreateDirectory(cacheRoot) |> ignore
        File.Copy(Path.Combine(AppContext.BaseDirectory, "themes", "themes.json"), Path.Combine(cacheRoot, "themes.json"))
        let themeRoot = Path.Combine(cacheRoot, "whisper")
        IOUtils.copyDirectory (Path.Combine(AppContext.BaseDirectory, "themes", "whisper")) themeRoot
        File.WriteAllText(
            themeRoot + ".complete.json",
            JsonConvert.SerializeObject(
                { ThemeCacheMetadata.repository = ThemeRepository.repository()
                  reference = ThemeRepository.reference()
                  theme = "whisper"
                  downloadedAt = DateTimeOffset.UtcNow }))

        Site.init paths "whisper" false

        test <@ File.Exists(SitePaths.inWorkspace paths "themes/whisper/theme.json") @>
        test <@ not (Directory.Exists(Path.Combine(cacheRoot, "architect"))) @>
    )
