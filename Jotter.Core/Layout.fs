namespace Jotter.Core

open System.IO
open Jotter.Core.Theme
open Jotter.Core.Config

module Layout = 
        
    let private basePath = $"{Config.contentDirectory}/themes/{Theme.current()}/layout"

    let private toFileName (template: string) =
        let dirInfo = new DirectoryInfo(basePath)
        let files = dirInfo.GetFiles() |> Array.map (fun fileInfo -> fileInfo.Name)
        let result = files |> Array.tryFind (fun f -> Path.ChangeExtension(f, null) = template)
        match result with
           | Some fileName -> fileName
           | None -> ""
        
   
    let private determineRenderer (content: string, path: string) =
        let renderer = Path.GetExtension(path)
        (content, renderer)
     
     
    let private toPath (fileName: string) =
        if System.String.IsNullOrWhiteSpace(fileName) then null
        else $"{basePath}/{fileName}"    
   
    let rec read (fallback: string) (path: string) =
       if (System.String.IsNullOrWhiteSpace(fallback)) then
           (File.ReadAllText(path), path)
       elif(System.String.IsNullOrWhiteSpace(path)) then
           fallback |> toFileName |> toPath |> read null 
       else read null path           

        
    let private load (template: string) (fallback: string) =
       let fileP = template |> toFileName |> toPath
       if(fileP = null && System.String.IsNullOrWhiteSpace(fallback)) then
           (null, null)
       else
           fileP |> read fallback |> determineRenderer

    
    let private resolveLayout (template: string) = load template "layout"  
    
    
    let tagIndex () = load "tag_index" ""
    let tagLayout () = resolveLayout "tag_layout"
    
    let indexLayout () = resolveLayout "index_layout"
    let index () = load "index" ""
    
    let pageLayout ()= resolveLayout "page_layout"
    let page () = load "page" ""
    
    let postLayout () = resolveLayout "post_layout"
    let post () = load "post" ""
    
    let layout ()= load "layout" ""
    
    let path = $"{Config.contentDirectory}/themes/{Theme.current()}/layout/layout.cshtml"
    
    