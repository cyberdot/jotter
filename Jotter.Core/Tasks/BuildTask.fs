namespace Jotter.Core

open Jotter.Core.Site
open Jotter.Core.Theme
open Jotter.Core.Assets
open Jotter.Core.Page

module BuildTask =
    
    let run () =
        Site.prepare()
        Assets.copy()
        
        let mutable pages: Document.Page list = []
        let mutable posts: Document.Post list = []
        
        for filepath in Page.list() do pages <- Page.prepare filepath pages
        for filepath in Post.list() do posts <- Post.prepare filepath posts
            
        pages |> Document.writeAllPages
        
        posts |> Tags.create
        posts |> Document.writeAllPosts
        posts |> Rss.create
        posts |> Index.create
        
        SiteMap.create()
        ()
        

