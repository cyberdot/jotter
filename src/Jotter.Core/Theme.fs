namespace Jotter.Core

open System.IO
open Jotter.Core.Config
open Jotter.Core.IOUtils

module Theme =

    let getTheme (config: ConfigurationModel) = config.theme
    let current () = Config.data() |> getTheme

    let available () =
        let dir = DirectoryInfo(IOUtils.resolveAppPath "themes")
        dir.GetDirectories() |> Array.map (fun d -> d.Name) |> Array.sort

    let init () =
        let dir = $"{Config.contentDirectory}/themes"
        Directory.CreateDirectory(dir) |> ignore
        copyDirectory (IOUtils.resolveAppPath "themes") dir

