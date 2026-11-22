namespace Jotter.Core

open System

module Layout =
    let private readRequired (context: BuildContext) (relativePath: string) =
        Theme.template context.theme (Some relativePath) None
        |> Option.defaultWith (fun () ->
            raise (BuildException $"Theme '{context.theme.manifest.id}' requires template '{relativePath}'."))

    let private resolveLayout (context: BuildContext) (relativePath: string option) =
        Theme.template context.theme relativePath (Some context.theme.manifest.templates.baseTemplate)
        |> Option.defaultWith (fun () ->
            raise (BuildException $"Theme '{context.theme.manifest.id}' has no base layout."))

    let tagIndex (context: BuildContext) =
        Theme.template context.theme context.theme.manifest.templates.tagIndex None

    let tagLayout (context: BuildContext) =
        resolveLayout context context.theme.manifest.templates.tagLayout

    let indexLayout (context: BuildContext) =
        resolveLayout context context.theme.manifest.templates.indexLayout

    let index (context: BuildContext) =
        readRequired context context.theme.manifest.templates.index

    let pageLayout (context: BuildContext) =
        resolveLayout context context.theme.manifest.templates.pageLayout

    let page (context: BuildContext) =
        readRequired context context.theme.manifest.templates.page

    let postLayout (context: BuildContext) =
        resolveLayout context context.theme.manifest.templates.postLayout

    let post (context: BuildContext) =
        readRequired context context.theme.manifest.templates.post

    let layout (context: BuildContext) =
        readRequired context context.theme.manifest.templates.baseTemplate
