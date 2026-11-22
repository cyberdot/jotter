# Jotter

Jotter is a static blog generator written in F#. It turns Markdown posts and pages into a deployable
HTML site with pagination, tag archives, RSS, a sitemap, theme assets, and an optional local preview
server.

Jotter is available as:

- a self-contained standalone executable for Windows, Linux, and macOS;
- a .NET tool package for developer and CI workflows.

Themes are maintained in the Jotter Git repository, downloaded on demand into a user cache, and
copied into each site for local editing. Builds use the project-local theme and therefore remain
offline after initialization.

## Features

- Markdown posts, pages, and drafts
- JSON front matter
- Generated post and page files from a title
- Draft publishing
- Configurable post sorting and pagination
- Per-tag archives and a tag index
- RSS feed and XML sitemap generation
- Project-local editable Razor themes
- GitHub-backed theme catalog with a local cache
- Theme catalog/list/validate/create/copy/use/update commands
- Google Analytics and Disqus integration
- Static preview server
- Atomic site builds that retain the previous `_public` output if a build fails
- Generated-link validation
- Standalone executable and `.NET` tool distributions

## Installation

### Standalone executable

Download the archive for your platform from GitHub Releases and extract the complete archive.

Windows:

```powershell
.\Jotter.Console.exe --help
```

Linux/macOS:

```bash
./Jotter.Console --help
```

The standalone release is self-contained and does not require the .NET SDK or runtime. You may
rename the executable to `jotter`.

Themes are intentionally not embedded in the standalone archive. On the first `theme list` or
`init`, Jotter downloads the catalog and only the requested theme from `cyberdot/jotter`.

### .NET tool

Install globally:

```bash
dotnet tool install --global Cyberdot.Jotter
jotter --help
```

Or install as a repository-local, version-pinned tool:

```bash
dotnet new tool-manifest
dotnet tool install Cyberdot.Jotter
dotnet tool run jotter --help
```

The shorthand `dotnet jotter` also works in a directory that can resolve the installed local tool.

Restore a committed local tool manifest on another machine with:

```bash
dotnet tool restore
```

The `.NET` tool requires a compatible .NET runtime. The `.nupkg` is also attached to GitHub Releases
for manual publication or installation from a local package source.

## Quick start

Choose a theme:

```bash
jotter theme list
```

Create a site in the current directory:

```bash
jotter init whisper
```

This creates:

```text
_workspace/
  config.json
  posts/
  pages/
  drafts/
  themes/
    whisper/
  robots.txt
  CNAME
```

Edit `_workspace/config.json`, then create content:

```bash
jotter post "Hello World"
jotter page "About"
jotter draft "An unfinished post"
```

Move a draft to published posts:

```bash
jotter publish "An unfinished post"
```

Build the site:

```bash
jotter build
```

Preview it locally:

```bash
jotter server
```

The default preview address is `http://localhost:5000`.

The deployable output is `_public/`.

## Typical authoring workflow

```bash
jotter init poole
jotter draft "Working Notes"

# Edit _workspace/drafts/working-notes.markdown

jotter publish "Working Notes"
jotter build
jotter server
```

Rebuild after editing Markdown, configuration, templates, or assets:

```bash
jotter build
```

## Command reference

Run `jotter --help` for the top-level command list and `jotter <command> --help` for command-specific
help.

### Global options

| Option | Description |
| --- | --- |
| `--help`, `-?`, `-h` | Show help |
| `--version` | Show the installed Jotter version |

### `init`

```bash
jotter init <theme> [--force]
```

Creates `_workspace` and `_public`, installs the selected theme, writes the default configuration,
and creates initial page/post content.

Options:

| Option | Description |
| --- | --- |
| `--force` | Replace an existing workspace and public directory |

`--force` is destructive for existing site content. The selected theme is downloaded and validated
before the existing workspace is removed.

Examples:

```bash
jotter init whisper
jotter init poole --force
```

### `post`

```bash
jotter post <title>
```

Creates `_workspace/posts/<slug>.markdown`. It fails rather than overwriting a file with the same
slug.

```bash
jotter post "Using F# for Static Sites"
```

### `page`

```bash
jotter page <title>
```

Creates `_workspace/pages/<slug>.markdown`.

```bash
jotter page "Contact"
```

### `draft`

```bash
jotter draft <title>
```

Creates `_workspace/drafts/<slug>.markdown`.

```bash
jotter draft "Roadmap"
```

### `publish`

```bash
jotter publish <title>
```

