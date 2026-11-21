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
        Path.ChangeExtension(Path.GetFileName(md), ".html")
        
    let htmlFilename (paths: SitePaths) (isPost: bool) (md: string) =
        let fileName = fileName(md)
        match isPost with
              | true -> SitePaths.inPublic paths (Path.Combine("posts", fileName))
              | false -> SitePaths.inPublic paths fileName
        
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

    let trySplitIntoParts (content: string) =
        let endIndex = findFrontMatterEnd content
        if endIndex = -1 then
            // Without this, an unterminated front matter block silently became "no front
            // matter, the whole file is body": frontMatter deserializes to null, and every
            // caller crashes on frontMatter.title/description/etc. with a NullReferenceException
            // that points nowhere near the actual typo.
            let preview = if content.Length > 80 then content.Substring(0, 80) + "..." else content
            Error [ $"Malformed front matter: no closing '}}' found for the JSON block. Starts with: {preview}" ]
        else
            let frontMatterJson = content.Substring(0, endIndex + 1)
            let body = content.Substring(endIndex + 1)
            try
                let frontMatter = JsonConvert.DeserializeObject<FrontMatter>(frontMatterJson)
                if isNull (box frontMatter) then
                    Error [ "Front matter must contain a JSON object" ]
                else
                    let errors =
                        [ if String.IsNullOrWhiteSpace(frontMatter.title) then "front matter title must not be blank"
                          if String.IsNullOrWhiteSpace(frontMatter.description) then "front matter description must not be blank"
                          if frontMatter.created = DateTimeOffset.MinValue then "front matter created must be a valid date"
                          if String.IsNullOrWhiteSpace(frontMatter.slug) then "front matter slug must not be blank"
                          if isNull frontMatter.tags then "front matter tags must be an array" ]

                    match errors with
                    | [] ->
                        let normalized =
                            { frontMatter with
                                tags =
                                    frontMatter.tags
                                    |> Array.map (fun tag -> tag.Trim())
                                    |> Array.filter (String.IsNullOrWhiteSpace >> not)
                                    |> Array.distinct }
                        Ok(normalized, body)
                    | errors -> Error errors
            with :? JsonException as ex ->
                Error [ $"Invalid front matter JSON: {ex.Message}" ]

    let splitIntoParts (content: string) =
        match trySplitIntoParts content with
        | Ok parts -> parts
        | Error errors -> raise (BuildException(String.Join(Environment.NewLine, errors)))
        
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
        
    

