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
    
    let buildItem (config: ConfigurationModel) post =
        let url = OutputUrls.post config post.filename
        let item = SyndicationItem()
        item.Title <- TextSyndicationContent(post.frontmatter.title)
        item.Summary <- TextSyndicationContent(post.frontmatter.description)
        item.PublishDate <- post.frontmatter.created
        item.LastUpdatedTime <- post.frontmatter.created
        item.Links.Add(SyndicationLink.CreateAlternateLink(new Uri(url, UriKind.RelativeOrAbsolute)))
        item
        
    let compileRss (config: ConfigurationModel) (posts: Post list) =
        posts |> Seq.map (buildItem config)

    // Feed readers expect newest-first regardless of how the site's own pagination is
    // configured to sort, and an unbounded feed grows forever as a blog accumulates posts.
    let maxItems = 20

    let selectFeedPosts (posts: Post list) =
        posts
        |> List.sortByDescending (fun p -> p.dateCreated)
        |> List.truncate maxItems

    let buildFeed (config: ConfigurationModel) (posts: Post list) =
        let feed = SyndicationFeed()
        feed.Title <- TextSyndicationContent(config.name)
        feed.Id <- config.url
        feed.Description <- TextSyndicationContent(config.description)
        feed.LastUpdatedTime <- DateTimeOffset.UtcNow
        feed.Language <- config.language
        feed.Links.Add(SyndicationLink.CreateAlternateLink(new Uri(config.url, UriKind.RelativeOrAbsolute)))

        feed.Items <- compileRss config posts

        feed

    // `use`, not a bare `Close()` call after `WriteTo`: if WriteTo throws (e.g. a post's
    // title contains a character XmlWriter rejects), the old code left the file handle open
    // for the rest of the process.
    let writeToFile (paths: SitePaths) (feed: SyndicationFeed) =
        let rssFormatter = Rss20FeedFormatter(feed)
        use rssWriter = XmlWriter.Create(SitePaths.inPublic paths rssFileName)
        rssFormatter.WriteTo(rssWriter)

    let create (context: BuildContext) (posts: Post list) =
        posts
        |> selectFeedPosts
        |> buildFeed context.config
        |> writeToFile context.paths
        
        
        

