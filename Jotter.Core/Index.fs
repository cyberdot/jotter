namespace Jotter.Core

open System.IO
open Document
open Jotter.Core.Config
open Jotter.Core.Assets
open Jotter.Core.Store
open Jotter.Core.Renderer
open Jotter.Core.Layout
open Jotter.Core.IndexPager
open Jotter.Core.Post

module Index =

    type LayoutViewModel = {
        config: ConfigurationModel;
        js: string;
        css: string;
        content: string;
    } with interface ILayoutViewModel with
             member this.config with get() = this.config
             member this.css with get() = this.css
             member this.js with get() = this.js
             member this.content with get() = this.content

    type IndexViewModel = {
        current: int;
        prev: int;
        prevFileName: string;
        next: int;
        nextFileName: string;
        content: Post list;
        config: ConfigurationModel;
    }
    
    let private buildIndexPath (path: string) = $"{Config.publicDirectory}/{path}"
    
    let htmlFileName (pageNum: int) (config: ConfigurationModel) =
        let blogIndex = config.blogIndex    
        let (num, path) = IndexPager.withIndexNum blogIndex pageNum
        buildIndexPath path         
        
    let private createLayoutViewModel (content: string) (config: ConfigurationModel) =
        {
           config = config; 
           js = Assets.js(); 
           css = Assets.css(); 
           content = content;
        }
        
    let private createIndexViewModel (pageNum: int) (config: ConfigurationModel) (isLastPage: bool) (posts: Post list) =
        let (prev, prevFileName) = IndexPager.previousPage pageNum config;
        let (next, nextFileName) = IndexPager.nextPage pageNum isLastPage config;
        {
          current = pageNum;
          prev = prev;
          prevFileName = prevFileName;
          next = next;
          nextFileName = nextFileName;
          content = posts;
          config = config;
        }
        
    let private writeIndexPage (posts: Post list) (config: ConfigurationModel) (pageNum: int) (isLastPage: bool) =
        let (layout, layoutRenderer) = Layout.indexLayout()
        let (index, indexRenderer) = Layout.index()
        
        let indexModel = createIndexViewModel pageNum config isLastPage posts
        let indexView = Renderer.render index indexModel indexRenderer
        
        let layoutModel = createLayoutViewModel indexView config
        
        let path = htmlFileName pageNum config
        let result = Renderer.render layout layoutModel layoutRenderer
        File.WriteAllText(path, result)
        
    let rec private compileIndex  (config: ConfigurationModel) (pageNum: int) (posts: Post list)=
        if posts.Length = 0 then null
        else
            let postsPerPage = List.truncate config.postsPerPage posts
            
            let mutable remainder = List.empty
            if (config.postsPerPage <= (List.length posts)) then
                remainder <- List.skip config.postsPerPage posts
            
            let lastPage = IndexPager.lastPage remainder
            writeIndexPage postsPerPage config pageNum lastPage
            compileIndex config (pageNum + 1) remainder

            
    let create (posts: Post list) =
        let config = Config.data()
        posts |> Posts.sortByDate |> compileIndex config 1 |> ignore

