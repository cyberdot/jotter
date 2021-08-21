namespace Jotter.Core

open System
open System.IO
open Newtonsoft.Json
open Jotter.Core.Config

module Document =
    
    [<Interface>]
    type ILayoutViewModel =
        abstract   config: ConfigurationModel with get
        abstract js: string with get
        abstract css: string with get
        abstract content: string with get
    
    type FrontMatter = {
        title: string;
        description: string;
        created: DateTimeOffset;
        tags: string array;
        slug: string;
    }
    
    type Page = {
      document: string;
      path: string;
      filename: string;
    }
    
    type Post = {
        dateCreated: DateTimeOffset;
        frontmatter: FrontMatter;
        document: string;
        path: string;
        filename: string;
        excerpt: string;
        config: ConfigurationModel;        
    }   
   
    let writeAllPosts (posts: Post list) =
        posts |> Seq.iter (fun post -> File.WriteAllText(post.path, post.document))
        
    let writeAllPages (pages: Page list) =
        pages |> Seq.iter (fun page -> File.WriteAllText(page.path, page.document))
    
    let fileName (md: string) =
        let filePart = Path.GetFileName(md)
        filePart.Replace(".markdown", ".html")
        
    let htmlFilename (isPost: bool) (md: string) =
        let fileName = fileName(md)
        match isPost with
              | true -> $"{Config.publicDirectory}/posts/{fileName}"
              | false -> $"{Config.publicDirectory}/{fileName}"
        
    let splitIntoParts (content: string) =
        let separators = [|'}'|]
        let parts = content.Split(separators, 2, StringSplitOptions.None)
        let frontMatter = JsonConvert.DeserializeObject<FrontMatter>(parts.[0] + "}")
        (frontMatter, parts.[1])
        
    let createExcerpt (content: string) =
        let length = content.Length
        let maxLength = match length with
                          |  i when i > 1_000 -> 1_000
                          | _ -> length
        let excerpt = content.Substring(0, maxLength)
        $"{excerpt} ..."
        
    

