namespace Jotter.Core

open System
open System.IO

exception BuildException of string

type SitePaths =
    { root: string
      workspace: string
      publicDirectory: string
      configFile: string
      bundledThemes: string
      themeCache: string
      bundledSiteFiles: string }

module SitePaths =
    let private applicationCacheRoot () =
        match Environment.GetEnvironmentVariable("JOTTER_CACHE_DIR") with
        | value when not (String.IsNullOrWhiteSpace(value)) -> value
        | _ ->
            let baseDirectory =
                match Environment.GetEnvironmentVariable("XDG_CACHE_HOME") with
                | value when not (String.IsNullOrWhiteSpace(value)) -> value
                | _ when OperatingSystem.IsWindows() ->
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)
                | _ when OperatingSystem.IsMacOS() ->
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Library", "Caches")
                | _ ->
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".cache")
            Path.Combine(baseDirectory, "Jotter")

    let fromRoot (root: string) =
        let normalizedRoot = Path.GetFullPath(root)
        let workspace = Path.Combine(normalizedRoot, "_workspace")
        { root = normalizedRoot
          workspace = workspace
          publicDirectory = Path.Combine(normalizedRoot, "_public")
          configFile = Path.Combine(workspace, "config.json")
          bundledThemes = Path.Combine(AppContext.BaseDirectory, "themes")
          themeCache = Path.Combine(applicationCacheRoot(), "themes")
          bundledSiteFiles = Path.Combine(AppContext.BaseDirectory, "site_files") }

    let inDirectory (paths: SitePaths) (relativePath: string) =
        Path.Combine(paths.root, relativePath)

    let inWorkspace (paths: SitePaths) (relativePath: string) =
        Path.Combine(paths.workspace, relativePath)

    let inPublic (paths: SitePaths) (relativePath: string) =
        Path.Combine(paths.publicDirectory, relativePath)

