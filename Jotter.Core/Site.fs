namespace Jotter.Core

open System
open System.IO
open Jotter.Core.Config
open Jotter.Core.Theme

module Site =
   
   let private copyRootFiles ()=
       File.Copy("robots.txt", $"{Config.contentDirectory}/robots.txt")
       File.Copy("CNAME", $"{Config.contentDirectory}/CNAME")

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

   let init ()=
       clean()
       copyRootFiles()
       Config.init()
       Theme.init()
       Draft.init()
       Page.init()
       Post.init()
       ()
       
