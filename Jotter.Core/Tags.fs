namespace Jotter.Core

open System.IO
open Jotter.Core.Config
open Jotter.Core.IndexPager
open Jotter.Core.Assets 
open Jotter.Core.Document
open Jotter.Core.Layout

module Tags =
    
    type TagLayoutViewModel = {
        config: ConfigurationModel;
        js: string;
        css: string;
        content: string;
        tag: string;
        path: string;
        filename: string;        
    } with interface ILayoutViewModel with
             member this.config with get() = this.config
             member this.js with get() = this.js
             member this.css with get() = this.css
             member this.content with get() = this.content
             
             
    type TagIndexViewModel = {
        current: int;
        prev: int;
        prevFilePath: string;
        next: int;
        nextFilePath: string;
        content: Post list
        config: ConfigurationModel        
    }
             

    let private buildIndexPath (path: string) = $"{Config.publicDirectory}/{path}"

    let private htmlFilename (pageNum: int) (config: ConfigurationModel) =
        let (_, path) = IndexPager.withIndexNum config.blogIndex pageNum
        buildIndexPath path
    
    let private createLayoutViewModel (content: string) (config: ConfigurationModel) (tag: string) =
        {
           config = config;
           js = Assets.js();
           css = Assets.css();
           content = content;
           tag = tag;
           path = "";
           filename = "";
        }

    let private createIndexViewModel (pageNum: int) (config: ConfigurationModel) (isLastPage: bool) (posts: Post list) =
        let (prev, prevFilePath) = IndexPager.previousPage pageNum config
        let (next, nextFilePath) = IndexPager.nextPage pageNum isLastPage config
        {
           current = pageNum;
           prev = prev;
           prevFilePath = prevFilePath;
           next = next;
           nextFilePath = nextFilePath;
           content = posts;
           config = config;
        }

    let private writeIndexPage (posts: Post list) (tag:string) (config: ConfigurationModel) (pageNum: int) (isLastPage: bool) =
        let (layout, layoutRenderer) = Layout.tagLayout()
        let (index, indexRenderer) = Layout.index()

        let indexModel = createIndexViewModel pageNum config isLastPage posts
        let indexView = Renderer.render index indexModel indexRenderer

        let layoutModel = createLayoutViewModel indexView config tag
        let layoutView = Renderer.render layout layoutModel layoutRenderer

        let filename = htmlFilename pageNum config
        File.WriteAllText(filename, layoutView)

    let rec private compileIndex (tag: string) (config: ConfigurationModel) (pageNum: int) (posts: Post list) =
        if posts.Length = 0 then ()
        else 
           let postsPerPage = List.truncate config.postsPerPage posts
           
           let mutable remainder = List.empty
           if(config.postsPerPage <= (List.length posts)) then
               remainder <- List.skip config.postsPerPage posts
           let isLastPage = IndexPager.lastPage remainder
           writeIndexPage postsPerPage tag config pageNum isLastPage
           compileIndex tag config (pageNum + 1) remainder

    let private createIndexPerTag (posts: Post list) (tag: string) (config: ConfigurationModel) =
        posts |> Posts.sortByDate |> compileIndex tag config 1

    let private renderPostsPerTag (tag: string) (posts: Post list) =
        let slug = IOUtils.urlSlug tag
        let config = Config.data()
        let updatedConfig = {config with blogIndex = $"tags/{tag}.html" }
        let filteredPosts = List.filter (fun p -> Array.exists (fun x -> x = tag) p.frontmatter.tags) posts
        createIndexPerTag filteredPosts tag updatedConfig

    let private createTagsIndexPage (tags: string array) =
        let (layout, layoutRenderer) = Layout.layout()
        let (index, indexRenderer) = Layout.tagIndex()
        
        let indexView = Renderer.render index {| content = tags; |} indexRenderer
        let layoutModel = {|
            config = Config.data();
            content = indexView;
            filename = "index.html"
            css = Assets.css();        
        |}

        let path = $"{Config.publicDirectory}/tags/index.html"
        let renderedView = Renderer.render layout layoutModel layoutRenderer
        File.WriteAllText(path, renderedView)

    let create (posts: Post list) =       

        let tags = posts 
                    |> List.map (fun p -> p.frontmatter.tags)
                    |> Array.concat
                    |> Array.distinct
                    |> Array.sort
        createTagsIndexPage tags
        tags |> Array.iter (fun t -> renderPostsPerTag t posts)
        
    let setup () =
         
         let tagsDir = $"{Config.publicDirectory}/tags"
         if(Directory.Exists(tagsDir)) then
             Directory.Delete(tagsDir, true)
            
         Directory.CreateDirectory(tagsDir) |> ignore

    

