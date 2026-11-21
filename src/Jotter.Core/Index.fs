namespace Jotter.Core

open System.IO
open Document
open Jotter.Core.Config
open Jotter.Core.Assets
open Jotter.Core.Renderer
open Jotter.Core.Layout
open Jotter.Core.IndexPager
open Jotter.Core.Post
open Jotter.Core.ViewModels

module Index =

    type LayoutViewModel = {
        config: ConfigurationModel;
        js: string;
        css: string;
        content: string;
        path: string;
        filename: string;
    } with interface ILayoutViewModel with
             member this.config with get() = this.config
             member this.css with get() = this.css
             member this.js with get() = this.js
             member this.content with get() = this.content

    let private buildIndexPath (paths: SitePaths) (path: string) =
        SitePaths.inPublic paths path
    
    let htmlFileName (paths: SitePaths) (pageNum: int) (config: ConfigurationModel) =
        let blogIndex = config.blogIndex    
        let (num, path) = IndexPager.withIndexNum blogIndex pageNum
        buildIndexPath paths path
        
    let private createLayoutViewModel (context: BuildContext) (outputPath: string) (content: string) =
        {
           config = context.config;
           js = Assets.js context.paths.publicDirectory outputPath;
           css = Assets.css context.paths.publicDirectory outputPath;
           content = content;
           path =
               OutputUrls.page
                   context.config
                   (Path.GetRelativePath(context.paths.publicDirectory, outputPath).Replace('\\', '/'));
           filename = Path.GetFileName(outputPath);
        }
        
    let private writeIndexPage (context: BuildContext) (posts: Post list) (pageNum: int) (isLastPage: bool) =
        let layout = Layout.indexLayout context
        let index = Layout.index context
        let path = htmlFileName context.paths pageNum context.config

        let indexModel = ViewModels.createIndexModel pageNum context.config isLastPage posts
        let indexView = Renderer.render index indexModel

        let layoutModel = createLayoutViewModel context path indexView

        let result = Renderer.render layout layoutModel
        File.WriteAllText(path, result)
        
    let create (context: BuildContext) (posts: Post list) =
        let writePage pageNum isLastPage pagePosts = writeIndexPage context pagePosts pageNum isLastPage
        posts |> Posts.sortByDate context.config |> IndexPager.paginate writePage context.config.postsPerPage 1

