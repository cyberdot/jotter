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

    let private buildDate (context: BuildContext) =
        context.builtAt.UtcDateTime.ToString("yyyy-MM-dd")

    let homeEntry (context: BuildContext) =
        { loc = OutputUrls.home context.config
          lastModify = buildDate context
          changeFreq = "weekly"
          priority = "1.0" }

    // Posts have a real date to report; everything else falls back to the build date, which
    // is still more honest than claiming every page on the site changed on the same day.
    let postEntry (config: ConfigurationModel) (post: Post) =
        { loc = OutputUrls.post config post.filename
          lastModify = post.frontmatter.created.UtcDateTime.ToString("yyyy-MM-dd")
          changeFreq = "monthly"
          priority = "0.8" }

    let pageEntry (context: BuildContext) (fileName: string) =
        { loc = OutputUrls.page context.config fileName
          lastModify = buildDate context
          changeFreq = "monthly"
          priority = "0.5" }

    let isIndexFile (fileName: string) =
        fileName = "index.html" || Regex.IsMatch(fileName, @"^index\d+\.html$")

    // Tag and pagination pages are aggregations of the same posts that are already listed
    // individually - they add noise for crawlers rather than content worth indexing.
    let private pageFiles (paths: SitePaths) =
        Directory.GetFiles(paths.publicDirectory, "*.html")
        |> Array.map Path.GetFileName
        |> Array.filter (fun f -> not (isIndexFile f))

    let private toXElement (ns: XNamespace) (entry: Entry) =
        XElement(ns + "url",
                 XElement(ns + "loc", entry.loc),
                 XElement(ns + "lastmod", entry.lastModify),
                 XElement(ns + "changefreq", entry.changeFreq),
                 XElement(ns + "priority", entry.priority)) :> obj

    let private generateXml (context: BuildContext) (posts: Post list) =
        let ns: XNamespace = XNamespace.Get("http://www.sitemaps.org/schemas/sitemap/0.9")
        let xsiNs: XNamespace = XNamespace.Get("http://www.w3.org/2001/XMLSchema-instance")

        let entries =
            [ homeEntry context ]
            @ (pageFiles context.paths |> Array.toList |> List.map (pageEntry context))
            @ (posts |> List.map (postEntry context.config))

        let declaration = XDeclaration("1.0", "UTF-8", "no")
        let content =
            Array.append
                [| XAttribute(XNamespace.Xmlns + "xsi", xsiNs) :> obj
                   XAttribute(xsiNs + "schemaLocation", "http://www.sitemaps.org/schemas/sitemap/0.9/sitemap.xsd") :> obj |]
                (entries |> List.map (toXElement ns) |> List.toArray)

        let rootElement = XElement(ns + "urlset", content)
        XDocument(declaration, [| rootElement :> obj |])

    let private writeToFile (paths: SitePaths) (doc: XDocument) =
        let filePath = SitePaths.inPublic paths "sitemap.xml"
        doc.Save(filePath) |> ignore

    let create (context: BuildContext) (posts: Post list) =
        posts |> generateXml context |> writeToFile context.paths
