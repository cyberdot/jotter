namespace Jotter.Core

open System.IO
open System.Xml.Linq
open Jotter.Core.Config

module SiteMap =
    
    let private getHtmlFiles =
        let rootFiles = Directory.GetFiles(Config.publicDirectory, ".html")
        let tagsFiles = Directory.GetFiles($"{Config.publicDirectory}/tags", ".html")
        rootFiles @ tagsFiles
        
    let private generateXml (entries: string[]) =
        let ns = "http://www.sitemaps.org/schemas/sitemap/0.9"
        let xsiNs = "http://www.w3.org/2001/XMLSchema-instance"
        let schemaLocation = "http://www.sitemaps.org/schemas/sitemap/0.9 http://www.sitemaps.org/schemas/sitemap/0.9/sitemap.xsd"
        let doc = XDocument(
                               XDeclaration("1.0", "UTF-8", "no"),
                               XElement(ns + "urlset",
                                        XAttribute(XNamespace.Xmlns + "xsi", xsiNs),
                                        XAttribute(xsiNs + "schemaLocation", schemaLocation),
                                        
                                        for entry in entries do
                                            XElement(ns + "url",
                                                 new XElement(ns + "loc", entry))
                                        )
                           )
        doc.ToString()
        
    let private writeToFile (content: string) =
        let filePath = $"{Config.publicDirectory}/sitemap.xml"
        File.WriteAllText(filePath, content)
        
    
    let create = getHtmlFiles |> generateXml |> writeToFile

