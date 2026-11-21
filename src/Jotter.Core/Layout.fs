namespace Jotter.Core

open System.IO
open Jotter.Core.Theme
open Jotter.Core.Config

module Layout =

    let private basePath (context: BuildContext) =
        SitePaths.inWorkspace context.paths $"themes/{context.config.theme}/layout"

    let private toFileName (context: BuildContext) (template: string) =
        let dirInfo = new DirectoryInfo(basePath context)
        let files = dirInfo.GetFiles() |> Array.map (fun fileInfo -> fileInfo.Name)
        let result = files |> Array.tryFind (fun f -> Path.ChangeExtension(f, null) = template)
        match result with
           | Some fileName -> fileName
           | None -> ""


    let private toPath (context: BuildContext) (fileName: string) =
        if System.String.IsNullOrWhiteSpace(fileName) then null
        else Path.Combine(basePath context, fileName)

    // RazorLight renders every template the same way regardless of its file extension, so
    // only the content itself is worth returning - the extension used to be threaded all
    // the way out to Renderer.render, which ignored it too.
    let rec private read (context: BuildContext) (fallback: string) (path: string) : string =
       if (System.String.IsNullOrWhiteSpace(fallback)) then
           File.ReadAllText(path)
       elif(System.String.IsNullOrWhiteSpace(path)) then
           fallback |> toFileName context |> toPath context |> read context null
       else read context null path


    let private load (context: BuildContext) (template: string) (fallback: string) : string =
       let fileP = template |> toFileName context |> toPath context
       if(fileP = null && System.String.IsNullOrWhiteSpace(fallback)) then
           null
       else
           fileP |> read context fallback

    
    let private resolveLayout (context: BuildContext) (template: string) =
        load context template "layout"
    
    
    let tagIndex (context: BuildContext) = load context "tag_index" ""
    let tagLayout (context: BuildContext) = resolveLayout context "tag_layout"
    
    let indexLayout (context: BuildContext) = resolveLayout context "index_layout"
    let index (context: BuildContext) = load context "index" ""
    
    let pageLayout (context: BuildContext)= resolveLayout context "page_layout"
    let page (context: BuildContext) = load context "page" ""
    
    let postLayout (context: BuildContext) = resolveLayout context "post_layout"
    let post (context: BuildContext) = load context "post" ""
    
    let layout (context: BuildContext)= load context "layout" ""
