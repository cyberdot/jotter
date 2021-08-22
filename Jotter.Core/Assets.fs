namespace Jotter.Core

open System
open System.IO
open Jotter.Core.Config
open Jotter.Core.Theme
open Jotter.Core.IOUtils

module Assets =
    
    let copy () =
        let source = $"{Config.contentDirectory}/themes/{Theme.current()}/assets"
        let destination = $"{Config.publicDirectory}/assets"
        IOUtils.copyDirectory source destination
        
    let private cssFiles (dir: string) =
        let dirInfo = new DirectoryInfo(dir)
        let files = dirInfo.GetFiles(".", SearchOption.AllDirectories)
        files |> Seq.sortBy (fun f -> f.Name)
              |> Seq.map (fun f -> $"/assets/css/{f.Name}")
              |> Seq.filter (fun f -> Directory.Exists($"{Config.publicDirectory}/{f}") = false)
              
    let private jsFiles (dir: string) =
        let dirInfo = DirectoryInfo(dir)
        let files = dirInfo.GetFiles(".", SearchOption.AllDirectories)
        files |> Seq.sortBy (fun f -> f.Name)
              |> Seq.map (fun f -> $"assets/js/{f.Name}")
              |> Seq.filter (fun f -> Directory.Exists($"{Config.publicDirectory}/{f}") = false)
         
    let css () =
        let cssDir = $"{Config.publicDirectory}/assets/css"
        let exists = Directory.Exists(cssDir)
        match exists with
            | true -> let links = cssFiles(cssDir) 
                                    |> Seq.map (fun f -> $"<link rel=\"stylesheet\" href=\"{f}\" />")
                      String.Join("\n", links)                      
            | false -> String.Empty
     
    let js () =
        let jsDir = $"{Config.publicDirectory}/assets/js"
        let exists = Directory.Exists(jsDir)
        match exists with
            | true -> let links = jsFiles(jsDir)
                                  |> Seq.map (fun f -> $"<script type=\"text/javascript\" src=\"{f}\"></script>")
                      String.Join("\n", links)
            | false -> String.Empty
            
            
   
  

        
   

