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
      bundledSiteFiles: string }

module SitePaths =
    let fromRoot (root: string) =
        let normalizedRoot = Path.GetFullPath(root)
        let workspace = Path.Combine(normalizedRoot, "_workspace")
        { root = normalizedRoot
          workspace = workspace
          publicDirectory = Path.Combine(normalizedRoot, "_public")
          configFile = Path.Combine(workspace, "config.json")
          bundledThemes = Path.Combine(AppContext.BaseDirectory, "themes")
          bundledSiteFiles = Path.Combine(AppContext.BaseDirectory, "site_files") }

    let inDirectory (paths: SitePaths) (relativePath: string) =
        Path.Combine(paths.root, relativePath)

    let inWorkspace (paths: SitePaths) (relativePath: string) =
        Path.Combine(paths.workspace, relativePath)

    let inPublic (paths: SitePaths) (relativePath: string) =
        Path.Combine(paths.publicDirectory, relativePath)

