namespace Jotter.Core

open System.IO
open System
open Jotter.Core.Config
open Jotter.Core.Post

module Draft =
    let private draftsDirectory (paths: SitePaths) = SitePaths.inWorkspace paths "drafts"
    let private postsDirectory (paths: SitePaths) = SitePaths.inWorkspace paths "posts"

    let init (paths: SitePaths) = Directory.CreateDirectory(draftsDirectory paths) |> ignore

    let create (paths: SitePaths) (title: string) = Post.create paths title true

    let list (paths: SitePaths) =
        IOUtils.discoverMarkdownFiles (draftsDirectory paths)

    let publish (paths: SitePaths) (title: string) =
        let slug = IOUtils.urlSlug title
        if String.IsNullOrWhiteSpace(slug) then
            raise (BuildException $"The draft title '{title}' cannot produce a file name.")
        let candidates =
            [ ".markdown"; ".md" ]
            |> List.map (fun extension -> Path.Combine(draftsDirectory paths, slug + extension))
            |> List.filter File.Exists
        let sourcePath =
            match candidates with
            | [] ->
                raise (BuildException $"No draft found for '{title}' in '{draftsDirectory paths}'")
            | [ path ] -> path
            | paths ->
                let names = String.Join(", ", paths)
                raise (BuildException $"Multiple drafts found for '{title}': {names}")
        let destinationPath = Path.Combine(postsDirectory paths, Path.GetFileName(sourcePath))

        if File.Exists(destinationPath) then
            raise (BuildException $"Cannot publish '{title}' because '{destinationPath}' already exists.")

        File.Move(sourcePath, destinationPath)

