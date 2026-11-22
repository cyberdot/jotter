# GitHub Theme Delivery and Local Cache Design

> **Implementation status:** Catalog and selected-theme retrieval, atomic cache writes, offline
> cache use, local-development precedence, `theme update`, and publish exclusion are implemented.

## Goal

Distribute the Jotter executable without bundling theme templates or assets, and fetch themes from
the `cyberdot/jotter` GitHub repository only when needed.

The desired user experience is:

```text
jotter theme list
jotter init whisper
jotter theme copy architect
```

Each command should work with a normal released binary and use no internet except when required
metadata or theme files are absent from the local cache.

## Non-goals

This design does not:

- introduce ZIP theme packages;
- load themes from arbitrary untrusted repositories by default;
- automatically update an existing site's project-local theme;
- execute downloaded build scripts or theme code;
- require Git to be installed.

The first delivery mechanism should remain ordinary GitHub repository content accessed over
HTTPS.

## Source of truth

Themes remain Git-tracked under:

```text
https://github.com/cyberdot/jotter/
  themes/
    themes.json
    architect/
      theme.json
      layout/
      assets/
    whisper/
      theme.json
      layout/
      assets/
```

`themes/themes.json` is the catalog. Each `theme.json` remains the selected theme's manifest.

The release executable should contain no `themes/` directory. The release workflow may still use
the repository themes for tests, but it must not include them in the published archive.

## High-level flow

```text
CLI command
  -> project-local theme?
       yes -> use _workspace/themes/<id>
       no  -> local development catalog?
             yes -> use executable/repository themes
             no  -> cached catalog available?
                    yes -> use cache
                    no  -> download from GitHub
```

Theme retrieval is completed in a local cache before copying a theme into a user's site. Jotter
never renders directly from a partially downloaded network response.

## Release artifact

A release archive should contain:

```text
jotter executable
site_files/
  robots.txt
  CNAME
```

It should not contain:

```text
themes/
```

During local repository development, a `themes/` directory may still be copied to build/test
output. Published releases should set the theme content items to `CopyToPublishDirectory=Never`.

## Network endpoints

For the default repository and ref:

```text
repository: cyberdot/jotter
ref: master
```

The catalog can be retrieved from:

```text
https://raw.githubusercontent.com/cyberdot/jotter/master/themes/themes.json
```

A theme directory can be enumerated with the GitHub Contents API:

```text
https://api.github.com/repos/cyberdot/jotter/contents/themes/whisper?ref=master
```

Each returned file is downloaded from its `download_url`. Directory entries are traversed
recursively.

This downloads only the catalog and the requested theme directory, rather than an entire
repository archive.

## Configuration

The default source is fixed to `cyberdot/jotter`, while environment variables enable development
and testing:

```text
JOTTER_THEMES_REPOSITORY=owner/repository
JOTTER_THEMES_REF=master
GITHUB_TOKEN=<optional token>
GH_TOKEN=<optional token>
```

`GITHUB_TOKEN` and `GH_TOKEN` are used only for HTTPS authorization and must never be written to
the cache or site.

A future release should pin the default ref to a release tag, for example `v1.4.0`, rather than
`master`. A documented development fallback may continue using `master` when the binary has no
release version.

## Cache layout

Use a per-user application cache rather than writing into the executable directory:

Windows:

```text
%LOCALAPPDATA%\Jotter\themes\
```

macOS:

```text
~/Library/Caches/Jotter/themes/
```

Linux:

```text
${XDG_CACHE_HOME:-~/.cache}/jotter/themes/
```

Separate cache namespaces by repository and ref:

```text
cacheRoot/
  <repository-hash>/
    <ref-hash>/
      themes.json
      architect/
        theme.json
        layout/
        assets/
      whisper/
        theme.json
        layout/
        assets/
      metadata.json
```

Hashing repository and ref values prevents special characters or slashes from becoming unsafe
filesystem paths. Human-readable repository/ref values belong in `metadata.json`, not directory
names.

## Cached catalog behavior

`jotter theme list` should:

