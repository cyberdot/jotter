namespace Jotter.Core

open System
open System
open Newtonsoft.Json.Linq
open Jotter.Core.Config
open System.ServiceModel.Syndication
open Jotter.Core.Post

module Rss =    
    let buildItem post =
        let config = Config.data
        let url = $"{config.url}/{post.fileName}"
        
        let item = SyndicationItem()
        item.Title <- post.frontmatter.title
        item.Description <- post.frontmatter.description
        item.Links.Add(SyndicationLink.CreateAlternateLink(new Uri(url)))
        
    let compileRss (posts: Post list) = posts |> Seq.map (fun p -> buildItem p) 
    let buildFeed posts =
        let config = Config.data
        let feed = SyndicationFeed()
        feed.Title <- TextSyndicationContent(config.name)
        feed.Id <- config.url
        feed.Description <- TextSyndicationContent(config.description)
        feed.LastUpdatedTime <- DateTimeOffset.UtcNow
        feed.Language <- "en-gb"
        

