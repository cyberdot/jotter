namespace Jotter.Core

open System
open System.IO
open System.Reflection
open Newtonsoft.Json
open Newtonsoft.Json.Linq
open Jotter.Core.Config
open Jotter.Core.IOUtils

[<CLIMutable>]
type ThemeCatalogEntryDto =
    { id: string
      name: string
      path: string
      description: string }

[<CLIMutable>]
type ThemeCatalogDto =
    { schemaVersion: int
      themes: ThemeCatalogEntryDto array }

[<CLIMutable>]
type ThemeTemplatesDto =
    { [<JsonProperty("base")>] baseTemplate: string
      index: string
      post: string
      page: string
      postLayout: string
      pageLayout: string
      indexLayout: string
      tagLayout: string
      tagIndex: string }

[<CLIMutable>]
type ThemeAssetsDto =
    { styles: string array
      scripts: string array }

[<CLIMutable>]
type ThemeManifestDto =
    { schemaVersion: int
      id: string
      name: string
      version: string
      jotter: string
      author: string
      license: string
      templates: ThemeTemplatesDto
      assets: ThemeAssetsDto
      features: string array
      preview: string }

type ThemeTemplates =
    { baseTemplate: string
      index: string
      post: string
      page: string
      postLayout: string option
      pageLayout: string option
      indexLayout: string option
      tagLayout: string option
      tagIndex: string option }

type ThemeManifest =
    { schemaVersion: int
      id: string
      name: string
      version: string
      jotter: string
      author: string
      license: string
      templates: ThemeTemplates
      styles: string list
      scripts: string list
      features: string list
      preview: string option }

type ThemeSource =
    { root: string
      manifest: ThemeManifest }