Moves the matching draft into `_workspace/posts`. Publishing supports the `.markdown` and `.md`
extensions when the source file exists.

```bash
jotter publish "Roadmap"
```

### `build`

```bash
jotter build
```

Validates and renders the site into a staging directory. Only a successful, link-validated build
replaces `_public`, so the previous output remains available after a failed build.

Generated output includes:

- root and paginated index pages;
- page and post HTML;
- `tags/index.html`;
- per-tag archive pages;
- copied assets;
- `blog.rss`;
- `sitemap.xml`;
- `robots.txt` and `CNAME`, when initialized.

### `server`

```bash
jotter server [--urls <url>]
```

Serves `_public` with ASP.NET Core static-file hosting.

Options:

| Option | Default | Description |
| --- | --- | --- |
| `--urls <url>` | `http://localhost:5000` | Listener URL |

Examples:

```bash
jotter server
jotter server --urls http://127.0.0.1:8080
```

Run `jotter build` before starting the server.

## Theme commands

### `theme list`

Downloads or reads the cached catalog, then lists available themes:

```bash
jotter theme list
```

Current bundled themes include `architect`, `beautifuld`, `hyde`, `poole`, `pyxill`, `uno`, and
`whisper`.

### `theme copy`

Downloads or reads a theme from cache and copies it into the current site:

```bash
jotter theme copy <theme>
```

Example:

```bash
jotter theme copy architect
```

The command refuses to overwrite an installed theme directory.

### `theme use`

Selects an already installed project-local theme:

```bash
jotter theme use <theme>
```

Example:

```bash
jotter theme copy architect
jotter theme use architect
jotter build
```

### `theme new`

Creates a minimal project-local theme:

```bash
jotter theme new <theme>
```

Example:

```bash
jotter theme new custom
jotter theme use custom
```

The scaffold includes `theme.json`, required Razor layouts, and a stylesheet.

### `theme validate`

```bash
jotter theme validate
jotter theme validate <theme>
```

With no argument, validation checks the catalog, cached/available themes, and all installed
project-local themes. With a theme ID, it validates the local copy when present, otherwise the
repository/cache theme.

Validation includes:

- catalog and manifest JSON;
- duplicate IDs and paths;
- required templates;
- declared CSS/JavaScript assets;
- Jotter compatibility range;
- path traversal and containment;
- project-local rendering inputs.

### `theme update`

```bash
jotter theme update
jotter theme update <theme>
```

Refreshes the remote catalog or the catalog plus one cached theme. Updates modify only the user
cache; they do not replace an existing `_workspace/themes/<theme>` directory or overwrite edits.

## Theme delivery and cache

Released executables do not contain theme files.

Resolution order is:

1. project-local `_workspace/themes/<id>`;
2. a local catalog beside the executable during repository development;
3. the per-user cache;
4. GitHub.

Only the selected theme is downloaded. A normal build never contacts GitHub if the configured
project-local theme is installed and valid.

Environment variables:

| Variable | Purpose |
| --- | --- |
| `JOTTER_THEMES_REPOSITORY` | Override the `owner/repository` source |
| `JOTTER_THEMES_REF` | Override the source ref |
| `JOTTER_CACHE_DIR` | Override the Jotter cache root |
| `GITHUB_TOKEN` | Optional GitHub API authentication |
| `GH_TOKEN` | Alternative GitHub API authentication |
| `XDG_CACHE_HOME` | Linux/Unix cache base when `JOTTER_CACHE_DIR` is not set |

Typical cache roots:

- Windows: `%LOCALAPPDATA%\Jotter\themes`
- macOS: `~/Library/Caches/Jotter/themes`
- Linux: `${XDG_CACHE_HOME:-~/.cache}/Jotter/themes`

See [the theme delivery design](doc/theme-delivery.md) for cache, security, and version policy.

## Content format

Jotter accepts `.markdown` and `.md` content files. Generated content uses `.markdown`.

Every document starts with a JSON object followed by Markdown:

```markdown
{ "title": "Hello World", "description": "My first post", "created": "2024-01-01T12:00:00Z", "tags": ["intro", "fsharp"], "slug": "hello-world" }

# Hello World

This is my first post written in **Markdown**.
```

Front-matter fields:

| Field | Required | Description |
| --- | --- | --- |
| `title` | Yes | Display title |
| `description` | Yes | Summary and feed/metadata description |
| `created` | Yes | ISO-style date accepted as `DateTimeOffset` |
| `tags` | Yes | Array of tag strings; an empty array is valid |
| `slug` | Yes | URL/metadata slug |