1. Look for a valid local cache entry.
2. If present and not expired, parse and display it.
3. If absent or expired, download `themes/themes.json`.
4. Validate the catalog.
5. Atomically replace the cached catalog.
6. If GitHub is unavailable and a stale cache exists, display the stale catalog with a warning.

The initial catalog TTL can be 24 hours. No theme directory needs to be downloaded merely to list
themes.

## Selected theme behavior

`jotter init whisper` or `jotter theme copy whisper` should:

1. resolve `whisper` from the catalog;
2. look for a complete validated `whisper` directory in the cache;
3. if absent, download the complete `themes/whisper` Git directory recursively;
4. validate `theme.json` and all declared paths;
5. atomically mark the cached theme complete;
6. copy it into the selected site's `_workspace/themes/whisper`;
7. continue with normal site initialization.

The requested theme is the only theme downloaded. Other directories remain absent from the cache.

## Project-local precedence

Resolution order should be:

```text
_workspace/themes/<id>
  -> local development catalog beside executable
  -> per-user cache
  -> GitHub
```

For an existing site, a valid `_workspace/themes/<id>` is always authoritative. Building an
existing site must not contact GitHub or silently replace edits.

This preserves the current behavior:

- a site owns an editable copy of its theme;
- remote updates never overwrite user modifications;
- offline builds remain possible after initialization.

## Cache completeness marker

Do not consider a directory usable simply because it exists. Record a completion marker:

```json
{
  "theme": "whisper",
  "version": "1.0.0",
  "repository": "cyberdot/jotter",
  "ref": "master",
  "downloadedAt": "2026-11-21T21:00:00Z",
  "commit": "<git blob/tree commit when available>"
}
```

The cache root can use `.complete` markers and temporary sibling directories:

```text
whisper.tmp-<id>/
whisper/
whisper.complete.json
```

Only a directory with a valid matching completion marker is considered cached.

## Catalog validation

Catalog download validation must reject:

- unsupported schema versions;
- malformed JSON;
- duplicate theme IDs;
- duplicate paths;
- absolute paths;
- `..` path components;
- paths escaping the `themes/` root;
- untrusted URLs embedded in metadata;
- unknown catalog fields if strict parsing is enabled.

Catalog validation must not require every theme directory to already exist locally. Missing
directories are expected until a theme is selected and downloaded.

## Theme download validation

After downloading a theme:

1. validate all GitHub response paths remain below `themes/<themePath>/`;
2. reject symlinks and submodules;
3. reject absolute and traversal paths;
4. reject files larger than configured limits;
5. reject excessive directory/file counts;
6. parse `theme.json`;
7. verify the manifest ID matches the catalog ID;
8. verify mandatory templates exist;
9. verify every declared asset exists;
10. verify the cache is compatible with the running Jotter version.

Recommended defensive defaults:

- total theme download: 25 MB;
- individual file: 10 MB;
- maximum files: 2,000;
- maximum directories/API requests: 200;
- response timeout: 60 seconds.

These can be tuned after measuring the bundled themes.

## Atomic downloads

Downloads must not write directly into the final cache directory.

```text
download into whisper.tmp-<id>
  -> validate
  -> write completion marker
  -> rename to whisper
```

If GitHub fails midway:

- delete the temporary directory;
- retain a prior complete cached theme;
- return an actionable error.

If `init` fails after theme retrieval, the cached theme may remain because it is complete and
reusable.

## CLI commands

### `theme list`

Uses only `themes.json`.

```text
jotter theme list
```

Optional:

```text
jotter theme list --refresh
```

### `theme copy`

Downloads or uses one cached theme, then copies it into `_workspace/themes`.

```text
jotter theme copy whisper
```

### `theme validate`

```text
jotter theme validate
jotter theme validate whisper
```

Without an ID:

- validate the catalog;
- validate project-local themes;
- validate bundled themes already present in cache.

It should not unexpectedly download every remote theme. A separate option can perform full remote
validation:

```text
jotter theme validate --remote
```

### `theme update`

```text
jotter theme update whisper
jotter theme update --catalog
```

