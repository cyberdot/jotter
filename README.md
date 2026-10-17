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
| `publish <title>`     | Move a draft into `_workspace/posts`                        |
| `build`               | Render everything in `_workspace` into `_public`           |
| `server`              | Serve the `_public` folder locally for previewing          |

Typical workflow:

```
dotnet run --project src/Jotter.Console -- init whisper
dotnet run --project src/Jotter.Console -- draft "My first post"
dotnet run --project src/Jotter.Console -- publish "My first post"
dotnet run --project src/Jotter.Console -- build
dotnet run --project src/Jotter.Console -- server
```

## Configuration

`init` generates `_workspace/config.json`, which controls site-wide settings such as `name`, `author`,
`url`, `description`, `language`, `posts_per_page`, `sort_posts`, `theme`, `date_format`, social links
(`github`, `twitter`, `linked_in`, `email`), Disqus (`disqus_url`, `enable_disqus_comments`), and Google
Analytics (`google_analytics_id` - every bundled theme embeds the snippet automatically once this is set
to a tracking ID; leave it blank, the default, to leave Analytics out of the site entirely).

## Testing

```
dotnet test Jotter.slnx
```

## CI/CD

Two GitHub Actions workflows live under `.github/workflows`:

- **Build and Test** (`build.yml`) — builds the solution and runs the full test suite on every branch
  push and on every pull request opened or updated. A merged PR is covered too, since merging pushes
  a commit to the base branch.
- **Release** (`release.yml`) — manually triggered (`workflow_dispatch`). Builds and tests `master`,
  publishes self-contained, versioned Jotter.Console executables for Windows, Linux, and macOS
  (win-x64, linux-x64, osx-x64), tags `master` with that version, and creates a GitHub release with
  all three executables attached and auto-generated release notes. Pass a `version` input (e.g.
  `1.4.0`) to set it explicitly, or leave it empty to auto-increment the patch version from the
  latest `vX.Y.Z` tag.

## License

MIT — see [LICENSE](LICENSE).
