namespace Jotter.Core

open System.IO
open Jotter.Core.Config
open Jotter.Core.Post

module Draft =
    let private draftsDirectory = $"{Config.contentDirectory}/drafts"
    let private postsDirectory = $"{Config.contentDirectory}/posts"

    let init () = Directory.CreateDirectory(draftsDirectory) |> ignore

    let create (title: string) = Post.create title true

    let list () = Directory.GetFiles(draftsDirectory, "*.markdown", SearchOption.AllDirectories)

    let publish (title: string) =
        let fileName = IOUtils.slugFileName title
        let sourcePath = $"{draftsDirectory}/{fileName}"
        let destinationPath = $"{postsDirectory}/{fileName}"

        if not (File.Exists(sourcePath)) then
            failwith $"No draft found for '{title}' ({sourcePath})"

        File.Move(sourcePath, destinationPath)

