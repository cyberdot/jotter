namespace Jotter.Core

open Newtonsoft.Json
open Newtonsoft.Json.Linq
open Jotter.Core.Config
open Jotter.Core.Document

module StructuredData =
    let private script (data: JObject) =
        data.ToString(Formatting.None)
        |> IOUtils.protectInlineJson
        |> sprintf """<script type="application/ld+json">%s</script>"""

    let website (config: ConfigurationModel) (url: string) =
        let data =
            JObject(
                JProperty("@context", "https://schema.org"),
                JProperty("@type", "WebSite"),
                JProperty("name", config.name),
                JProperty("publisher", config.name),
                JProperty("url", url),
                JProperty("description", config.description))
        script data

    let article (config: ConfigurationModel) (frontMatter: FrontMatter) (url: string) =
        let author =
            JObject(
                JProperty("@type", "Person"),
                JProperty("name", config.author),
                JProperty("description", config.authorDescription))
        let keywords = JArray(frontMatter.tags)
        let data =
            JObject(
                JProperty("@context", "https://schema.org"),
                JProperty("@type", "Article"),
                JProperty("author", author),
                JProperty("headline", frontMatter.title),
                JProperty("publisher", config.name),
                JProperty("url", url),
                JProperty("datePublished", frontMatter.created),
                JProperty("dateModified", frontMatter.created),
                JProperty("keywords", keywords),
                JProperty("description", frontMatter.description))
        script data

