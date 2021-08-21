namespace Jotter.Core

open System
open System.Xml
open Newtonsoft.Json.Linq
open Jotter.Core.Config
open System.ServiceModel.Syndication
open Jotter.Core.Document

module Rss =
    
    let rssFileName = "blog.rss"
    
    let buildItem post =
        let config = Config.data()
        let url = $"{config.url}/posts/{post.filename}"
        
        let item = SyndicationItem()
        item.Title <- TextSyndicationContent(post.frontmatter.title)
        item.Summary <- TextSyndicationContent(post.frontmatter.description)
        item.Links.Add(SyndicationLink.CreateAlternateLink(new Uri(url)))
        item
        
    let compileRss (posts: Post list) = posts |> Seq.map (fun p -> buildItem p)
    
    let buildFeed posts =
        let config = Config.data()
        
        let feed = SyndicationFeed()
        feed.Title <- TextSyndicationContent(config.name)
        feed.Id <- config.url
        feed.Description <- TextSyndicationContent(config.description)
        feed.LastUpdatedTime <- DateTimeOffset.UtcNow
        feed.Language <- "en-gb"
        
        feed.Items <- compileRss posts
        
        feed
        
    let writeToFile (feed: SyndicationFeed) =
        let rssFormatter = Rss20FeedFormatter(feed)
        let rssWriter = XmlWriter.Create($"{Config.publicDirectory}/{rssFileName}")
        rssFormatter.WriteTo(rssWriter)
        rssWriter.Close();
        
    let create posts = posts |> buildFeed |> writeToFile
        
        
        

