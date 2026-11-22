namespace Jotter.Core

open System
open System.IO
open Jotter.Core.Site
open Jotter.Core.Theme
open Jotter.Core.Assets
open Jotter.Core.Page

module BuildTask =
    let private deleteIfExists (path: string) =
        if Directory.Exists(path) then
            Directory.Delete(path, true)

    let private render (context: BuildContext) =
        Site.prepare context
        Assets.copy context

        let pages = Page.list context.paths |> Page.prepareAll context
        let posts = Post.list context.paths |> Post.prepareAll context

        pages |> Document.writeAllPages
        posts |> Tags.create context
        posts |> Document.writeAllPosts
        posts |> Rss.create context
        posts |> Index.create context
        posts |> SiteMap.create context

    let private publish (paths: SitePaths) (stagingDirectory: string) =
        let backupDirectory =
            $"{paths.publicDirectory}.previous-{Guid.NewGuid():N}"
        let hadPrevious = Directory.Exists(paths.publicDirectory)

        if hadPrevious then
            Directory.Move(paths.publicDirectory, backupDirectory)

        try
            Directory.Move(stagingDirectory, paths.publicDirectory)
            if hadPrevious then
                Directory.Delete(backupDirectory, true)
        with ex ->
            if hadPrevious && not (Directory.Exists(paths.publicDirectory)) && Directory.Exists(backupDirectory) then
                Directory.Move(backupDirectory, paths.publicDirectory)
            raise (BuildException $"Unable to publish generated site: {ex.Message}")

    let run (paths: SitePaths) =
        let stagingDirectory =
            Path.Combine(paths.root, $".jotter-build-{Guid.NewGuid():N}")
        let stagingPaths = { paths with publicDirectory = stagingDirectory }
        let context = { BuildContext.create paths with paths = stagingPaths }

        try
            render context
            match OutputValidation.validate stagingPaths with
            | Ok() -> publish paths stagingDirectory
            | Error errors ->
                raise
                    (BuildException(
                        "Generated site validation failed:"
                        + Environment.NewLine
                        + String.Join(Environment.NewLine, errors)))
        with ex ->
            deleteIfExists stagingDirectory
            reraise ()

