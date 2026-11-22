namespace Jotter.Core

open System
open System.IO
open Newtonsoft.Json

module Config =

    type PostSort =
        | Ascending
        | Descending

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
      sortPosts: PostSort;
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

    [<CLIMutable>]
    type private ConfigurationDocument = {
      [<JsonProperty("name")>] name: string;
      [<JsonProperty("author")>] author: string;
      [<JsonProperty("author_description")>] authorDescription: string;
      [<JsonProperty("author_location")>] authorLocation: string;
      [<JsonProperty("url")>] url: string;
      [<JsonProperty("description")>] description: string;
      [<JsonProperty("language")>] language: string;
      [<JsonProperty("posts_per_page")>] postsPerPage: int;
      [<JsonProperty("sort_posts")>] sortPosts: string;
      [<JsonProperty("theme")>] theme: string;
      [<JsonProperty("date_format")>] dateFormat: string;
      [<JsonProperty("github")>] github: string;
      [<JsonProperty("twitter")>] twitter: string;
      [<JsonProperty("linked_in")>] linkedIn: string;
      [<JsonProperty("email")>] email: string;
      [<JsonProperty("disqus_url")>] disqusUrl: string;
      [<JsonProperty("enable_disqus_comments")>] enableDisqusComments: bool;
      [<JsonProperty("google_analytics_id")>] googleAnalyticsId: string;
      [<JsonProperty("blog_index")>] blogIndex: string;
    }

    let configFile = "config.json"

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

    let private text (name: string) (value: string) errors =
        if String.IsNullOrWhiteSpace(value) then
            $"{name} must not be blank" :: errors
        else
            errors

    let private toSort (value: string) =
        match value.ToLowerInvariant() with
        | "ascending" -> Ok Ascending
        | "descending" -> Ok Descending
        | _ -> Error $"sort_posts must be 'ascending' or 'descending', but was '{value}'"

    let private toConfiguration (document: ConfigurationDocument) =
        if isNull (box document) then
            Error [ $"{configFile} must contain a JSON object" ]
        else
            let mutable errors: string list = []
            errors <- text "name" document.name errors
            errors <- text "author" document.author errors
            errors <- text "url" document.url errors
            errors <- text "description" document.description errors
            errors <- text "language" document.language errors
            errors <- text "theme" document.theme errors
            errors <- text "date_format" document.dateFormat errors
            errors <-
                if document.postsPerPage <= 0 then
                    $"posts_per_page must be positive, but was {document.postsPerPage}" :: errors
                else errors

            let parsedSort =
                if String.IsNullOrWhiteSpace(document.sortPosts) then
                    Error "sort_posts must be 'ascending' or 'descending'"
                else
                    toSort document.sortPosts
            let sort =
                match parsedSort with
                | Ok value -> Some value
                | Error message ->
                    errors <- message :: errors
                    None

            let parsedUrl, uri =
                if String.IsNullOrWhiteSpace(document.url) then
                    false, Unchecked.defaultof<Uri>
                else
                    Uri.TryCreate(document.url, UriKind.Absolute)
            if not parsedUrl && not (String.IsNullOrWhiteSpace(document.url)) then
                errors <- $"url must be an absolute URI, but was '{document.url}'" :: errors

            if not (String.IsNullOrWhiteSpace(document.dateFormat)) then
                try
                    DateTimeOffset.UtcNow.ToString(document.dateFormat) |> ignore
                with :? FormatException ->
                    errors <- $"date_format is invalid: '{document.dateFormat}'" :: errors

            match errors, sort with
            | [], Some sort ->
                let config: ConfigurationModel =
                    { name = document.name
                      author = document.author
                      authorDescription = document.authorDescription
                      authorLocation = document.authorLocation
                      url = uri.ToString().TrimEnd('/')
                      description = document.description
                      language = document.language
                      postsPerPage = document.postsPerPage
                      sortPosts = sort
                      theme = document.theme
                      dateFormat = document.dateFormat
                      github = document.github
                      twitter = document.twitter
                      linkedIn = document.linkedIn
                      email = document.email
                      disqusUrl = document.disqusUrl
                      enableDisqusComments = document.enableDisqusComments
                      googleAnalyticsId = document.googleAnalyticsId
                      blogIndex =
                        if String.IsNullOrWhiteSpace(document.blogIndex) then "index.html"
                        else document.blogIndex }
                Ok config
            | errors, _ -> Error (List.rev errors)

    let tryParse (content: string) =
        try
            JsonConvert.DeserializeObject<ConfigurationDocument>(content) |> toConfiguration
        with :? JsonException as ex ->
            Error [ $"Invalid JSON in {configFile}: {ex.Message}" ]

    let init (paths: SitePaths) (theme: string) =
        Directory.CreateDirectory(paths.workspace) |> ignore
        File.WriteAllText(paths.configFile, defaultConfig theme)

    let tryLoad (paths: SitePaths) =
        if not (File.Exists(paths.configFile)) then
            Error [ $"Configuration file not found: {paths.configFile}. Run 'init' first." ]
        else
            File.ReadAllText(paths.configFile) |> tryParse

    let load (paths: SitePaths) =
        match tryLoad paths with
        | Ok config -> config
        | Error errors -> raise (BuildException(String.Join(Environment.NewLine, errors)))
        
        

