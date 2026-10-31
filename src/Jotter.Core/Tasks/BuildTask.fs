namespace Jotter.Core

open Jotter.Core.Site
open Jotter.Core.Theme
open Jotter.Core.Assets
open Jotter.Core.Page

module BuildTask =
    
    let run () =
        Site.prepare()
        Assets.copy()
        
        let pages = Page.list() |> Page.prepareAll
        let posts = Post.list() |> Post.prepareAll

        pages |> Document.writeAllPages
        
        posts |> Tags.create
        posts |> Document.writeAllPosts
        posts |> Rss.create
        posts |> Index.create
        
        posts |> SiteMap.create
        ()
        

