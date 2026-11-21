namespace Jotter.Core

open System
open System.IO
open Newtonsoft.Json
open FSharp.Formatting.Markdown
open Jotter.Core.Config
open Jotter.Core.IOUtils
open Jotter.Core.Document
open Jotter.Core.Layout
open Jotter.Core.Assets

module Post =
    
   
    type PostLayoutViewModel = {
       config: ConfigurationModel;
       content: string;
       frontmatter: FrontMatter;
       filename: string;
       path: string;
    } with interface ILayoutViewModel with
             member this.config with get() = this.config
             member this.css with get() = String.Empty
             member this.js with get() = String.Empty
             member this.content with get() = this.content

    type PostViewModel = {
       config: ConfigurationModel;
       js: string;
       css: string;
       content: string;
       frontmatter: FrontMatter;
       filename: string;
       path: string;
    }
    
    let private defaultPost (title: string) =
        $"""
        {{
          "title": {JsonConvert.ToString(title)},
          "description": "A new blog post",
          "created": "{DateTimeOffset.UtcNow.ToString("o")}",
          "tags": ["post", "new"],
          "slug": "{IOUtils.urlSlug(title)}"
        }}
        
        Welcome to your brand new Jotter post.
        """
        
    // Model.path in the post templates must be a public URL, not a filesystem path, or
    // Disqus (and any theme's canonical link tag) keys off the wrong value.
    let postUrl (config: ConfigurationModel) (fileName: string) =
        OutputUrls.post config fileName

    let private preparePage (context: BuildContext) (mdFile: string) =
        let content = File.ReadAllText(mdFile)
        let (frontmatter, mdContent) =
            match Document.trySplitIntoParts content with
            | Ok parts -> parts
            | Error errors ->
                raise (BuildException $"{mdFile}: {System.String.Join(System.Environment.NewLine, errors)}")

        let parsedContent = Markdown.Parse mdContent
        let htmlContent = Markdown.ToHtml parsedContent

        let config = context.config
        let fileName = mdFile |> Document.fileName
        let htmlPath = Document.htmlFilename context.paths true mdFile
        let url = postUrl config fileName
        let excerpt = htmlContent |> Document.createExcerpt

        let pageLayout = Layout.post context
        let layoutTemplate = Layout.postLayout context

        let vm = {
                   config = config;
                   content = htmlContent;
                   frontmatter = frontmatter;
                   filename = fileName;
                   path = url
                 }
        let result = Renderer.render pageLayout vm

        let dvm = {
                     config = config;
                     js = Assets.js context.paths.publicDirectory htmlPath;
                     css = Assets.css context.paths.publicDirectory htmlPath;
                     content = result;
                     frontmatter = frontmatter;
                     filename = fileName;
                     path = url;
                  }
        let document = Renderer.render layoutTemplate dvm
        {
          dateCreated = frontmatter.created;
          frontmatter = frontmatter;
          document = document;
          path = htmlPath;
          filename = fileName;
          excerpt = excerpt;
          config = config;
        }
        
    let prepareAll (context: BuildContext) (files: string seq) : Post list =
        files |> Seq.map (preparePage context) |> Seq.toList
        
       
    let create (paths: SitePaths) (title: string) (isDraft: bool) =
      let dir = match isDraft with
                     | true -> SitePaths.inWorkspace paths "drafts"
                     | false -> SitePaths.inWorkspace paths "posts"
      let path = IOUtils.filenameFromTitle dir title
      let content = defaultPost title
      IOUtils.writeNewFile path content
        
    let list (paths: SitePaths) =
        let dir = SitePaths.inWorkspace paths "posts"
        IOUtils.discoverMarkdownFiles dir |> Array.rev
        
    let init (paths: SitePaths) =
        let contentDir = SitePaths.inWorkspace paths "posts"
        if (Directory.Exists(contentDir)) then
            Directory.Delete(contentDir, true)
        Directory.CreateDirectory(contentDir) |> ignore
        
        create paths "Welcome to Jotter" false
        
    let setup (paths: SitePaths) =
         let publicDir = SitePaths.inPublic paths "posts"
         if(Directory.Exists(publicDir)) then
             Directory.Delete(publicDir, true)
         Directory.CreateDirectory(publicDir) |> ignore