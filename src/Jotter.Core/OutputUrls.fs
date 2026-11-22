namespace Jotter.Core

open System
open Jotter.Core.Config

module OutputUrls =
    let private combine (config: ConfigurationModel) (relativePath: string) =
        let root = Uri(config.url.TrimEnd('/') + "/", UriKind.Absolute)
        Uri(root, relativePath.TrimStart('/')).ToString()

    let home (config: ConfigurationModel) =
        config.url.TrimEnd('/')

    let page (config: ConfigurationModel) (fileName: string) =
        combine config fileName

    let post (config: ConfigurationModel) (fileName: string) =
        combine config $"posts/{fileName}"

    let tag (config: ConfigurationModel) (tag: string) =
        combine config $"tags/{IOUtils.urlSlug tag}.html"

    let feed (config: ConfigurationModel) =
        combine config "blog.rss"

