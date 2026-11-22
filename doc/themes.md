# Theme Management Design

> **Implementation status:** The repository catalog, per-theme manifests, typed resolution,
> selected-theme initialization, manifest-controlled assets, and `theme list/validate/copy/new/use`
> commands are implemented.

## Goal

Make bundled and user-created themes easy to:

- discover;
- select during `init`;
- inspect and modify;
- copy into a project for customization;
- validate automatically;
- add to the Jotter repository without hard-coded theme lists;
- maintain independently without introducing a package system.

This design keeps themes in Git and does not require ZIP packages, a registry, or network
resolution.

## Proposed repository layout

Move themes to a dedicated repository-level folder rather than treating them as console-app
implementation content:

```text
jotter/
  themes/
    themes.json
    architect/
      theme.json
      layouts/
      assets/
      preview.png
      README.md
    beautifuld/
      theme.json
      layouts/
      assets/
      preview.png
      README.md
    whisper/
      theme.json
      layouts/
      assets/
      preview.png
      README.md
  src/
    Jotter.Console/
    Jotter.Core/
    Jotter.Core.Tests/
```

The repository-root catalog provides discovery. Each theme remains a normal Git-tracked folder
that can be added, reviewed, copied, or modified independently.

The existing `layout/` folder name can be retained during the first migration to reduce churn.
Using `layouts/` is recommended for new themes because it more clearly describes the folder's
contents.

## Catalog: `themes/themes.json`

`themes/themes.json` is the authoritative list of bundled themes.

```json
{
  "schemaVersion": 1,
  "themes": [
    {
      "id": "architect",
      "name": "Architect",
      "path": "architect",
      "description": "GitHub Pages Architect-inspired theme"
    },
    {
      "id": "beautifuld",
      "name": "Beautiful D",
      "path": "beautifuld",
      "description": "A clean, image-led blog theme"
    },
    {
      "id": "whisper",
      "name": "Whisper",
      "path": "whisper",
      "description": "A minimal Ghost-inspired blog theme"
    }
  ]
}
```

Rules:

- `id` is the stable CLI/configuration identifier.
- `path` is relative to the catalog directory.
- `id` and `path` must be unique.
- Paths cannot be absolute, contain `..`, or escape the `themes/` directory.
- Every catalog entry must point to an existing directory.
- Every theme directory should appear in the catalog.

The catalog only needs fields used by theme discovery and selection. Per-theme behavior should
live in `theme.json`.

## Per-theme manifest: `themes/<id>/theme.json`

The per-theme manifest makes a theme self-describing and avoids implicit assumptions spread across
the renderer.

```json
{
  "schemaVersion": 1,
  "id": "whisper",
  "name": "Whisper",
  "version": "1.0.0",
  "jotter": ">=1.0.0 <2.0.0",
  "author": "Jotter contributors",
  "license": "MIT",
  "templates": {
    "base": "layouts/layout.cshtml",
    "index": "layouts/index.cshtml",
    "post": "layouts/post.cshtml",
    "page": "layouts/page.cshtml",
    "postLayout": "layouts/post_layout.cshtml",
    "tagLayout": "layouts/tag_layout.cshtml",
    "tagIndex": null
  },
  "assets": {
    "styles": [
      "assets/css/screen.css"
    ],
    "scripts": [
      "assets/js/index.js",
      "assets/js/jquery.fitvids.js"
    ]
  },
  "preview": "preview.png",
  "features": [
    "posts",
    "pages",
    "tags",
    "pagination"
  ]
}
```

### Manifest conventions

- `id` must match the catalog ID.
- `version` uses semantic versioning.
- `jotter` declares the compatible Jotter version range.
- Template paths are explicit and relative to the theme root.
- A `null` optional template means the renderer should use the documented fallback.
- Styles and scripts are ordered intentionally by the manifest.
- Every declared file must exist.
- The manifest should not expose absolute paths.

The existing optional-template fallback remains compatible:

```text
postLayout -> base
pageLayout -> base
indexLayout -> base
tagLayout -> base
```

However, `post`, `page`, and `index` should be mandatory because they define the central site
content views.

## Runtime selection

`init <theme>` should:

1. load and validate `themes/themes.json`;
2. locate the requested `id`;
3. load and validate the selected theme's `theme.json`;
4. copy only that theme into the initialized site;
5. store the theme identity in `_workspace/config.json`.