module Theme =
    let catalogFileName = "themes.json"
    let manifestFileName = "theme.json"

    let private nullableOption value =
        if String.IsNullOrWhiteSpace(value) then None else Some value

    let private ensureRelativePath (root: string) (relativePath: string) =
        if String.IsNullOrWhiteSpace(relativePath) then
            Error "Path must not be blank."
        elif Path.IsPathFullyQualified(relativePath) then
            Error $"Absolute paths are not allowed: '{relativePath}'."
        else
            let rootPath = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar)
            let candidate = Path.GetFullPath(Path.Combine(rootPath, relativePath))
            if candidate = rootPath || candidate.StartsWith(rootPath + string Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) then
                Ok candidate
            else
                Error $"Path escapes the theme directory: '{relativePath}'."

    let private loadJson<'T> (path: string) =
        try
            if not (File.Exists(path)) then
                Error [ $"Required theme file not found: {path}" ]
            else
                let value = JsonConvert.DeserializeObject<'T>(File.ReadAllText(path))
                if isNull (box value) then
                    Error [ $"Theme metadata must contain a JSON object: {path}" ]
                else
                    Ok value
        with
        | :? JsonException as ex -> Error [ $"Invalid JSON in {path}: {ex.Message}" ]
        | :? IOException as ex -> Error [ $"Cannot read {path}: {ex.Message}" ]

    let private requireFile (root: string) (label: string) (path: string) (errors: string list) =
        match ensureRelativePath root path with
        | Error error -> $"{label}: {error}" :: errors
        | Ok fullPath when not (File.Exists(fullPath)) -> $"{label}: file not found: '{path}'." :: errors
        | Ok _ -> errors

    let private currentJotterVersion =
        Assembly.GetExecutingAssembly().GetName().Version
        |> Option.ofObj
        |> Option.defaultValue (Version(1, 0))

    let private isCompatible (range: string) =
        if String.IsNullOrWhiteSpace(range) then
            false
        else
            range.Split(' ', StringSplitOptions.RemoveEmptyEntries)
            |> Array.forall (fun requirement ->
                let operator =
                    [ ">="; "<="; ">"; "<"; "=" ]
                    |> List.tryFind requirement.StartsWith
                    |> Option.defaultValue "="
                let versionText =
                    if operator = "=" && not (requirement.StartsWith("=")) then
                        requirement
                    else
                        requirement.Substring(operator.Length)
                match Version.TryParse(versionText) with
                | false, _ -> false
                | true, version ->
                    match operator with
                    | ">=" -> currentJotterVersion >= version
                    | "<=" -> currentJotterVersion <= version
                    | ">" -> currentJotterVersion > version
                    | "<" -> currentJotterVersion < version
                    | "=" -> currentJotterVersion.Major = version.Major
                            && currentJotterVersion.Minor = version.Minor
                            && currentJotterVersion.Build = version.Build
                    | _ -> false)

    let private validateManifestDto (root: string) (expectedId: string option) (dto: ThemeManifestDto) =
        let mutable errors = []
        if dto.schemaVersion <> 1 then
            errors <- $"Unsupported theme manifest schemaVersion: {dto.schemaVersion}." :: errors
        if String.IsNullOrWhiteSpace(dto.id) then
            errors <- "Theme id must not be blank." :: errors
        if String.IsNullOrWhiteSpace(dto.name) then
            errors <- "Theme name must not be blank." :: errors
        if String.IsNullOrWhiteSpace(dto.version) then
            errors <- "Theme version must not be blank." :: errors
        if String.IsNullOrWhiteSpace(dto.jotter) then
            errors <- "Theme jotter compatibility must not be blank." :: errors
        elif not (isCompatible dto.jotter) then
            errors <- $"Theme is not compatible with Jotter {currentJotterVersion}: '{dto.jotter}'." :: errors
        expectedId
        |> Option.iter (fun id ->
            if dto.id <> id then
                errors <- $"Theme manifest id '{dto.id}' does not match catalog id '{id}'." :: errors)

        if isNull (box dto.templates) then
            errors <- "Theme templates must be declared." :: errors
        else
            [ "base", dto.templates.baseTemplate
              "index", dto.templates.index
              "post", dto.templates.post
              "page", dto.templates.page ]
            |> List.iter (fun (label, path) ->
                if String.IsNullOrWhiteSpace(path) then
                    errors <- $"Required template '{label}' is missing." :: errors
                else
                    errors <- requireFile root label path errors)

            [ "postLayout", dto.templates.postLayout
              "pageLayout", dto.templates.pageLayout
              "indexLayout", dto.templates.indexLayout
              "tagLayout", dto.templates.tagLayout
              "tagIndex", dto.templates.tagIndex ]
            |> List.iter (fun (label, path) ->
                if not (String.IsNullOrWhiteSpace(path)) then
                    errors <- requireFile root label path errors)

        if isNull (box dto.assets) then
            errors <- "Theme assets must be declared." :: errors
        else
            if isNull dto.assets.styles then
                errors <- "Theme assets.styles must be an array." :: errors
            else
                let duplicates =
                    dto.assets.styles
                    |> Array.countBy id
                    |> Array.filter (fun (_, count) -> count > 1)
                    |> Array.map fst
                if duplicates.Length > 0 then
                    let names = String.Join(", ", duplicates)
                    errors <- $"Duplicate style declarations: {names}." :: errors
                dto.assets.styles
                |> Array.iter (fun path -> errors <- requireFile root "style" path errors)
            if isNull dto.assets.scripts then
                errors <- "Theme assets.scripts must be an array." :: errors
            else
                let duplicates =
                    dto.assets.scripts
                    |> Array.countBy id
                    |> Array.filter (fun (_, count) -> count > 1)
                    |> Array.map fst
                if duplicates.Length > 0 then
                    let names = String.Join(", ", duplicates)
                    errors <- $"Duplicate script declarations: {names}." :: errors
                dto.assets.scripts
                |> Array.iter (fun path -> errors <- requireFile root "script" path errors)

        if not (String.IsNullOrWhiteSpace(dto.preview)) then
            errors <- requireFile root "preview" dto.preview errors

        match errors with
        | [] ->
            let manifest =
                { schemaVersion = dto.schemaVersion
                  id = dto.id
                  name = dto.name
                  version = dto.version
                  jotter = dto.jotter
                  author = dto.author
                  license = dto.license
                  templates =
                    { baseTemplate = dto.templates.baseTemplate
                      index = dto.templates.index
                      post = dto.templates.post
                      page = dto.templates.page
                      postLayout = nullableOption dto.templates.postLayout
                      pageLayout = nullableOption dto.templates.pageLayout
                      indexLayout = nullableOption dto.templates.indexLayout
                      tagLayout = nullableOption dto.templates.tagLayout
                      tagIndex = nullableOption dto.templates.tagIndex }
                  styles = dto.assets.styles |> Array.toList
                  scripts = dto.assets.scripts |> Array.toList
                  features = if isNull dto.features then [] else dto.features |> Array.toList
                  preview = nullableOption dto.preview }
            Ok manifest
        | errors -> Error(List.rev errors)

    let tryLoadManifest (root: string) (expectedId: string option) =
        loadJson<ThemeManifestDto> (Path.Combine(root, manifestFileName))
        |> Result.bind (validateManifestDto root expectedId)

    let loadManifest (root: string) (expectedId: string option) =
        match tryLoadManifest root expectedId with
        | Ok manifest -> manifest
        | Error errors -> raise (BuildException(String.Join(Environment.NewLine, errors)))

    let tryLoadCatalog (paths: SitePaths) =
        let catalogPath = Path.Combine(paths.bundledThemes, catalogFileName)
        loadJson<ThemeCatalogDto> catalogPath
        |> Result.bind (fun catalog ->
            if isNull (box catalog) || catalog.schemaVersion <> 1 then
                Error [ $"Unsupported theme catalog schemaVersion in {catalogPath}." ]
            elif isNull catalog.themes then
                Error [ "themes must be an array in the theme catalog." ]
            else
                let errors = ResizeArray<string>()
                let ids = Collections.Generic.HashSet<string>(StringComparer.OrdinalIgnoreCase)
                let relativePaths = Collections.Generic.HashSet<string>(StringComparer.OrdinalIgnoreCase)

                for entry in catalog.themes do
                    if String.IsNullOrWhiteSpace(entry.id) then errors.Add("Catalog theme id must not be blank.")
                    elif not (ids.Add(entry.id)) then errors.Add($"Duplicate theme id: '{entry.id}'.")

                    if String.IsNullOrWhiteSpace(entry.name) then errors.Add($"Theme '{entry.id}' name must not be blank.")
                    if String.IsNullOrWhiteSpace(entry.path) then
                        errors.Add($"Theme '{entry.id}' path must not be blank.")
                    else
                        match ensureRelativePath paths.bundledThemes entry.path with
                        | Error error -> errors.Add($"Theme '{entry.id}': {error}")
                        | Ok root ->
                            if not (relativePaths.Add(Path.GetFullPath(root))) then
                                errors.Add($"Duplicate theme path: '{entry.path}'.")
                            if not (Directory.Exists(root)) then
                                errors.Add($"Theme '{entry.id}' directory not found: '{entry.path}'.")
                            match tryLoadManifest root (Some entry.id) with
                            | Ok _ -> ()
                            | Error manifestErrors ->
                                manifestErrors |> List.iter (fun error -> errors.Add($"Theme '{entry.id}': {error}"))

                let registered =
                    catalog.themes
                    |> Array.choose (fun entry -> nullableOption entry.path)
                    |> Array.map (fun path -> Path.GetFullPath(Path.Combine(paths.bundledThemes, path)))
                    |> Set.ofArray

                Directory.GetDirectories(paths.bundledThemes)
                |> Array.iter (fun directory ->
                    if not (registered.Contains(Path.GetFullPath(directory))) then
                        errors.Add($"Unregistered theme directory: '{Path.GetFileName(directory)}'."))

                if errors.Count = 0 then
                    Ok(Array.toList catalog.themes)
                else
                    Error(List.ofSeq errors))

    let loadCatalog (paths: SitePaths) =
        match tryLoadCatalog paths with
        | Ok entries -> entries
        | Error errors -> raise (BuildException(String.Join(Environment.NewLine, errors)))

    let available (paths: SitePaths) =
        loadCatalog paths
        |> List.map (fun entry -> entry.id)
        |> List.sort
        |> List.toArray

    let resolveBundled (paths: SitePaths) (themeId: string) =
        let entry =
            loadCatalog paths
            |> List.tryFind (fun item -> item.id = themeId)
            |> Option.defaultWith (fun () ->
                raise
                    (BuildException
                        $"""Unknown theme '{themeId}'. Available themes: {String.Join(", ", available paths)}."""))
        let root =
            match ensureRelativePath paths.bundledThemes entry.path with
            | Ok value -> value
            | Error error -> raise (BuildException error)
        { root = root
          manifest = loadManifest root (Some entry.id) }

    let resolveProject (paths: SitePaths) (themeId: string) =
        let themesRoot = SitePaths.inWorkspace paths "themes"
        match ensureRelativePath themesRoot themeId with
        | Error error -> raise (BuildException error)
        | Ok root when not (Directory.Exists(root)) ->
            raise (BuildException $"Theme '{themeId}' is not installed in '{themesRoot}'. Run 'init' again or copy the theme into the project.")
        | Ok root ->
            { root = root
              manifest = loadManifest root (Some themeId) }

    let init (paths: SitePaths) (themeId: string) =
        let source = resolveBundled paths themeId
        let destination = SitePaths.inWorkspace paths (Path.Combine("themes", themeId))
        Directory.CreateDirectory(Path.GetDirectoryName(destination)) |> ignore
        copyDirectory source.root destination

    let copyToProject (paths: SitePaths) (themeId: string) =
        if not (Directory.Exists(paths.workspace)) then
            raise (BuildException $"Site workspace not found: '{paths.workspace}'. Run 'init' first.")
        let destination = SitePaths.inWorkspace paths (Path.Combine("themes", themeId))
        if Directory.Exists(destination) then
            raise (BuildException $"Theme '{themeId}' already exists at '{destination}'.")
        init paths themeId

    let createProjectTheme (paths: SitePaths) (themeId: string) =
        if not (Directory.Exists(paths.workspace)) then
            raise (BuildException $"Site workspace not found: '{paths.workspace}'. Run 'init' first.")
        if String.IsNullOrWhiteSpace(themeId)
           || themeId |> Seq.exists (fun c -> not (Char.IsLetterOrDigit(c) || c = '-' || c = '_')) then
            raise (BuildException "Theme id may contain only letters, digits, '-' and '_'.")
        let themeRoot = SitePaths.inWorkspace paths (Path.Combine("themes", themeId))
        if Directory.Exists(themeRoot) then
            raise (BuildException $"Theme '{themeId}' already exists at '{themeRoot}'.")

        let layoutRoot = Path.Combine(themeRoot, "layout")
        let assetRoot = Path.Combine(themeRoot, "assets", "css")
        Directory.CreateDirectory(layoutRoot) |> ignore
        Directory.CreateDirectory(assetRoot) |> ignore

        File.WriteAllText(
            Path.Combine(themeRoot, manifestFileName),
            JsonConvert.SerializeObject(
                {| schemaVersion = 1
                   id = themeId
                   name = themeId
                   version = "1.0.0"
                   jotter = ">=1.0.0 <2.0.0"
                   author = ""
                   license = "MIT"
                   templates =
                     {| ``base`` = "layout/layout.cshtml"
                        index = "layout/index.cshtml"
                        post = "layout/post.cshtml"
                        page = "layout/page.cshtml" |}
                   assets = {| styles = [| "assets/css/theme.css" |]; scripts = Array.empty<string> |}
                   features = [| "posts"; "pages"; "tags"; "pagination" |] |},
                Formatting.Indented))
        File.WriteAllText(
            Path.Combine(layoutRoot, "layout.cshtml"),
            """<!doctype html><html><head>@Raw(Model.css)</head><body>@Raw(Model.content)@Raw(Model.js)</body></html>""")
        File.WriteAllText(
            Path.Combine(layoutRoot, "index.cshtml"),
            """<main>@foreach(var post in Model.content) { <article><a href="/posts/@post.filename">@post.frontmatter.title</a></article> }</main>""")
        File.WriteAllText(Path.Combine(layoutRoot, "post.cshtml"), """<article><h1>@Model.frontmatter.title</h1>@Raw(Model.content)</article>""")
        File.WriteAllText(Path.Combine(layoutRoot, "page.cshtml"), """<article><h1>@Model.frontmatter.title</h1>@Raw(Model.content)</article>""")
        File.WriteAllText(Path.Combine(assetRoot, "theme.css"), "body { font-family: sans-serif; }\n")

    let useProjectTheme (paths: SitePaths) (themeId: string) =
        resolveProject paths themeId |> ignore
        if not (File.Exists(paths.configFile)) then
            raise (BuildException $"Configuration file not found: '{paths.configFile}'. Run 'init' first.")
        try
            let config = JObject.Parse(File.ReadAllText(paths.configFile))
            config.["theme"] <- JValue(themeId)
            File.WriteAllText(paths.configFile, config.ToString(Formatting.Indented))
        with :? JsonException as ex ->
            raise (BuildException $"Cannot update {paths.configFile}: {ex.Message}")

    let validate (paths: SitePaths) (themeId: string option) =
        match themeId with
        | Some id ->
            let localRoot = SitePaths.inWorkspace paths (Path.Combine("themes", id))
            if Directory.Exists(localRoot) then resolveProject paths id |> ignore
            else resolveBundled paths id |> ignore
        | None ->
            loadCatalog paths |> ignore
            let localThemes = SitePaths.inWorkspace paths "themes"
            if Directory.Exists(localThemes) then
                Directory.GetDirectories(localThemes)
                |> Array.iter (fun directory ->
                    let id = Path.GetFileName(directory)
                    resolveProject paths id |> ignore)

    let validateBundled = validate

    let templatePath (theme: ThemeSource) (relativePath: string) =
        match ensureRelativePath theme.root relativePath with
        | Ok path when File.Exists(path) -> path
        | Ok _ -> raise (BuildException $"Theme '{theme.manifest.id}' is missing template '{relativePath}'.")
        | Error error -> raise (BuildException $"Theme '{theme.manifest.id}': {error}")

    let template (theme: ThemeSource) (relativePath: string option) (fallback: string option) =
        relativePath
        |> Option.orElse fallback
        |> Option.map (templatePath theme)
        |> Option.map File.ReadAllText

    let assetPath (theme: ThemeSource) (relativePath: string) =
        match ensureRelativePath theme.root relativePath with
        | Ok path when File.Exists(path) -> path
        | Ok _ -> raise (BuildException $"Theme '{theme.manifest.id}' is missing asset '{relativePath}'.")
        | Error error -> raise (BuildException $"Theme '{theme.manifest.id}': {error}")

    let assetsRoot (theme: ThemeSource) =
        match ensureRelativePath theme.root "assets" with
        | Ok path when Directory.Exists(path) -> path
        | Ok _ -> raise (BuildException $"Theme '{theme.manifest.id}' is missing its assets directory.")
        | Error error -> raise (BuildException $"Theme '{theme.manifest.id}': {error}")

