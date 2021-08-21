namespace Jotter.Core

open System
open System.IO
open FSharp.Formatting.Markdown
open Jotter.Core.Config
open Jotter.Core.IOUtils
open Jotter.Core.Document
open Jotter.Core.Store
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
          "title": "{title}",
          "description": "A new blog post",
          "created": "{DateTimeOffset.UtcNow.ToString("yyyy-MM-dd hh:mm:ssZ")}",
          "tags": ["post", "new"],
          "slug": "{IOUtils.urlSlug(title)}"
        }}
        
        Welcome to your brand new Jotter post.
        """
        
    let private preparePage (mdFile: string) =
        let content = File.ReadAllText(mdFile)
        let (frontmatter, mdContent) = content |> Document.splitIntoParts
        
        let parsedContent = Markdown.Parse mdContent
        let htmlContent = Markdown.ToHtml parsedContent
        
        let fileName = mdFile |> Document.fileName
        let htmlPath = mdFile |> Document.htmlFilename true
        let excerpt = htmlContent |> Document.createExcerpt
        
        let (pageLayout, pageRenderer) = Layout.post()
        let (layoutTemplate, layoutRenderer) = Layout.postLayout()
        
        let vm = {
                   config = Config.data();
                   content = htmlContent;
                   frontmatter = frontmatter;
                   filename = fileName;
                   path = htmlPath
                 }
        let result = Renderer.render pageLayout vm pageRenderer
        
        let dvm = {
                     config = Config.data();
                     js = Assets.js();
                     css = Assets.css();
                     content = result;
                     frontmatter = frontmatter;
                     filename = fileName;
                     path = htmlPath;
                  }
        let document = Renderer.render layoutTemplate dvm layoutRenderer
        {
          dateCreated = frontmatter.created;
          frontmatter = frontmatter;
          document = document;
          path = htmlPath;
          filename = fileName;
          excerpt = excerpt;
          config = Config.data();
        }
        
    let prepare (mdFile: string) (posts: Post list) =
        let post = preparePage mdFile
        Store.addPosts posts [post]
        
       
    let create (title: string) (isDraft: bool) =
      let dir = match isDraft with
                     | true -> $"{Config.contentDirectory}/drafts"
                     | false -> $"{Config.contentDirectory}/posts"
      let path = IOUtils.filenameFromTitle dir title
      let content = defaultPost title
      File.WriteAllText(path, content) |> ignore        
        
    let list () =
        let dir = $"{Config.contentDirectory}/posts"
        Directory.GetFiles(dir) |> Array.sort |> Array.rev
        
    let init () =
        let contentDir = $"{Config.contentDirectory}/posts"
        if (Directory.Exists(contentDir)) then
            Directory.Delete(contentDir, true)
        Directory.CreateDirectory(contentDir) |> ignore
        
        create "Welcome to Jotter" false
        
    let setup () =
         let publicDir = $"{Config.publicDirectory}/posts"
         if(Directory.Exists(publicDir)) then
             Directory.Delete(publicDir, true)
         Directory.CreateDirectory(publicDir) |> ignore