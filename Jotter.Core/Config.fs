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
      blogIndex: string;
    }


    let contentDirectory = "./_workspace"
    let publicDirectory = "./_public"
    let configFile = "config.json"
    
    let filePath = $"{contentDirectory}/{configFile}"
    
    let private defaultConfig = """
    
    { 
      "name": "A brand new static site", 
      "author": "author_name",
      "author_description": "Author's description, skills, etc", 
      "author_location": "Location", 
      "url": "http://localhost:4000", 
      "description": "Your site description", 
      "language": "en-gb", 
      "posts_per_page": 10, 
      "sort_posts": "ascending", 
      "theme": "poole", 
	  "date_format": "dd MMM yyyy", 
	  "github": "github_account_name", 
	  "twitter": "twitter_account_name", 
	  "linked_in": "linked_in_account_name", 
	  "email": "user_email@email-provider.com", 
	  "disqus_url": "disqus_username.disqus.com", 
      "enable_disqus_comments": true 
    }
    
    """

    let private loadConfig (content: string) = JsonConvert.DeserializeObject<ConfigurationModel>(content)

    let init () = File.WriteAllText(filePath, defaultConfig)

    let data () = File.ReadAllText(filePath) |> loadConfig      
        
        