Recommended initialized layout:

```text
_workspace/
  config.json
  themes/
    whisper/
      theme.json
      layouts/
      assets/
  posts/
  pages/
  drafts/
```

This avoids copying all seven bundled themes into every site. It also gives the user a complete,
editable theme for that project.

`config.json` may initially continue using:

```json
{
  "theme": "whisper"
}
```

Later, it can accept a structured form without breaking existing sites:

```json
{
  "theme": {
    "id": "whisper",
    "version": "1.0.0"
  }
}
```

## Theme model in F#

Introduce a small typed theme domain:

```fsharp
type ThemeTemplateSet =
    { baseTemplate: string
      indexTemplate: string
      postTemplate: string
      pageTemplate: string
      postLayout: string option
      indexLayout: string option
      pageLayout: string option
      tagLayout: string option
      tagIndex: string option }

type ThemeAssetSet =
    { styles: string list
      scripts: string list }

type ThemeManifest =
    { schemaVersion: int
      id: string
      name: string
      version: string
      jotter: string
      templates: ThemeTemplateSet
      assets: ThemeAssetSet
      features: string list
      preview: string option }

type ThemeCatalogEntry =
    { id: string
      name: string
      path: string
      description: string }

type ThemeSource =
    { root: string
      catalogEntry: ThemeCatalogEntry
      manifest: ThemeManifest }
```

The names can be adjusted to follow the repository's F# conventions. The important boundary is
that the rest of the renderer receives a validated `ThemeSource`, rather than building arbitrary
paths from strings.

## Build integration

Add theme resolution to `BuildContext`:

```fsharp
type BuildContext =
    { paths: SitePaths
      config: ConfigurationModel
      theme: ThemeSource
      builtAt: DateTimeOffset }
```

`BuildContext.create` should:

1. load and validate configuration;
2. resolve the configured project-local theme;
3. load and validate its manifest;
4. fail before `_public` staging begins if the theme is invalid.

Layout loading should use manifest-declared paths:

```fsharp
let templatePath (theme: ThemeSource) (relativePath: string) =
    safePathCombine theme.root relativePath
```

This removes ad hoc directory enumeration and implicit extension matching from `Layout.fs`.

Asset generation should use the manifest's ordered `styles` and `scripts` lists:

```fsharp
context.theme.manifest.assets.styles
context.theme.manifest.assets.scripts
```

Assets must still be copied into `_public/assets`, but only declared CSS/JS files should receive
automatically generated `<link>` or `<script>` tags.

## Project-local editing workflow

Each initialized site contains the selected theme, so users can customize it directly:

```text
_workspace/themes/whisper/layouts/layout.cshtml
_workspace/themes/whisper/assets/css/screen.css
```

Recommended commands:

```text
jotter theme list
jotter theme validate
jotter theme validate whisper
jotter theme new my-theme
jotter theme copy whisper
jotter theme use my-theme
```

### `theme list`

Lists repository or currently available themes:

```text
architect       1.0.0  Bundled
beautifuld      1.0.0  Bundled
whisper         1.0.0  Bundled
```

### `theme validate`

Validates either all catalog themes or a single named theme.

Checks:

- catalog JSON syntax;
- duplicate IDs and paths;
- path traversal;
- unregistered/missing directories;
- manifest ID/catalog consistency;
- version and compatibility syntax;
- missing templates;
- missing declared assets;
- unknown template keys;
- missing required features;
- fixture rendering;
- generated local-link integrity.

### `theme new <id>`

Scaffolds:

```text
themes/<id>/
  theme.json
  layouts/
    layout.cshtml
    index.cshtml
    post.cshtml
    page.cshtml
  assets/
    css/
      theme.css
  fixtures/
    config.json
    post.markdown
    page.markdown
  preview.png
  README.md
```

It should also add the new entry to `themes.json`.

### `theme copy <id>`

Copies a bundled theme into a site's local `themes` directory. This is useful for advanced
customization when a future configuration refers directly to a bundled/source theme instead of
copying it during `init`.

## Existing themes migration

### Step 1: create catalog metadata

Move the current `src/Jotter.Console/themes/` content to repository-root `themes/`, then add:

