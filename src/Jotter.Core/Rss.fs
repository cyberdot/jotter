namespace Jotter.Core

open System
open System.Xml
open Newtonsoft.Json.Linq
open Jotter.Core.Config
open System.ServiceModel.Syndication
open Jotter.Core.Document
open Jotter.Core.Posts

module Rss =
    
    let rssFileName = "blog.rss"
    
    let buildItem post =
        let config = Config.data()
        let url = $"{config.url}/posts/{post.filename}"
        
        let item = SyndicationItem()
        item.Title <- TextSyndicationContent(post.frontmatter.title)
        item.Summary <- TextSyndicationContent(post.frontmatter.description)
        item.PublishDate <- post.frontmatter.created
        item.LastUpdatedTime <- post.frontmatter.created
        item.Links.Add(SyndicationLink.CreateAlternateLink(new Uri(url, UriKind.RelativeOrAbsolute)))
        item
        
    let compileRss (posts: Post list) = posts |> Seq.map (fun p -> buildItem p)

    // Feed readers expect newest-first regardless of how the site's own pagination is
    // configured to sort, and an unbounded feed grows forever as a blog accumulates posts.
    let private maxItems = 20

    let buildFeed (posts: Post list) =
        let config = Config.data()

        let feed = SyndicationFeed()
        feed.Title <- TextSyndicationContent(config.name)
        feed.Id <- config.url
        feed.Description <- TextSyndicationContent(config.description)
        feed.LastUpdatedTime <- DateTimeOffset.UtcNow
        feed.Language <- config.language
        feed.Links.Add(SyndicationLink.CreateAlternateLink(new Uri(config.url, UriKind.RelativeOrAbsolute)))

        feed.Items <- compileRss posts

        feed

    let writeToFile (feed: SyndicationFeed) =
        let rssFormatter = Rss20FeedFormatter(feed)
        let rssWriter = XmlWriter.Create($"{Config.publicDirectory}/{rssFileName}")
        rssFormatter.WriteTo(rssWriter)
        rssWriter.Close();

    let create (posts: Post list) =
        posts
        |> List.sortByDescending (fun p -> p.dateCreated)
        |> List.truncate maxItems
        |> buildFeed
        |> writeToFile
        
        
        

