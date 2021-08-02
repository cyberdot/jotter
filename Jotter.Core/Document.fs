namespace Jotter.Core

open System
open System.IO
open Newtonsoft.Json
open Jotter.Core.Config

module Document =
    
    type Page = {
        path: string;
        document: string
    }
    
    type FrontMatter = {
        title: string;
        description: string;
        created: DateTimeOffset;
        tags: string array;
        slug: string;
    }
    
    let writeAll pages = pages |> Seq.iter (fun page -> File.WriteAllText(page.path, page.document))
    
    let fileName (md: string) =
        let filePart = Path.GetFileName(md);
        filePart.Replace(".markdown", ".html")
        
    let htmlFilename (md: string) =
        let fileName = fileName(md)
        $"{Config.publicDirectory}/{fileName}"
        
    let splitIntoParts (pageContent: string) =
        let separators = [|'}'|]
        let parts = pageContent.Split(separators, 2, StringSplitOptions.None)
        let frontMatter = JsonConvert.DeserializeObject<FrontMatter>(parts.[0] + "}")
        (frontMatter, parts.[1])
        
    let createExcerpt (content: string) =
        let excerpt = content.Substring(0, 1000)
        $"{excerpt} ..."
        
    

