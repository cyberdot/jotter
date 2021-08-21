namespace Jotter.Core

open System
open System.IO
open System.Xml.Linq
open Jotter.Core.Config

module SiteMap =
    
    let private getHtmlFiles () : string array =
        let rootFiles = Directory.GetFiles(Config.publicDirectory, "*.html") |> Array.map (fun file -> file.Replace(Config.publicDirectory + "/", ""))
        let postsFiles = Directory.GetFiles($"{Config.publicDirectory}/posts", "*.html") |> Array.map (fun file -> file.Replace(Config.publicDirectory + "/", ""))
        let tagsFiles = Directory.GetFiles($"{Config.publicDirectory}/tags", "*.html") |> Array.map (fun file -> file.Replace(Config.publicDirectory + "/", ""))
        let htmlFiles = Array.append rootFiles postsFiles
        let allFiles = Array.append htmlFiles tagsFiles
        allFiles
        
        
    let private generateXmlEntries (entries: string array) =
        
        let ns: XNamespace = XNamespace.Get("http://www.sitemaps.org/schemas/sitemap/0.9")
        let lastModify = DateTime.UtcNow.ToString("yyyy-MM-dd")
        let changeFreq = "monthly"
        let mediumPriority  = "0.8"
        let elements = entries |> Array.map (fun entry -> XElement(ns + "url",
                                                                   XElement(ns + "loc", $"{Config.data().url}/{entry}"),
                                                                   XElement(ns + "lastmod", lastModify),
                                                                   XElement(ns + "changefreq", changeFreq),
                                                                   XElement(ns + "priority", mediumPriority)))
        elements
        
    let private generateXml (entries: string array) =
        
         let domain = Config.data().url
         let lastModify = DateTime.UtcNow.ToString("yyyy-MM-dd")
         let changeFreq = "monthly"
         let topPriority = "0.5"
         let mediumPriority  = "0.8"
         
         let ns: XNamespace = XNamespace.Get("http://www.sitemaps.org/schemas/sitemap/0.9")
         let xsiNs: XNamespace = XNamespace.Get("http://www.w3.org/2001/XMLSchema-instance")
         
         let elements = generateXmlEntries entries
         
         let declaration: XDeclaration = XDeclaration("1.0", "UTF-8", "no")
         
         let rootElement: XElement = XElement(ns + "urlset",
                                                     XAttribute(XNamespace.Xmlns + "xsi", xsiNs),
                                                     XAttribute(xsiNs + "schemaLocation",
                                                                            "http://www.sitemaps.org/schemas/sitemap/0.9/sitemap.xsd"),
                                                     XElement(ns + "url",
 
                                                      XElement(ns + "loc",        domain),
                                                      XElement(ns + "lastmod",    lastModify),
                                                      XElement(ns + "changefreq", "weekly"),
                                                      XElement(ns + "priority",   topPriority)),
 
                                                      elements                                  
 
                )
         let elements: obj array = [|rootElement|]         
         let xDoc = new XDocument(declaration, elements)
         xDoc
         
    let private writeToFile (doc: XDocument) =
        let filePath = $"{Config.publicDirectory}/sitemap.xml"
        doc.Save(filePath) |> ignore
        
    let create () = getHtmlFiles() |> generateXml |> writeToFile