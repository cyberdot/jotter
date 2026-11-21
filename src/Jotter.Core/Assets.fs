namespace Jotter.Core

open System
open System.IO

module Assets =
    let copy (context: BuildContext) =
        let source = Theme.assetsRoot context.theme
        let destination = SitePaths.inPublic context.paths "assets"
        IOUtils.copyDirectory source destination

    let private relativeOutputPath (relativePath: string) =
        relativePath
            .Replace('\\', '/')
            .TrimStart('/')

    let private declaredPaths (context: BuildContext) paths =
        paths
        |> List.map (fun path ->
            Theme.assetPath context.theme path |> ignore
            relativeOutputPath path)

    let cssLinks (context: BuildContext) =
        context.theme.manifest.styles
        |> declaredPaths context
        |> List.map (fun path -> $"<link rel=\"stylesheet\" href=\"/{path}\" />")
        |> String.concat Environment.NewLine

    let jsLinks (context: BuildContext) =
        context.theme.manifest.scripts
        |> declaredPaths context
        |> List.map (fun path -> $"<script type=\"text/javascript\" src=\"/{path}\"></script>")
        |> String.concat Environment.NewLine

    let css (context: BuildContext) = cssLinks context
    let js (context: BuildContext) = jsLinks context
