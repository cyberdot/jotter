namespace Jotter.Core

open System
open System.IO
open System.Net
open System.Net.Http
open System.Net.Http.Headers
open System.Security.Cryptography
open System.Text
open System.Threading.Tasks
open Newtonsoft.Json

[<CLIMutable>]
type GitHubContentItem =
    { name: string
      path: string
      [<JsonProperty("type")>] itemType: string
      [<JsonProperty("download_url")>] downloadUrl: string
      url: string }

[<CLIMutable>]
type ThemeCacheMetadata =
    { repository: string
      reference: string
      theme: string
      downloadedAt: DateTimeOffset }

[<CLIMutable>]
type private CachedCatalogEntry =
    { id: string
      name: string
      path: string }

[<CLIMutable>]
type private CachedCatalog =
    { schemaVersion: int
      themes: CachedCatalogEntry array }

module ThemeRepository =
    let catalogFileName = "themes.json"
    let defaultRepository = "cyberdot/jotter"
    let defaultReference = "master"
    let private catalogTtl = TimeSpan.FromHours(24)
    let private maxFiles = 2_000
    let private maxRequests = 200
    let private maxFileBytes = 10L * 1024L * 1024L
    let private maxThemeBytes = 25L * 1024L * 1024L

    let repository () =
        let value =
            match Environment.GetEnvironmentVariable("JOTTER_THEMES_REPOSITORY") with
            | configured when not (String.IsNullOrWhiteSpace(configured)) -> configured.Trim()
            | _ -> defaultRepository
        let parts = value.Split('/', StringSplitOptions.RemoveEmptyEntries)
        if parts.Length <> 2
           || parts |> Array.exists (fun part ->
                part = "."
                || part = ".."
                || part |> Seq.exists (fun c ->
                    not (Char.IsLetterOrDigit(c) || c = '-' || c = '_' || c = '.'))) then
            raise (BuildException $"Invalid theme repository '{value}'. Expected owner/repository.")
        value

    let reference () =
        match Environment.GetEnvironmentVariable("JOTTER_THEMES_REF") with
        | value when not (String.IsNullOrWhiteSpace(value)) -> value.Trim()
        | _ -> defaultReference

    let private safeCacheName (value: string) =
        use hash = SHA256.Create()
        Convert.ToHexString(hash.ComputeHash(Encoding.UTF8.GetBytes(value))).ToLowerInvariant()

    let cacheRoot (paths: SitePaths) =
        Path.Combine(paths.themeCache, safeCacheName (repository()), safeCacheName (reference()))

    let hasLocalCatalog (paths: SitePaths) =
        File.Exists(Path.Combine(paths.bundledThemes, catalogFileName))

    let private httpClient =
        let client = new HttpClient()
        client.Timeout <- TimeSpan.FromSeconds(60.0)
        client.DefaultRequestHeaders.UserAgent.Add(ProductInfoHeaderValue("Jotter", "1.0"))
        client.DefaultRequestHeaders.Accept.Add(MediaTypeWithQualityHeaderValue("application/vnd.github+json"))
        [ "GITHUB_TOKEN"; "GH_TOKEN" ]
        |> List.tryPick (fun name ->
            Environment.GetEnvironmentVariable(name)
            |> Option.ofObj
            |> Option.bind (fun value -> if String.IsNullOrWhiteSpace(value) then None else Some value))
        |> Option.iter (fun token ->
            client.DefaultRequestHeaders.Authorization <- AuthenticationHeaderValue("Bearer", token))
        client

    let private rawUri (relativePath: string) =
        let path =
            relativePath.Split('/')
            |> Array.map Uri.EscapeDataString
            |> String.concat "/"
        Uri(
            $"https://raw.githubusercontent.com/{repository().Trim('/')}/{Uri.EscapeDataString(reference())}/{path}")

    let private apiUri (relativePath: string) =
        let path =
            relativePath.Split('/')
            |> Array.map Uri.EscapeDataString
            |> String.concat "/"
        Uri(
            $"https://api.github.com/repos/{repository().Trim('/')}/contents/{path}?ref={Uri.EscapeDataString(reference())}")

    let private atomicWrite (path: string) (bytes: byte array) =
        let directory = Path.GetDirectoryName(path) |> nonNull
        Directory.CreateDirectory(directory) |> ignore
        let temporary = $"{path}.tmp-{Guid.NewGuid():N}"
        try
            File.WriteAllBytes(temporary, bytes)
            File.Move(temporary, path, true)
        with _ ->
            if File.Exists(temporary) then File.Delete(temporary)
            reraise ()

    let private validateCatalogBytes (bytes: byte array) =
        try
            let catalog = JsonConvert.DeserializeObject<CachedCatalog>(Encoding.UTF8.GetString(bytes))
            if isNull (box catalog) || catalog.schemaVersion <> 1 then
                raise (BuildException "The remote theme catalog uses an unsupported schema.")
            if isNull catalog.themes then
                raise (BuildException "The remote theme catalog has no themes array.")
            let ids = Collections.Generic.HashSet<string>(StringComparer.OrdinalIgnoreCase)
            let paths = Collections.Generic.HashSet<string>(StringComparer.OrdinalIgnoreCase)
            for entry in catalog.themes do
                if String.IsNullOrWhiteSpace(entry.id)
                   || entry.id |> Seq.exists (fun c ->
                        not (Char.IsLetterOrDigit(c) || c = '-' || c = '_')) then
                    raise (BuildException $"The remote theme catalog contains invalid theme id '{entry.id}'.")
                if String.IsNullOrWhiteSpace(entry.name) then
                    raise (BuildException $"The remote theme catalog theme '{entry.id}' has no name.")
                if not (ids.Add(entry.id)) then
                    raise (BuildException $"The remote theme catalog contains duplicate id '{entry.id}'.")
                if String.IsNullOrWhiteSpace(entry.path) || Path.IsPathFullyQualified(entry.path) then
                    raise (BuildException $"The remote theme catalog contains invalid path '{entry.path}'.")
                let normalized = entry.path.Replace('\\', '/')
                if normalized.Split('/') |> Array.exists (fun part -> part = "." || part = ".." || String.IsNullOrWhiteSpace(part)) then
                    raise (BuildException $"The remote theme catalog contains unsafe path '{entry.path}'.")
                if not (paths.Add(normalized)) then
                    raise (BuildException $"The remote theme catalog contains duplicate path '{entry.path}'.")
        with :? JsonException as ex ->
            raise (BuildException $"The remote theme catalog is invalid JSON: {ex.Message}")

    let private getBytes (uri: Uri) =
        try
            if uri.Host <> "api.github.com" && uri.Host <> "raw.githubusercontent.com" then
                raise (BuildException $"Refusing to request theme content from untrusted host '{uri.Host}'.")
            use response = httpClient.GetAsync(uri).GetAwaiter().GetResult()
            if not response.IsSuccessStatusCode then
                let rateLimited =
                    response.StatusCode = HttpStatusCode.Forbidden
                    && response.Headers.Contains("X-RateLimit-Remaining")
                let detail =
                    if rateLimited then
                        "GitHub API rate limit exceeded. Set GITHUB_TOKEN or GH_TOKEN to authenticate requests."
                    elif response.StatusCode = HttpStatusCode.NotFound then
                        $"Theme content was not found at repository '{repository()}' ref '{reference()}'."
                    else
                        $"GitHub returned HTTP {(int response.StatusCode)}."
                raise (BuildException $"Cannot download {uri}: {detail}")
            response.Content.ReadAsByteArrayAsync().GetAwaiter().GetResult()
        with
        | :? BuildException -> reraise ()
        | :? HttpRequestException as ex ->
            raise (BuildException $"Cannot access GitHub repository '{repository()}' at ref '{reference()}': {ex.Message}")
        | :? TaskCanceledException ->
            raise (BuildException $"Timed out accessing GitHub repository '{repository()}'.")

    let ensureCatalog (paths: SitePaths) (refresh: bool) =
        if hasLocalCatalog paths then
            paths.bundledThemes
        else
            let root = cacheRoot paths
            let path = Path.Combine(root, catalogFileName)
            let expired =
                File.Exists(path)
                && DateTime.UtcNow - File.GetLastWriteTimeUtc(path) > catalogTtl
            if refresh || not (File.Exists(path)) || expired then
                try
                    let bytes = getBytes (rawUri $"themes/{catalogFileName}")
                    if bytes.LongLength > 1L * 1024L * 1024L then
                        raise (BuildException "The remote theme catalog exceeds the 1 MB size limit.")
                    validateCatalogBytes bytes
                    atomicWrite path bytes
                with :? BuildException as ex ->
                    if File.Exists(path) then Console.Error.WriteLine($"warning: {ex.Message} Using cached theme catalog.")
                    else reraise ()
            root

    let private parseDirectoryResponse (bytes: byte array) =
        try
            let items = JsonConvert.DeserializeObject<GitHubContentItem array>(Encoding.UTF8.GetString(bytes))
            if isNull items then [||] else items
        with :? JsonException as ex ->
            raise (BuildException $"GitHub returned invalid directory metadata: {ex.Message}")

    let private safeRelativePath (root: string) (relativePath: string) =
        if Path.IsPathFullyQualified(relativePath) then
            raise (BuildException $"GitHub returned an absolute path: '{relativePath}'.")
        let rootPath = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar)
        let candidate = Path.GetFullPath(Path.Combine(rootPath, relativePath.Replace('/', Path.DirectorySeparatorChar)))
        if not (candidate.StartsWith(rootPath + string Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)) then
            raise (BuildException $"GitHub returned a path outside the theme directory: '{relativePath}'.")
        candidate

    let private markerPath (root: string) =
        root + ".complete.json"

    let themeIsCached (paths: SitePaths) (themePath: string) =
        let root = Path.Combine(cacheRoot paths, themePath)
        let marker = markerPath root
        if not (Directory.Exists(root) && File.Exists(marker)) then
            false
        else
            try
                let metadata = JsonConvert.DeserializeObject<ThemeCacheMetadata>(File.ReadAllText(marker))
                not (isNull (box metadata))
                && metadata.repository = repository()
                && metadata.reference = reference()
                && metadata.theme = themePath
            with
            | :? JsonException
            | :? IOException -> false

    let private downloadThemeDirectory
        (themePath: string)
        (destination: string)
        (validate: string -> unit) =
        let temporary = destination + ".tmp-" + Guid.NewGuid().ToString("N")
        let temporaryMarker = markerPath temporary
        let backup = destination + ".previous-" + Guid.NewGuid().ToString("N")
        let backupMarker = markerPath backup
        Directory.CreateDirectory(temporary) |> ignore
        let pending = ResizeArray<Uri>()
        pending.Add(apiUri $"themes/{themePath}")
        let mutable requests = 0
        let mutable files = 0
        let mutable bytes = 0L
        let prefix = $"themes/{themePath}/"
        try
            while pending.Count > 0 do
                requests <- requests + 1
                if requests > maxRequests then
                    raise (BuildException $"Theme '{themePath}' exceeds the directory request limit.")
                let uri = pending.[pending.Count - 1]
                pending.RemoveAt(pending.Count - 1)
                let items = getBytes uri |> parseDirectoryResponse
                for item in items do
                    let path = item.path.Replace('\\', '/')
                    if not (path.StartsWith(prefix, StringComparison.Ordinal)) then
                        raise (BuildException $"GitHub returned a path outside the selected theme: '{item.path}'.")
                    let relative = path.Substring(prefix.Length)
                    let target = safeRelativePath temporary relative
                    match item.itemType with
                    | "dir" ->
                        Directory.CreateDirectory(target) |> ignore
                        pending.Add(Uri(item.url))
                    | "file" ->
                        files <- files + 1
                        if files > maxFiles then
                            raise (BuildException $"Theme '{themePath}' contains too many files.")
                        if String.IsNullOrWhiteSpace(item.downloadUrl) then
                            raise (BuildException $"GitHub did not provide a download URL for '{item.path}'.")
                        let content = getBytes (Uri(item.downloadUrl))
                        bytes <- bytes + int64 content.Length
                        if int64 content.Length > maxFileBytes || bytes > maxThemeBytes then
                            raise (BuildException $"Theme '{themePath}' exceeds the download size limits.")
                        atomicWrite target content
                    | value ->
                        raise (BuildException $"Theme '{themePath}' uses unsupported GitHub content type '{value}'.")
            validate temporary
            let metadata =
                { repository = repository()
                  reference = reference()
                  theme = themePath
                  downloadedAt = DateTimeOffset.UtcNow }
            atomicWrite
                temporaryMarker
                (JsonConvert.SerializeObject(metadata, Formatting.Indented) |> Encoding.UTF8.GetBytes)
            let hadDestination = Directory.Exists(destination)
            let hadMarker = File.Exists(markerPath destination)
            if hadDestination then Directory.Move(destination, backup)
            if hadMarker then File.Move(markerPath destination, backupMarker)
            try
                Directory.Move(temporary, destination)
                File.Move(temporaryMarker, markerPath destination)
                if hadDestination then Directory.Delete(backup, true)
                if hadMarker then File.Delete(backupMarker)
            with ex ->
                if Directory.Exists(destination) then Directory.Delete(destination, true)
                if File.Exists(markerPath destination) then File.Delete(markerPath destination)
                if hadDestination && Directory.Exists(backup) then Directory.Move(backup, destination)
                if hadMarker && File.Exists(backupMarker) then File.Move(backupMarker, markerPath destination)
                raise ex
            destination
        with _ ->
            if Directory.Exists(temporary) then Directory.Delete(temporary, true)
            if File.Exists(temporaryMarker) then File.Delete(temporaryMarker)
            reraise ()

    let ensureTheme (paths: SitePaths) (themePath: string) (refresh: bool) (validate: string -> unit) =
        if hasLocalCatalog paths then
            let localRoot = Path.Combine(paths.bundledThemes, themePath)
            if not (Directory.Exists(localRoot)) then
                raise (BuildException $"Theme directory '{themePath}' is missing from the local development catalog.")
            localRoot
        else
            ensureCatalog paths false |> ignore
            let destination = Path.Combine(cacheRoot paths, themePath)
            if refresh || not (themeIsCached paths themePath) then
                downloadThemeDirectory themePath destination validate
            else
                validate destination
                destination

    let invalidateTheme (paths: SitePaths) (themePath: string) =
        let destination = Path.Combine(cacheRoot paths, themePath)
        if Directory.Exists(destination) then Directory.Delete(destination, true)
        let marker = markerPath destination
        if File.Exists(marker) then File.Delete(marker)

    let refresh (paths: SitePaths) (themePath: string option) (validate: string -> unit) =
        if hasLocalCatalog paths then
            raise (BuildException "The local development theme catalog is already used directly and cannot be refreshed.")
        ensureCatalog paths true |> ignore
        themePath |> Option.iter (fun path -> ensureTheme paths path true validate |> ignore)

