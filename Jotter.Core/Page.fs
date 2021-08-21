namespace Jotter.Core

open System
open System.IO
open Newtonsoft.Json.Linq
open FSharp.Formatting.Markdown
open Jotter.Core.Config
open Jotter.Core.Assets
open Jotter.Core.Layout
open Jotter.Core.Document
open Jotter.Core.Store
open Jotter.Core.IOUtils
open Jotter.Core.Renderer
open Jotter.Core.Store

module Page =

    type PageViewModel = {
       config: ConfigurationModel;
       content: string;
       frontmatter: FrontMatter;
       filename: string;
    }

    type PageLayoutViewModel = {
      config: ConfigurationModel;
      js: string;
      css: string;
      content: string;    
      frontmatter: FrontMatter;
      path: string;
      filename: string;
    } with interface ILayoutViewModel with
                member this.config with get() = this.config
                member this.js with get() = this.js
                member this.css with get() = this.css
                member this.content with get() = this.content
  
    
    let defaultPage (title: string) =
        $"""
        {{ 
	       "title": "{title}", 
	       "description": "A new page", 
	       "created": "{DateTimeOffset.UtcNow.ToString("yyyy-MM-dd hh:mm:ssZ")}", 
	       "tags": ["page", "new"], 
	       "slug": "{IOUtils.urlSlug(title)}"
	    }}

        Welcome to your brand new page.
        """
        
    let private layoutViewModel (content: string) (frontmatter: FrontMatter) (path: string) (filename: string) =
        {
           config = Config.data();
           js = Assets.js();
           css = Assets.css();
           content = content;
           frontmatter = frontmatter;
           path = path;
           filename = filename;
        }
        
    let private pageViewModel (content: string) (frontmatter: FrontMatter) (filename: string) =
         let parsedContent = Markdown.Parse content
         let htmlContent = Markdown.ToHtml parsedContent
         {
           config = Config.data();
           content = htmlContent;
           frontmatter = frontmatter;
           filename = filename;
         }
         
    let private preparePage (filepath: string) =
        let (layoutTemplate: string, layoutRenderer: string) = Layout.pageLayout()
        let (pageTemplate: string, pageRenderer: string) = Layout.page()
        let (frontMatter: FrontMatter, content: string) = File.ReadAllText(filepath) |> Document.splitIntoParts
                       
        let filename = Document.fileName filepath
        let path = Document.htmlFilename false filepath
        
        let vm = pageViewModel content frontMatter filename
        let renderedContent = Renderer.render pageTemplate vm pageRenderer
        
        let lvm = layoutViewModel content frontMatter path filename
        let renderedDocument = Renderer.render layoutTemplate lvm layoutRenderer
        
        {
          document = renderedDocument; path = path; filename = filename;
        }
        
    let private pagesDir = $"{Config.contentDirectory}/pages"
    
    let prepare (mdFile: string) (pages: Page list) =
        let page = preparePage mdFile
        Store.addPages pages [page]
    
    let create (title: string) =
        let path = title |> IOUtils.filenameFromTitle pagesDir
        let content = defaultPage title
        File.WriteAllText(path, content) |> ignore
        
    let list () = Directory.GetFiles(pagesDir)        
        
    let init () =
        Directory.CreateDirectory(pagesDir) |> ignore
        create "About"
        
        
        

