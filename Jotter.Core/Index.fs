namespace Jotter.Core

open System.IO
open Document
open Jotter.Core.Config
open Jotter.Core.Assets
open Jotter.Core.Store
open Jotter.Core.Renderer
open Jotter.Core.Layout
open Jotter.Core.IndexPager

module Index =
    
    let private buildIndexPath (path: string) = "{Config.publicDirectory}/{path}"
    
    let htmlFileName (pageNum: int) (config: ConfigurationModel) =
        ()
        
        
    let private createLayoutViewModel (content: string) (config: ConfigurationModel) =
        {|
           config = config; js = Assets.js; css = Assets.css; content = content;
        |}
        
    let private createIndexViewModel (pageNum: int) (config: ConfigurationModel) (isLastPage: bool) (posts: FrontMatter list) =
        {|
          current = pageNum;
          prev = IndexPager.previousPage pageNum config;
          next = IndexPager.nextPage pageNum isLastPage config;
          content = posts;
          config = config;
        |}
        
    let private writeIndexPage (posts: FrontMatter list) (config: ConfigurationModel) (pageNum: int) (isLastPage: bool) =
        let (layout, layoutRenderer) = Layout.indexLayout
        let (index, indexRenderer) = Layout.index
        
        let indexModel = createIndexViewModel pageNum config isLastPage posts
        let indexView = Renderer.render index indexModel indexRenderer
        
        let layoutModel = createLayoutViewModel indexView config
        
        let path = Index.htmlFilename pageNum config
        let result = Renderer.render layout layoutModel layoutRenderer
        File.WriteAllText(path, result)
        
    let rec private compileIndex  (config: ConfigurationModel) (pageNum: int) (posts: FrontMatter list)=
        if posts.Length = 0 then null
        else
            let postsPerPage = List.take config.postsPerPage posts
            let remainder = List.skip config.postsPerPage posts
            let lastPage = IndexPager.lastPage remainder
            writeIndexPage postsPerPage config pageNum lastPage
            compileIndex remainder config (pageNum + 1)
            
    let create (posts: FrontMatter list) (config: ConfigurationModel) =
        posts |> Posts.sortByDate |> compileIndex config 1

