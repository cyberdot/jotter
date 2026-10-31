namespace Jotter.Core

open System
open System.IO
open Jotter.Core.Config
open Jotter.Core.Theme
open Jotter.Core.IOUtils

module Site =
   
   let private copyRootFiles ()=
       File.Copy(IOUtils.resolveAppPath "site_files/robots.txt", $"{Config.contentDirectory}/robots.txt")
       File.Copy(IOUtils.resolveAppPath "site_files/CNAME", $"{Config.contentDirectory}/CNAME")

   let clean ()=       
       printfn "Setting up %s" Config.contentDirectory
       if(Directory.Exists(Config.contentDirectory)) then
           Directory.Delete(Config.contentDirectory, true)
        
       Directory.CreateDirectory(Config.contentDirectory) |> ignore
       
       printfn "Setting up %s" Config.publicDirectory
       if(Directory.Exists(Config.publicDirectory)) then
           Directory.Delete(Config.publicDirectory, true)
       
       Directory.CreateDirectory(Config.publicDirectory) |> ignore

   let prepare ()=
       if(Directory.Exists(Config.publicDirectory)) then
           Directory.Delete(Config.publicDirectory, true)
           
       Directory.CreateDirectory(Config.publicDirectory) |> ignore

       File.Copy($"{Config.contentDirectory}/robots.txt", $"{Config.publicDirectory}/robots.txt")
       File.Copy($"{Config.contentDirectory}/CNAME", $"{Config.publicDirectory}/CNAME")
       
       Post.setup()
       Tags.setup()

   let init (theme: string) (force: bool) =
       if not force && Directory.Exists(Config.contentDirectory) then
           failwith $"{Config.contentDirectory} already exists. Re-run with --force to overwrite it and delete all existing posts, drafts and pages, or remove it yourself first."

       // Without this, a typo'd theme name succeeded silently here and only failed later,
       // confusingly, when `build` tried to read a theme directory that was never copied.
       let availableThemes = Theme.available()
       if not (Array.contains theme availableThemes) then
           failwith $"""Unknown theme '{theme}'. Available themes: {String.Join(", ", availableThemes)}."""

       clean()
       copyRootFiles()
       Config.init theme
       Theme.init()
       Draft.init()
       Page.init()
       Post.init()
       ()
       
