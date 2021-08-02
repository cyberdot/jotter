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
    
    type Post = {
        dateCreated: DateTimeOffset;
        frontmatter: FrontMatter;
        document: string;
        path: string;
        filename: string;
        excerpt: string;
        config: ConfigurationModel;        
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
        let htmlPath = mdFile |> Document.htmlFilename
        let excerpt = htmlContent |> Document.createExcerpt
        
        let (pageLayout, pageRenderer) = Layout.post
        let (layoutTemplate, layoutRenderer) = Layout.post_layout
        
        let vm = {|
                   config = Config.data;
                   content = htmlContent;
                   frontmatter = frontmatter;
                   filename = fileName;
                   path = htmlPath
                 |}
        let result = Renderer.render pageLayout vm pageRenderer
        
        let dvm = {|
                     config = Config.data;
                     js = Assets.js;
                     css = Assets.css;
                     content = result;
                     frontmatter = frontmatter;
                     filename = fileName;
                     path = htmlPath;
                  |}
        let document = Renderer.render layoutTemplate dvm layoutRenderer
        let post: Post = {
          dateCreated = frontmatter.created;
          frontmatter = frontmatter;
          document = document;
          path = htmlPath;
          filename = fileName;
          excerpt = excerpt;
          config = Config.data;
        }
        post
        
    let prepare (mdFile: string) =
        preparePage $"{Config.contentDirectory}/posts/{mdFile}"
        
       
    let create (title: string) (isDraft: bool) =
      let dir = match isDraft with
            | true -> $"{Config.contentDirectory}/drafts"
            | false -> $"{Config.contentDirectory}/posts"
      let path = IOUtils.filenameFromTitle dir title
      let content = defaultPost title
      File.WriteAllText(path, content) |> ignore        
        
    let list =
        let dir = $"{Config.contentDirectory}/posts"
        Directory.GetFiles(dir) |> Seq.sort |> Seq.rev
        
    let init =
        let dir = $"{Config.contentDirectory}/posts"
        Directory.CreateDirectory(dir)
        create "Welcome to Jotter" false