- `themes/themes.json`;
- one `theme.json` per theme;
- one preview image per theme;
- a short README per theme.

### Step 2: keep template compatibility

Continue accepting the current paths and names initially:

```text
layout/layout.cshtml
layout/index.cshtml
layout/post.cshtml
layout/page.cshtml
```

The manifest can point at these files. Rename `layout/` to `layouts/` in a later cleanup if desired.

### Step 3: copy themes for distribution

Keep publishing the theme catalog next to the Jotter executable, but copy from the repository-root
`themes/` directory:

```xml
<Content Include="..\..\themes\**\*"
         Link="themes\%(RecursiveDir)%(Filename)%(Extension)"
         CopyToOutputDirectory="PreserveNewest"
         CopyToPublishDirectory="PreserveNewest" />
```

The exact relative path depends on the console project's location.

### Step 4: update tests

Replace the hard-coded list of theme names in tests with catalog-driven discovery:

```fsharp
let manifests =
    ThemeCatalog.load paths
    |> ThemeCatalog.validate
    |> ThemeCatalog.entries
```

For each catalog entry:

- copy/init the theme;
- create standard fixture content;
- force pagination;
- build the site;
- compile all templates;
- validate output links;
- assert required files and metadata.

Do not maintain a separate theme-name list in the test project.

### Step 5: update documentation

Document:

- theme directory structure;
- catalog and manifest schemas;
- how to create a theme;
- which template fields are available;
- fallback behavior;
- how to customize a bundled theme;
- compatibility/version rules;
- validation commands.

## Validation strategy

Theme validation should occur in three places.

### Repository CI

Every theme catalog entry is discovered and rendered through the same fixtures:

- empty site;
- posts and pages;
- pagination;
- tags with multiple pages;
- analytics enabled/disabled;
- comments enabled/disabled.

### CLI build

Before writing staged output, Jotter validates:

- catalog/theme identity;
- manifest schema;
- declared files;
- compatibility range;
- template compilation.

### Theme development command

```text
jotter theme validate ./themes/my-theme
```

allows theme authors to validate a theme before adding it to the catalog.

## Security and path safety

Theme metadata controls file paths, so validation must be strict:

- reject absolute paths;
- reject `..`;
- resolve with `Path.GetFullPath`;
- ensure every resolved path remains under the theme root;
- reject symbolic-link escapes where the platform exposes them;
- reject duplicate output asset paths;
- avoid executing scripts or build hooks from a theme.

A theme must be treated as declarative content. Razor templates are executable during compilation,
so themes used by Jotter must be trusted in the same way as source code in the repository.

## Testing theme metadata separately from rendering

Use two test layers.

### Metadata and file-layout tests

Fast tests that validate:

- JSON schemas;
- catalog uniqueness;
- path containment;
- referenced file existence;
- compatibility ranges;
- mandatory templates;
- duplicate asset declarations.

### Render integration tests

For each valid theme:

1. initialize a fixture site;
2. create pages and at least two posts;
3. set `posts_per_page` to one;
4. ensure common tags produce multiple tag pages;
5. run the atomic build;
6. validate links, assets, RSS, sitemap, and metadata.

This preserves broad compatibility while keeping pure catalog failures quick to diagnose.

## Optional future enhancements

These are useful later but not needed for the first implementation:

- theme inheritance;
- project-level overrides;
- semantic-version compatibility resolution;
- a `jotter theme update` command;
- visual snapshot tests with Playwright;
- optional Git repository source references;
- theme checksums/lock files.

ZIP package support is intentionally excluded from this design.

## Recommended implementation order

1. Create repository-root `themes/themes.json`.
2. Add `theme.json` to each existing theme.
3. Add catalog/manifest DTOs and strict path validation.
4. Update `SitePaths` to locate the repository/published `themes` folder.
5. Move theme selection from directory scans to catalog lookup.
6. Update `Layout` and `Assets` to use manifest paths.
7. Change `init` to copy only the selected theme.
8. Convert the all-theme test to catalog-driven discovery.
9. Add `theme list`, `theme validate`, `theme new`, `theme copy`, and `theme use`.
10. Update README and contributor documentation.

The first milestone should preserve existing `config.json` and template formats while moving
discovery and file resolution to `themes.json` plus `theme.json`.
