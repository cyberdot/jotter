namespace Jotter.Core

open System.IO
open Jotter.Core.Config
open Jotter.Core.IndexPager
open Jotter.Core.Assets 
open Jotter.Core.Document
open Jotter.Core.Layout
open Jotter.Core.ViewModels

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
             
    let private buildIndexPath (paths: SitePaths) (path: string) =
        SitePaths.inPublic paths path

    let private htmlFilename (paths: SitePaths) (pageNum: int) (config: ConfigurationModel) =
        let (_, path) = IndexPager.withIndexNum config.blogIndex pageNum
        buildIndexPath paths path
    
    let private createLayoutViewModel (context: BuildContext) (outputPath: string) (content: string) (tag: string) (pageConfig: ConfigurationModel) =
        let relativePath =
            Path.GetRelativePath(context.paths.publicDirectory, outputPath).Replace('\\', '/')
        {
           config = context.config;
           js = Assets.js context.paths.publicDirectory outputPath;
           css = Assets.css context.paths.publicDirectory outputPath;
           content = content;
           tag = tag;
           path = OutputUrls.page pageConfig relativePath;
           filename = Path.GetFileName(outputPath);
        }

    let private writeIndexPage (context: BuildContext) (posts: Post list) (tag:string) (pageConfig: ConfigurationModel) (pageNum: int) (isLastPage: bool) =
        let layout = Layout.tagLayout context
        let index = Layout.index context
        let filename = htmlFilename context.paths pageNum pageConfig

        let indexModel = ViewModels.createIndexModel pageNum pageConfig isLastPage posts
        let indexView = Renderer.render index indexModel

        let layoutModel = createLayoutViewModel context filename indexView tag pageConfig
        let layoutView = Renderer.render layout layoutModel

        File.WriteAllText(filename, layoutView)

    let private createIndexPerTag (context: BuildContext) (posts: Post list) (tag: string) (pageConfig: ConfigurationModel) =
        let writePage pageNum isLastPage pagePosts = writeIndexPage context pagePosts tag pageConfig pageNum isLastPage
        posts |> Posts.sortByDate context.config |> IndexPager.paginate writePage pageConfig.postsPerPage 1

    // Tags.create writes each tag's index to this path (via IOUtils.urlSlug), so any link
    // pointing at a tag page must be built from the same function or the link 404s.
    let tagIndexFileName (tag: string) = $"tags/{IOUtils.urlSlug tag}.html"

    let private renderPostsPerTag (context: BuildContext) (tag: string) (posts: Post list) =
        let updatedConfig = {context.config with blogIndex = tagIndexFileName tag }
        let filteredPosts = List.filter (fun p -> Array.exists (fun x -> x = tag) p.frontmatter.tags) posts
        createIndexPerTag context filteredPosts tag updatedConfig

    let private createTagsIndexPage (context: BuildContext) (tags: string array) =
        let layout = Layout.layout context
        let index =
            match Layout.tagIndex context with
            | null ->
                """<ul>@foreach(var tag in Model.content) { <li><a href="/tags/@(Jotter.Core.IOUtils.urlSlug(tag)).html">@tag</a></li> }</ul>"""
            | template -> template

        let indexModel: TagIndexListTemplateModel = { content = tags }
        let indexView = Renderer.render index indexModel
        let path = SitePaths.inPublic context.paths "tags/index.html"
        let layoutModel: TagIndexLayoutModel =
            { content = indexView
              config = context.config
              css = Assets.css context.paths.publicDirectory path
              js = Assets.js context.paths.publicDirectory path
              path = OutputUrls.page context.config "tags/index.html"
              filename = "index.html" }

        let renderedView = Renderer.render layout layoutModel
        File.WriteAllText(path, renderedView)

    // Only poole ships a tag_index template (the "list every tag" page), but every theme can
    // still render each tag's own archive via tag_layout, which falls back to the theme's
    // plain layout when it doesn't have one. Gating this on tag_index existing meant every
    // other bundled theme got zero tag pages at all.
    let create (context: BuildContext) (posts: Post list) =

        let tags = posts
                    |> List.map (fun p -> p.frontmatter.tags)
                    |> Array.concat
                    |> Array.distinct
                    |> Array.sort
        createTagsIndexPage context tags
        tags |> Array.iter (fun t -> renderPostsPerTag context t posts)
        
    let setup (paths: SitePaths) =
         
         let tagsDir = SitePaths.inPublic paths "tags"
         if(Directory.Exists(tagsDir)) then
             Directory.Delete(tagsDir, true)
            
         Directory.CreateDirectory(tagsDir) |> ignore

    

