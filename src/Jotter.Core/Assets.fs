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
        
    // Recursing without filtering by extension pulled in every file under the directory
    // (images, text files, a theme's highlight.js stylesheets under assets/js/styles/...),
    // and flattening to f.Name discarded the subpath, so files with the same name in
    // different subdirectories collided and nested files resolved to a path that doesn't
    // exist (assets/js/agate.css instead of assets/js/styles/agate.css).
    let private assetFiles (dir: string) (assetType: string) (extension: string) =
        let dirInfo = DirectoryInfo(dir)
        let baseLength = dirInfo.FullName.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar).Length + 1
        let files = dirInfo.GetFiles($"*.{extension}", SearchOption.AllDirectories)
        files |> Seq.sortBy (fun f -> f.FullName)
              |> Seq.map (fun f ->
                    let relativePath =
                        f.FullName.Substring(baseLength)
                                  .Replace(Path.DirectorySeparatorChar, '/')
                                  .Replace(Path.AltDirectorySeparatorChar, '/')
                    $"assets/{assetType}/{relativePath}")
              |> Seq.filter (fun f -> Directory.Exists($"{Config.publicDirectory}/{f}") = false)

    let private cssFiles (dir: string) = assetFiles dir "css" "css"

    let private jsFiles (dir: string) = assetFiles dir "js" "js"
         
    let css (startPath: string) =
        let cssDir = $"{Config.publicDirectory}/assets/css"
        let exists = Directory.Exists(cssDir)
        match exists with
            | true -> let links = cssFiles(cssDir) 
                                    |> Seq.map (fun f -> $"<link rel=\"stylesheet\" href=\"{startPath}/{f}\" />")
                      String.Join("\n", links)                      
            | false -> String.Empty
     
    let js (startPath: string) =
        let jsDir = $"{Config.publicDirectory}/assets/js"
        let exists = Directory.Exists(jsDir)
        match exists with
            | true -> let links = jsFiles(jsDir)
                                  |> Seq.map (fun f -> $"<script type=\"text/javascript\" src=\"{startPath}/{f}\"></script>")
                      String.Join("\n", links)
            | false -> String.Empty
            
            
   
  

        
   

