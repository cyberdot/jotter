namespace Jotter.Core

open System
open System.IO
open System.Text.RegularExpressions
open System.Xml.Linq
open Jotter.Core.Config
open Jotter.Core.Document

module SiteMap =

    type Entry = {
        loc: string
        lastModify: string
        changeFreq: string
        priority: string
    }

    let private buildDate = DateTime.UtcNow.ToString("yyyy-MM-dd")

    let homeEntry (config: ConfigurationModel) =
        { loc = config.url; lastModify = buildDate; changeFreq = "weekly"; priority = "1.0" }

    // Posts have a real date to report; everything else falls back to the build date, which
    // is still more honest than claiming every page on the site changed on the same day.
    let postEntry (config: ConfigurationModel) (post: Post) =
        { loc = $"{config.url}/posts/{post.filename}"
          lastModify = post.frontmatter.created.UtcDateTime.ToString("yyyy-MM-dd")
          changeFreq = "monthly"
          priority = "0.8" }

    let pageEntry (config: ConfigurationModel) (fileName: string) =
        { loc = $"{config.url}/{fileName}"
          lastModify = buildDate
          changeFreq = "monthly"
          priority = "0.5" }

    let isIndexFile (fileName: string) =
        fileName = "index.html" || Regex.IsMatch(fileName, @"^index\d+\.html$")

    // Tag and pagination pages are aggregations of the same posts that are already listed
    // individually - they add noise for crawlers rather than content worth indexing.
    let private pageFiles () =
        Directory.GetFiles(Config.publicDirectory, "*.html")
        |> Array.map Path.GetFileName
        |> Array.filter (fun f -> not (isIndexFile f))

    let private toXElement (ns: XNamespace) (entry: Entry) =
        XElement(ns + "url",
                 XElement(ns + "loc", entry.loc),
                 XElement(ns + "lastmod", entry.lastModify),
                 XElement(ns + "changefreq", entry.changeFreq),
                 XElement(ns + "priority", entry.priority)) :> obj

    let private generateXml (posts: Post list) =
        let config = Config.data()
        let ns: XNamespace = XNamespace.Get("http://www.sitemaps.org/schemas/sitemap/0.9")
        let xsiNs: XNamespace = XNamespace.Get("http://www.w3.org/2001/XMLSchema-instance")

        let entries =
            [ homeEntry config ]
            @ (pageFiles() |> Array.toList |> List.map (pageEntry config))
            @ (posts |> List.map (postEntry config))

        let declaration = XDeclaration("1.0", "UTF-8", "no")
        let content =
            Array.append
                [| XAttribute(XNamespace.Xmlns + "xsi", xsiNs) :> obj
                   XAttribute(xsiNs + "schemaLocation", "http://www.sitemaps.org/schemas/sitemap/0.9/sitemap.xsd") :> obj |]
                (entries |> List.map (toXElement ns) |> List.toArray)

        let rootElement = XElement(ns + "urlset", content)
        XDocument(declaration, [| rootElement :> obj |])

    let private writeToFile (doc: XDocument) =
        let filePath = $"{Config.publicDirectory}/sitemap.xml"
        doc.Save(filePath) |> ignore

    let create (posts: Post list) = posts |> generateXml |> writeToFile
