namespace Jotter.Core

open System.IO
open Jotter.Core.Config
open Jotter.Core.IOUtils

module Theme =

    let getTheme (config: ConfigurationModel) = config.theme
    let current (config: ConfigurationModel) = getTheme config

    let available (paths: SitePaths) =
        let dir = DirectoryInfo(paths.bundledThemes)
        dir.GetDirectories() |> Array.map (fun d -> d.Name) |> Array.sort

    let init (paths: SitePaths) =
        let dir = SitePaths.inWorkspace paths "themes"
        Directory.CreateDirectory(dir) |> ignore
        copyDirectory paths.bundledThemes dir

