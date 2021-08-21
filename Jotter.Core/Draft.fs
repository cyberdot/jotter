namespace Jotter.Core

open System.IO
open System.IO
open Jotter.Core.Config
open Jotter.Core.Post

module Draft =
    let init () = Directory.CreateDirectory($"{Config.contentDirectory}/drafts") |> ignore
    
    let create (title: string) = Post.create title true
    
    let list () = Directory.GetFiles($"{Config.contentDirectory}/drafts", ".", SearchOption.AllDirectories)

