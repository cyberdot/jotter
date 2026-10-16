namespace Jotter.Core

open System
open System.IO
open System.Net
open System.Text.RegularExpressions
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
        
    // Splitting on the first `}` corrupts front matter whose title or description contains one,
    // e.g. `{"title": "Oops } Title", ...}`. Track brace depth and skip braces inside quoted
    // strings so we find the `}` that actually closes the top-level JSON object.
    let private findFrontMatterEnd (content: string) =
        let mutable depth = 0
        let mutable inString = false
        let mutable escaped = false
        let mutable endIndex = -1
        let mutable i = 0
        while endIndex = -1 && i < content.Length do
            let c = content.[i]
            if inString then
                if escaped then escaped <- false
                elif c = '\\' then escaped <- true
                elif c = '"' then inString <- false
            else
                match c with
                | '"' -> inString <- true
                | '{' -> depth <- depth + 1
                | '}' ->
                    depth <- depth - 1
                    if depth = 0 then endIndex <- i
                | _ -> ()
            i <- i + 1
        endIndex

    let splitIntoParts (content: string) =
        let endIndex = findFrontMatterEnd content
        let frontMatterJson = content.Substring(0, endIndex + 1)
        let body = content.Substring(endIndex + 1)
        let frontMatter = JsonConvert.DeserializeObject<FrontMatter>(frontMatterJson)
        (frontMatter, body)
        
    // Truncating rendered HTML by raw character count can slice through a tag or attribute
    // (e.g. `<a href="...`), corrupting the excerpt's markup. Stripping tags first and decoding
    // entities gives a plain-text excerpt that is safe to truncate anywhere and renders correctly
    // whether or not the theme wraps it in @Raw.
    let createExcerpt (content: string) =
        let plainText = content |> fun c -> Regex.Replace(c, "<[^>]*>", String.Empty) |> WebUtility.HtmlDecode

        let excerpt =
            if plainText.Length <= 1_000 then
                plainText
            else
                let truncated = plainText.Substring(0, 1_000)
                let lastSpace = truncated.LastIndexOf(' ')
                if lastSpace > 0 then truncated.Substring(0, lastSpace) else truncated

        $"{excerpt.Trim()} ..."
        
    

