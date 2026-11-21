namespace Jotter.Core

open System
open System.IO
open Jotter.Core.Config
open Jotter.Core.Theme
open Jotter.Core.IOUtils

module Site =
   
   let private copyRootFiles (paths: SitePaths)=
       File.Copy(Path.Combine(paths.bundledSiteFiles, "robots.txt"), SitePaths.inWorkspace paths "robots.txt")
       File.Copy(Path.Combine(paths.bundledSiteFiles, "CNAME"), SitePaths.inWorkspace paths "CNAME")

   let clean (paths: SitePaths)=
       printfn "Setting up %s" paths.workspace
       if(Directory.Exists(paths.workspace)) then
           Directory.Delete(paths.workspace, true)
        
       Directory.CreateDirectory(paths.workspace) |> ignore
       
       printfn "Setting up %s" paths.publicDirectory
       if(Directory.Exists(paths.publicDirectory)) then
           Directory.Delete(paths.publicDirectory, true)
       
       Directory.CreateDirectory(paths.publicDirectory) |> ignore

   let prepare (context: BuildContext)=
       if(Directory.Exists(context.paths.publicDirectory)) then
           Directory.Delete(context.paths.publicDirectory, true)
           
       Directory.CreateDirectory(context.paths.publicDirectory) |> ignore

       File.Copy(SitePaths.inWorkspace context.paths "robots.txt", SitePaths.inPublic context.paths "robots.txt")
       File.Copy(SitePaths.inWorkspace context.paths "CNAME", SitePaths.inPublic context.paths "CNAME")
       
       Post.setup context.paths
       Tags.setup context.paths

   let init (paths: SitePaths) (theme: string) (force: bool) =
       if not force && Directory.Exists(paths.workspace) then
           raise (BuildException $"{paths.workspace} already exists. Re-run with --force to overwrite it and delete all existing posts, drafts and pages, or remove it yourself first.")

       // Without this, a typo'd theme name succeeded silently here and only failed later,
       // confusingly, when `build` tried to read a theme directory that was never copied.
       let availableThemes = Theme.available paths
       if not (Array.contains theme availableThemes) then
           raise (BuildException $"""Unknown theme '{theme}'. Available themes: {String.Join(", ", availableThemes)}.""")

       clean paths
       copyRootFiles paths
       Config.init paths theme
       Theme.init paths
       Draft.init paths
       Page.init paths
       Post.init paths
       ()
       
