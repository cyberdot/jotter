namespace Jotter.Core

open System.IO
open Jotter.Core.Config
open Jotter.Core.IOUtils

module Theme =
       
    let getTheme (config: ConfigurationModel) = config.theme
    let current = Config.data |> getTheme
    
    let init =
        let dir = $"{Config.contentDirectory}/themes"
        Directory.CreateDirectory(dir) |> ignore
        copyDirectory "themes" dir

