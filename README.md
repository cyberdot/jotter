# Jotter

Jotter is a static site generator written in F# (originally ported from an Elixir project). It turns a
folder of Markdown posts and pages into a fully static blog: paginated index pages, per-tag archives, an
RSS feed and a sitemap, rendered through a swappable HTML theme.

## How it works

Jotter keeps two directories at the root of your project:

- `_workspace` — your source content: `posts/`, `pages/`, `drafts/`, a copy of the chosen `themes/<name>`,
  and `config.json`.
- `_public` — the generated, deployable static site (HTML, assets, `blog.rss`, `sitemap.xml`).

Each Markdown file starts with a JSON "front matter" block followed by the Markdown body, e.g.:

```
{ "title": "Hello World", "description": "My first post", "created": "2024-01-01 12:00:00Z", "tags": ["intro"], "slug": "hello-world" }

# Hello World

This is my first post.
```

Front matter is parsed, the body is converted from Markdown to HTML, and both are fed into the theme's
Razor (`.cshtml`) or [RazorLight](https://github.com/toddams/RazorLight) templates to produce the final
page.

## Features

- Markdown content for posts and pages, with JSON front matter (title, description, tags, slug, created date)
- Pluggable themes — several bundled themes are included (`architect`, `beautifuld`, `hyde`, `poole`,
  `pyxill`, `uno`, `whisper`), selected at `init` time
- Paginated post index, configurable via `posts_per_page` and `sort_posts` (ascending/descending)
- Per-tag archive pages and a tags index
- RSS feed (`blog.rss`) and XML `sitemap.xml` generation
- Optional Google Analytics snippet and Disqus comments integration
- Built-in local web server (Giraffe/Kestrel) for previewing the generated `_public` folder
- Drafts workflow, separate from published posts

## Project layout

| Project                 | Purpose                                                                 |
|--------------------------|--------------------------------------------------------------------------|
| `src/Jotter.Core`         | Core library: content/config model, Markdown & template rendering, index/tag/RSS/sitemap generation |
| `src/Jotter.Console`      | Command-line front end (built on `System.CommandLine`) plus the bundled themes |
| `src/Jotter.Core.Tests`   | Unit tests for `Jotter.Core` (xUnit v3 + [Unquote](https://github.com/SwensenSoftware/unquote) assertions) |

## Requirements

- [.NET 10 SDK](https://dotnet.microsoft.com/) or later

## Building

```
dotnet build Jotter.slnx
```

## Usage

Run the console app from the directory where you want your site to live:

```
dotnet run --project src/Jotter.Console -- <command> [args]
```

Available commands:

| Command             | Description                                              |
|----------------------|------------------------------------------------------------|
| `init <theme>`        | Scaffold a new site in `_workspace` using the given theme |
| `page <title>`        | Create a new page under `_workspace/pages`                |
| `post <title>`        | Create a new published post under `_workspace/posts`      |
| `draft <title>`       | Create a new draft under `_workspace/drafts`               |
| `build`               | Render everything in `_workspace` into `_public`           |
| `server`              | Serve the `_public` folder locally for previewing          |

Typical workflow:

```
dotnet run --project src/Jotter.Console -- init whisper
dotnet run --project src/Jotter.Console -- post "My first post"
dotnet run --project src/Jotter.Console -- build
dotnet run --project src/Jotter.Console -- server
```

## Configuration

`init` generates `_workspace/config.json`, which controls site-wide settings such as `name`, `author`,
`url`, `description`, `language`, `posts_per_page`, `sort_posts`, `theme`, `date_format`, social links
(`github`, `twitter`, `linked_in`, `email`), and Disqus (`disqus_url`, `enable_disqus_comments`).

## Testing

```
dotnet test Jotter.slnx
```

## License

MIT — see [LICENSE](LICENSE).