The closing `}` may appear inside quoted values. Unterminated or incomplete front matter produces a
validation error before `_public` is replaced.

## Configuration

Initialization creates `_workspace/config.json`:

```json
{
  "name": "A brand new static site",
  "author": "author_name",
  "author_description": "Author's description, skills, etc",
  "author_location": "Location",
  "url": "http://localhost:5000",
  "description": "Your site description",
  "language": "en-gb",
  "posts_per_page": 10,
  "sort_posts": "descending",
  "theme": "whisper",
  "date_format": "dd MMM yyyy",
  "github": "github_account_name",
  "twitter": "twitter_account_name",
  "linked_in": "linked_in_account_name",
  "email": "user_email@email-provider.com",
  "disqus_url": "disqus_username.disqus.com",
  "enable_disqus_comments": true,
  "google_analytics_id": ""
}
```

| Setting | Description |
| --- | --- |
| `name` | Site name |
| `author` | Author name |
| `author_description` | Short author biography |
| `author_location` | Displayed author location |
| `url` | Absolute production/local site URL; normalized without a trailing slash |
| `description` | Site description |
| `language` | RSS language, for example `en-gb` |
| `posts_per_page` | Positive index page size |
| `sort_posts` | `ascending` or `descending` |
| `theme` | Installed theme ID under `_workspace/themes` |
| `date_format` | .NET date format string |
| `github`, `twitter`, `linked_in`, `email` | Theme social/contact values |
| `disqus_url` | Disqus short/site host |
| `enable_disqus_comments` | Render Disqus where a theme supports it |
| `google_analytics_id` | Analytics ID; leave blank to disable |

Use the final deployed origin for `url` before creating a production build:

```json
"url": "https://example.com"
```

## Customizing a theme

Themes installed by `init` or `theme copy` are owned by the site:

```text
_workspace/themes/<theme>/
  theme.json
  layout/
  assets/
```

Modify the local files directly. A cache or catalog refresh does not overwrite these edits.

To customize a repository theme:

```bash
jotter theme copy whisper
jotter theme use whisper
# Edit _workspace/themes/whisper
jotter theme validate whisper
jotter build
```

To scaffold an original theme:

```bash
jotter theme new my-theme
jotter theme use my-theme
```

See [the theme implementation guide](doc/themes.md) and [theme catalog documentation](themes/README.md).

## Building and developing Jotter

Requirements:

- .NET 10 SDK or later

Commands:

```bash
dotnet restore Jotter.slnx
dotnet build Jotter.slnx
dotnet test Jotter.slnx
```

Run from source:

```bash
dotnet run --project src/Jotter.Console -- --help
```

Pack the .NET tool:

```bash
dotnet pack src/Jotter.Console/Jotter.Console.fsproj \
  --configuration Release \
  --output packages
```

Publish a standalone executable:

```bash
dotnet publish src/Jotter.Console/Jotter.Console.fsproj \
  --configuration Release \
  --runtime win-x64 \
  --self-contained true \
  -p:PublishSingleFile=true \
  -p:PublishReadyToRun=true \
  -p:IncludeNativeLibrariesForSelfExtract=true
```

Use `linux-x64` or `osx-x64` as the runtime where appropriate.

## CI and releases

- `build.yml` builds, tests, and packs the tool after a pull request is merged into `master`, or
  when triggered manually.
- `release.yml` is manually triggered.
- A release version can be explicit (`1.4.0`) or omitted to increment the latest `vX.Y.Z` patch.
- Release output includes:
  - Windows, Linux, and macOS self-contained archives;
  - a `Cyberdot.Jotter` `.nupkg`;
  - a Git tag and GitHub Release.
- NuGet publication is manual.

## Troubleshooting

### A theme cannot be downloaded

Check internet access and GitHub availability. If rate-limited, set `GITHUB_TOKEN` or `GH_TOKEN`.
Jotter can use a stale catalog cache when GitHub is temporarily unavailable.

### A site builds offline

This is expected after its selected theme has been copied into `_workspace/themes`. Jotter builds
from project-local theme files and does not need GitHub.

### A build fails after changing a theme

Run:

```bash
jotter theme validate
jotter build
```

Confirm every path in `theme.json` exists and every required layout renders with the expected model.

### A previous `_public` remains after failure

This is intentional. Jotter publishes generated files only after the staged build succeeds.

### A title cannot be created

Titles must produce a non-empty URL slug, and Jotter will not overwrite a document that already has
the same slug.

## License

MIT — see [LICENSE](LICENSE).
