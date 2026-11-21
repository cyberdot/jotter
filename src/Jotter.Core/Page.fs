namespace Jotter.Core

open System
open System.IO
open Newtonsoft.Json
open Newtonsoft.Json.Linq
open FSharp.Formatting.Markdown
open Jotter.Core.Config
open Jotter.Core.Assets
open Jotter.Core.Layout
open Jotter.Core.Document
open Jotter.Core.IOUtils
open Jotter.Core.Renderer

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
	       "title": {JsonConvert.ToString(title)},
	       "description": "A new page",
	       "created": "{DateTimeOffset.UtcNow.ToString("o")}",
	       "tags": ["page", "new"], 
	       "slug": "{IOUtils.urlSlug(title)}"
	    }}

        Welcome to your brand new page.
        """
        
    // Model.path in the page templates must be a public URL, not a filesystem path.
    let pageUrl (config: ConfigurationModel) (filename: string) =
        OutputUrls.page config filename

    let private layoutViewModel (context: BuildContext) (outputPath: string) (content: string) (frontmatter: FrontMatter) (path: string) (filename: string) =
        {
           config = context.config;
           js = Assets.js context.paths.publicDirectory outputPath;
           css = Assets.css context.paths.publicDirectory outputPath;
           content = content;
           frontmatter = frontmatter;
           path = path;
           filename = filename;
        }

    let private pageViewModel (config: ConfigurationModel) (content: string) (frontmatter: FrontMatter) (filename: string) =
         let parsedContent = Markdown.Parse content
         let htmlContent = Markdown.ToHtml parsedContent
         {
           config = config;
           content = htmlContent;
           frontmatter = frontmatter;
           filename = filename;
         }

    let private preparePage (context: BuildContext) (filepath: string) =
        let layoutTemplate: string = Layout.pageLayout context
        let pageTemplate: string = Layout.page context
        let (frontMatter: FrontMatter, content: string) =
            match File.ReadAllText(filepath) |> Document.trySplitIntoParts with
            | Ok parts -> parts
            | Error errors ->
                raise (BuildException $"{filepath}: {System.String.Join(System.Environment.NewLine, errors)}")

        let config = context.config
        let filename = Document.fileName filepath
        let path = Document.htmlFilename context.paths false filepath
        let url = pageUrl config filename

        let vm = pageViewModel config content frontMatter filename
        let renderedContent = Renderer.render pageTemplate vm

        let lvm = layoutViewModel context path renderedContent frontMatter url filename
        let renderedDocument = Renderer.render layoutTemplate lvm
        
        {
          document = renderedDocument; path = path; filename = filename;
        }
        
    let private pagesDir (paths: SitePaths) =
        SitePaths.inWorkspace paths "pages"
    
    let prepareAll (context: BuildContext) (files: string seq) : Page list =
        files |> Seq.map (preparePage context) |> Seq.toList
    
    let create (paths: SitePaths) (title: string) =
        let path = title |> IOUtils.filenameFromTitle (pagesDir paths)
        let content = defaultPage title
        IOUtils.writeNewFile path content
        
    let list (paths: SitePaths) = IOUtils.discoverMarkdownFiles (pagesDir paths)
        
    let init (paths: SitePaths) =
        Directory.CreateDirectory(pagesDir paths) |> ignore
        create paths "About"
        
        
        

