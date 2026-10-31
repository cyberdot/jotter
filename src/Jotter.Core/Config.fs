namespace Jotter.Core

open System.IO
open Newtonsoft.Json

module Config =
    
    type ConfigurationModel = {
      name: string;
      author: string;
      [<JsonProperty("author_description")>]
      authorDescription: string;
      [<JsonProperty("author_location")>]
      authorLocation: string;
      url: string;
      description: string;
      language: string;
      [<JsonProperty("posts_per_page")>]
      postsPerPage: int;
      [<JsonProperty("sort_posts")>]
      sortPosts: string;
      theme: string;
      [<JsonProperty("date_format")>]
      dateFormat: string;
      github: string;
      twitter: string;
      [<JsonProperty("linked_in")>]
      linkedIn: string;
      email: string;
      [<JsonProperty("disqus_url")>]
      disqusUrl: string;
      [<JsonProperty("enable_disqus_comments")>]
      enableDisqusComments: bool;
      [<JsonProperty("google_analytics_id")>]
      googleAnalyticsId: string;
      blogIndex: string;
    }


    let contentDirectory = "./_workspace"
    let publicDirectory = "./_public"
    let configFile = "config.json"
    
    let filePath = $"{contentDirectory}/{configFile}"
    
    let private defaultConfig (theme: string) = $"""
    
    {{ 
      "name": "A brand new static site", 
      "author": "author_name",
      "author_description": "Author's description, skills, etc", 
      "author_location": "Location", 
      "url": "http://localhost:5000", 
      "description": "Your site description", 
      "language": "en-gb", 
      "posts_per_page": 10,
      "sort_posts": "descending",
      "theme": "{theme}",
	  "date_format": "dd MMM yyyy", 
	  "github": "github_account_name", 
	  "twitter": "twitter_account_name", 
	  "linked_in": "linked_in_account_name", 
	  "email": "user_email@email-provider.com", 
	  "disqus_url": "disqus_username.disqus.com",
      "enable_disqus_comments": true,
      "google_analytics_id": ""
    }}
    
    """

    let private loadConfig (content: string) = JsonConvert.DeserializeObject<ConfigurationModel>(content)

    // Index.compileIndex/Tags.compileIndex page through posts by skipping postsPerPage items
    // each round; postsPerPage <= 0 (including the 0 Newtonsoft defaults a missing or mistyped
    // posts_per_page to) never shrinks the remaining list, so the build hangs writing an
    // unbounded stream of index pages instead of failing. Fail fast here instead.
    let private validate (config: ConfigurationModel) =
        if config.postsPerPage <= 0 then
            failwith $"{configFile}: posts_per_page must be a positive number, but was {config.postsPerPage}."
        config

    let init (theme: string) = File.WriteAllText(filePath, (defaultConfig theme))

    let data () = File.ReadAllText(filePath) |> loadConfig |> validate
        
        