This forces a cache refresh. Updating the cache must not modify `_workspace/themes/whisper`.
Users explicitly copy or replace project-local content if they want an update.

## Offline behavior

The following should work offline after initial retrieval:

- `build`;
- `server`;
- `theme list`, when catalog cache exists;
- `init <theme>`, when that theme exists in cache;
- `theme copy <id>`, when that theme exists in cache;
- validation of project-local and cached themes.

When a required remote entry is not cached, fail with a concise error:

```text
Theme 'whisper' is not cached and GitHub is unavailable.
Run this command while online once, or copy the theme into _workspace/themes/whisper.
```

Do not silently fall back to another theme.

## Authentication and rate limits

Unauthenticated GitHub API access has a low rate limit. Use:

- conditional requests/ETags for catalog refresh;
- cached tree entries where practical;
- no API calls for an existing complete theme;
- optional `GITHUB_TOKEN` or `GH_TOKEN`;
- raw GitHub for known files where possible.

If rate-limited, show:

- whether a cached version is available;
- the time of the last successful download;
- a suggestion to configure a token.

Tokens must only be sent to trusted GitHub API hosts.

## Ref/version policy

### Development

Use `master` and `JOTTER_THEMES_REF`.

### Releases

Prefer the matching release tag:

```text
Jotter 1.4.0 -> v1.4.0
```

This gives a stable theme catalog and source contents for each binary release.

Compatibility still needs to be checked through each theme's `theme.json` `jotter` range. A newer
binary may intentionally consume themes from a newer tag; an older binary must not consume a theme
outside its compatibility range.

## Local development and tests

Tests must not depend on GitHub.

Use a local development source:

```text
AppContext.BaseDirectory/themes/
```

or an explicit test override in `SitePaths`.

When a local catalog exists, it wins and no network request occurs. The release build excludes
this local theme content.

Unit tests should cover:

- catalog parsing and validation;
- missing cache download decision;
- stale-cache fallback;
- path traversal rejection;
- unsupported GitHub entry types;
- partial-download cleanup;
- existing project theme preventing network access;
- catalog-only listing;
- one-theme-only download;
- refresh not modifying project-local themes.

Network integration tests should be optional and separately gated.

## Implementation components

### `ThemeRepository`

Responsibilities:

- choose local development source or remote cache;
- fetch catalog;
- fetch one theme directory;
- write atomic cache entries;
- implement refresh and stale-cache fallback;
- expose repository/ref metadata.

### `Theme`

Responsibilities:

- validate catalog and manifests;
- resolve project-local versus cached themes;
- copy a selected theme into `_workspace`;
- avoid network concerns.

### `SitePaths`

Responsibilities:

- project directories;
- local development theme path;
- platform-specific cache root.

### `Program`

Responsibilities:

- expose `list`, `copy`, `validate`, and `update`;
- print download/cache information;
- map network errors to concise CLI messages.

## Release workflow change

Change theme content publishing from:

```xml
CopyToPublishDirectory="PreserveNewest"
```

to:

```xml
CopyToPublishDirectory="Never"
```

Keep output-copying for repository builds if local development relies on it. The release should
then contain no bundled theme templates or assets.

## Migration plan

1. Add a platform-aware theme cache root to `SitePaths`.
2. Add `ThemeRepository` without changing current local behavior.
3. Make local development catalogs take precedence.
4. Implement catalog download and validation.
5. Implement one-directory GitHub download with atomic cache writes.
6. Change `init` and `theme copy` to resolve through `ThemeRepository`.
7. Add stale/offline handling and `theme update`.
8. Remove themes from publish output.
9. Add local, mocked, and optional GitHub integration tests.
10. Publish a transition release and verify that clean-machine `init` downloads only the selected
    theme.

## Recommended outcome

The executable remains small and self-contained, while theme source stays in the Jotter Git
repository.

The user pays no theme download cost until they list themes, initialize a site, or copy a theme.
Once a theme is installed in `_workspace`, it is fully local, editable, stable, and buildable
offline.
