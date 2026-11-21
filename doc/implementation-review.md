# Jotter Implementation Review and Refactoring Recommendations

**Reviewed:** 2026-11-21  
**Revision:** `7dcff5c` on `feature/bug-fixes`  
**Scope:** solution structure, `Jotter.Core`, CLI entry point, tests, bundled Razor templates, and CI workflows

## Executive summary

Jotter has a clear three-project structure, a small and understandable module layout, and useful test coverage. The core publishing pipeline is also easy to follow: discover Markdown, parse front matter, render posts/pages, then generate indexes, tags, RSS, and a sitemap.

The main risks are not the F# design style but inconsistent boundaries around that pipeline:

- Tag pagination can fail at runtime because tag indexes and the main index expose different view-model property names.
- Tag archive pages generate asset links relative to `/tags/`, while assets are published at `/assets/`.
- The build deletes and repopulates `_public` incrementally, so a late rendering failure can leave a partial deployment.
- Configuration, paths, clocks, and current-directory state are repeatedly read from global process/file-system state.
- Razor templates are compiled against loosely shaped objects, so model/template mismatches are discovered only when a particular branch executes.

The recommended direction is to introduce one validated build context, one shared paging model, an atomic output stage, and end-to-end rendering tests across every bundled theme.

## Verified baseline

- `dotnet test Jotter.slnx --configuration Release` passes: **75 tests, 0 failures**.
- A clean isolated build with two Poole posts and `posts_per_page = 1` fails while rendering the tag archive:
  - `Jotter.Core.Tags.TagIndexViewModel` does not contain a definition for `nextFileName`.
- A single-page Poole tag archive builds, but its generated stylesheet links are `./assets/...`, which resolve relative to `/tags/` rather than the published `/assets/` directory.

## Implementation status

The recommendations were implemented on 2026-11-21:

- shared validated `SitePaths` and `BuildContext`;
- a single strongly typed pagination model;
- validated configuration and front matter;
- explicit output URL generation;
- inline JavaScript/JSON serialization and generated JSON-LD;
- consistent `.markdown`/`.md` discovery;
- atomic staged builds with rollback and output-link validation;
- corrected links and missing templates across all bundled themes;
- controlled CLI errors and a simpler static-file preview server;
- parallel test execution and a seven-theme, multi-page integration matrix;
- push/PR CI triggers, run concurrency, and scoped `NU1608` suppression.

The superseded “priority findings” below remain useful as the rationale and regression record for
this refactor. The verified baseline numbers describe the pre-refactor state.

## Priority findings

### 1. Critical: multi-page tag archives fail at runtime

**Evidence**

- [`Index.IndexViewModel`](../src/Jotter.Core/Index.fs#L26) defines `prevFileName` and `nextFileName`.
- [`Tags.TagIndexViewModel`](../src/Jotter.Core/Tags.fs#L27) defines the incompatible names `prevFilePath` and `nextFilePath`.
- The same shared index templates used for both flows expect the `prevFileName` and `nextFileName` properties.
- The mismatch is only exercised when a tag has more than one page, which the existing tests do not cover.

**Recommended change**

1. Move the index/pagination model into one shared module.
2. Use that model for both the home index and tag archives.
3. Add an end-to-end test that creates enough posts in one tag to produce at least two pages.
4. Assert that all generated tag navigation links point to existing files.

Do not fix this with a second conditional template. The shared templates should have one explicit contract.

### 2. High: tag pages emit broken asset URLs

**Evidence**

[`Tags.fs`](../src/Jotter.Core/Tags.fs#L47) uses `Assets.js(".")` and `Assets.css(".")`, and the tag-index layout uses the same root at [line 105](../src/Jotter.Core/Tags.fs#L105). Tag output is written below `_public/tags/`, while assets are below `_public/assets/`.

A generated tag archive therefore contains:

```html
<link rel="stylesheet" href="./assets/css/01_poole.css" />
```

The browser resolves that to `/tags/assets/...`, not `/assets/...`.

**Recommended change**

- Centralize URL generation by output location rather than passing `"."` or `".."` from callers.
- For tag pages, use a root-relative path (`/assets/...`) or a computed relative path from the output file.
- Add a test that checks generated asset references resolve to files in the published tree.

### 3. High: a failed build can leave a partial `_public` deployment

**Evidence**

[`BuildTask.run`](../src/Jotter.Core/Tasks/BuildTask.fs#L11) deletes/prepares `_public` before parsing and rendering all content. It then writes pages, tags, posts, RSS, the home index, and the sitemap incrementally.

The confirmed tag-pagination exception occurs after `_public` has already been replaced and after earlier build products may have been written.

**Recommended change**

Refactor the pipeline into:

1. Load and validate configuration.
2. Discover, parse, and validate all input documents.
3. Resolve the theme and all required templates.
4. Render the complete site into a fresh staging directory.
5. Verify required output invariants.
6. Replace the previous output only after the staged build succeeds.
7. Retain or restore the previous output on failure.

The staging directory should be a sibling of `_public` on the same volume so the final replacement can use directory moves. This is a more important correctness improvement than parallelizing the build.

### 4. High: rendering relies on a loosely typed, runtime-only template contract

**Evidence**

- [`Renderer.render`](../src/Jotter.Core/Renderer.fs#L35) accepts `obj`.
- Index, page, post, tag, and layout models are declared independently.
- `Tags.fs` uses anonymous records for its tag index.
- [`Post.fs`](../src/Jotter.Core/Post.fs) contains separate post-layout and post-content models whose differences must be remembered manually.

RazorLight therefore discovers property mismatches at runtime. The tag pagination failure is a direct example.

**Recommended change**

Define named records for each actual template contract and render through wrappers that accept those specific types, for example:

- `SiteLayoutModel`
- `IndexTemplateModel`
- `PostTemplateModel`
- `TagIndexTemplateModel`
- `PageTemplateModel`

The common fields should live in a reusable base/context record. Anonymous Razor models should be removed.

Add a rendering matrix test that:

- enumerates bundled themes automatically;
- renders page, post, index, tag-index, and multi-page tag outputs for each applicable theme;
- verifies every selected layout fallback;
- catches missing Razor properties before release.

Keep the Razor templates and RazorLight unless there is a specific product reason to replace them; the issue is the unverified model boundary, not the engine itself.

### 5. High: configuration and path state are global, repeatedly re-read, and incompletely validated

**Evidence**

- [`Config.data`](../src/Jotter.Core/Config.fs#L84) reads `config.json` on every call.
- Config is reread independently by `Page`, `Post`, `Index`, `Tags`, `Posts`, `Rss`, `SiteMap`, and `Theme`.
- [`Layout.basePath`](../src/Jotter.Core/Layout.fs#L13) derives the theme from a fresh config read, then repeatedly enumerates the layout directory.
- The paths in `Config.fs` are fixed relative strings tied to `Directory.GetCurrentDirectory()`.
- `postsPerPage` is validated, but `sortPosts`, URLs, required strings, date format, theme, and deserialization nullability are not comprehensively validated.
- An invalid `sort_posts` silently preserves the source order in [`Posts.sortByDate`](../src/Jotter.Core/Posts.fs).

**Recommended change**

Introduce a validated build context:

```fsharp
type SitePaths =
    { Root: string
      Workspace: string
      Public: string
      BundledThemes: string }

type BuildContext =
    { Paths: SitePaths
      Config: SiteConfiguration
      Theme: ThemeModel
      Now: DateTimeOffset }
```

Pass it through `Page`, `Post`, `Index`, `Tags`, `Rss`, `SiteMap`, and `Assets`. Load and validate once per command/build.

Separate persisted JSON DTOs from validated domain values:

- `SortOrder` discriminated union instead of arbitrary `string`
- validated absolute `Uri` for the configured site URL
- positive nonzero page size
- validated date format
- required/nonblank theme name
- required front-matter fields and useful field-level diagnostics

Prefer a `Result` containing an aggregated list of validation errors over scattered `failwith` calls.

### 6. High: the build is coupled to process-wide current-directory mutation

**Evidence**

Production paths assume the process starts at the site root, while [`TestHelpers.withTempWorkingDirectory`](../src/Jotter.Core.Tests/TestHelpers.fs#L54) mutates the global current directory. Parallel test collections are disabled in [`xunit.runner.json`](../src/Jotter.Core.Tests/xunit.runner.json#L2) to make that mutation safe.

This couples library behavior and test execution to a process-wide setting and prevents safe parallelism.

**Recommended change**

- Build `SitePaths` from an explicit root directory.
- Use the explicit root in every file-system operation.
- Use `Path.Combine` for platform-correct path construction.
- Keep a small CLI default of `Directory.GetCurrentDirectory()`, but keep it at the command boundary.
- Let tests create a `SitePaths` instance for a temp directory instead of changing the current directory.
- Re-enable test parallelization after the global-state dependency is removed.

There is no need for a large repository abstraction unless the tests demonstrate one is useful. Path injection alone removes most of the coupling.

### 7. Medium: input discovery is inconsistent and permissive

**Evidence**

- [`Post.list`](../src/Jotter.Core/Post.fs#L121) and [`Page.list`](../src/Jotter.Core/Page.fs#L114) process every file in their source directories.
- `Draft.list` specifically searches recursively for `*.markdown`.
- [`Document.fileName`](../src/Jotter.Core/Document.fs#L50) performs a string replacement only for `.markdown`.
- README terminology allows users to reasonably expect Markdown inputs, while the implementation accepts only the `.markdown` generated by Jotter.

**Recommended change**

Create one content-discovery module that:

- accepts an explicit set of supported extensions (`.markdown`, optionally `.md`);
- rejects or reports unsupported unexpected files;
- applies identical recursion/order rules to posts, pages, and drafts;
- derives output names with `Path.ChangeExtension`;
- guarantees that a discovered file maps to a unique output path.

Also reject a title whose slug is empty before creating `.markdown`.

### 8. Medium: front matter deserialization and document output need a validation boundary

**Evidence**

[`Document.splitIntoParts`](../src/Jotter.Core/Document.fs#L96) directly deserializes the parsed JSON into `FrontMatter`. A syntactically valid but semantically incomplete document can yield default/null values that fail later during sorting, templating, or URL generation.

**Recommended change**

Return a structured document result containing:

- source path;
- title and description;
- validated creation date;
- normalized tags;
- normalized/validated slug;
- Markdown body.

Aggregate all document errors with the file path and field name before any `_public` replacement begins. This also gives CLI users actionable messages instead of Razor or `NullReferenceException` failures.

### 9. Medium: URL and metadata construction is duplicated and inconsistent

**Evidence**

- Post, page, RSS, sitemap, sharing links, canonical links, and static asset links each construct URLs independently.
- Several Whisper templates use the site URL as a post canonical URL, and its Twitter metadata omits the actual `/posts/<file>` location:
  [`post_layout.cshtml`](../src/Jotter.Console/themes/whisper/layout/post_layout.cshtml#L20).
- A trailing slash in `config.url` can produce double slashes in string concatenation.
- The `slug` front-matter field is not the authoritative output filename, even though themes use it in sharing metadata.

**Recommended change**

Create one URL utility based on `Uri`, with explicit:

- `PageUrl`
- `PostUrl`
- `TagUrl`
- `AssetUrl`
- `FeedUrl`

Construct canonical and sharing metadata from the rendered document's actual final URL. Decide whether the filename or front-matter slug is authoritative and enforce that rule during validation.

### 10. Medium: JavaScript and JSON embedding should use serializers, not ad hoc escaping

**Evidence**

[`escapeForJsString`](../src/Jotter.Core/IOUtils.fs#L65) escapes only backslashes and apostrophes. It does not fully address line separators or script-content boundaries.

Whisper templates also interpolate site/post values directly into `application/ld+json` in [`post_layout.cshtml`](../src/Jotter.Console/themes/whisper/layout/post_layout.cshtml#L43).

**Recommended change**

- Use a JSON serializer to generate JavaScript string literals for Google Analytics and Disqus values.
- Generate JSON-LD as an object in F# and serialize it once.
- Keep `@Raw` limited to intentionally rendered Markdown and deliberate HTML/JSON helpers.
- Add tests for quotes, newlines, Unicode line separators, `</script>`, and HTML characters.

This is defense in depth for generated files. It also reduces malformed metadata for ordinary punctuation.

### 11. Medium: bundled themes contain correctness and consistency drift

Examples:

- The Architect post heading is malformed at [`architect/layout/post.cshtml`](../src/Jotter.Console/themes/architect/layout/post.cshtml#L2).
- [`uno/layout/layout.cshtml`](../src/Jotter.Console/themes/uno/layout/layout.cshtml#L69) hard-codes the `cyberdot` account instead of the configured social account.
- Poole's pagination labels are reversed relative to the usual descending-post semantics in [`poole/layout/index.cshtml`](../src/Jotter.Console/themes/poole/layout/index.cshtml#L17).
- Canonical, Open Graph, JSON-LD, RSS, asset, and social URLs vary between themes.
- Several old hard-coded footer years, product versions, and external dependencies differ by theme.

**Recommended change**

Do not rely only on source-text assertions. Add a baseline contract for every theme and then render snapshots or semantic assertions:

- valid page title;
- relative links resolve to generated files;
- canonical URL matches the current output;
- expected metadata exists;
- configured author/social fields are used;
- pagination controls are correct;
- comments and analytics are rendered only when enabled.

Fix the malformed Architect heading as part of this contract work.

### 12. Medium: remove duplicate/dead abstractions and consolidate paging/rendering logic

Examples:

- [`Store.fs`](../src/Jotter.Core/Store.fs) only appends lists with `@`; the current bulk preparation paths already avoid this O(n) operation.
- `Index` and `Tags` duplicate view-model construction.
- `Page` and `Post` duplicate front-matter loading, Markdown conversion, URL construction, and two-stage template rendering.
- Layout lookup, file discovery, asset URL generation, and directory copying are spread across several modules.

**Recommended change**

Introduce narrowly focused shared services/modules:

- `SourceDiscovery`
- `MarkdownDocument`
- `Paging`
- `OutputUrl`
- `TemplateCatalog`
- `SiteBuild`

Retire `Store` if it has no stateful responsibility after the consolidation.

Avoid a broad “engine” framework or an over-general plugin system. Jotter is small enough that explicit records and functions will be easier to maintain.

### 13. Medium: CLI errors should be controlled user-facing failures

**Evidence**

The task modules and model modules throw via `failwith`, and the CLI directly invokes them in [`Program.fs`](../src/Jotter.Console/Program.fs#L20). The verified tag-pagination probe printed a full aggregate exception and stack trace.

**Recommended change**

- Use named exception/domain error types or `Result<_, UserError list>` internally.
- Catch expected user/build errors at the CLI boundary.
- Print a concise `error: ...` message with source path where applicable.
- Return a stable nonzero exit code.
- Keep an unexpected exception/stack-trace mode for diagnostics.
- Add command tests for invalid titles, missing workspaces, unknown themes, malformed config, and failed builds.

### 14. Medium: simplify the preview server and add startup validation

**Evidence**

[`ServerTask.fs`](../src/Jotter.Core/Tasks/ServerTask.fs) combines ASP.NET static files, a separate Giraffe route, and manually constructed relative paths from `Config.publicDirectory`.

**Recommended change**

- Resolve `_public` from `SitePaths`.
- Fail clearly if the directory does not exist and suggest running `build`.
- Use static-file middleware and default files directly unless Giraffe is needed for planned application behavior.
- Add a configurable listen URL/port.
- Add one smoke test that starts the host, requests `/`, a post, a tag page, and an asset, then stops cleanly.

### 15. Medium: CI does not match the documented/default safety model

**Evidence**

[`build.yml`](../.github/workflows/build.yml#L4) is manual-only, while [`README.md`](../README.md#L107) says build/test runs on branch pushes and pull requests.

**Recommended change**

- Restore `pull_request` and `push` triggers for the build/test workflow.
- Keep release manual unless automatic releases are explicitly desired.
- Add build concurrency to cancel superseded branch runs.
- Include a smoke build that renders a multi-page tag archive across bundled themes.
- Optionally add formatting/static analysis and package-update automation.

### 16. Low: remove build-wide warning suppression after resolving package constraints

**Evidence**

[`Directory.Build.props`](../Directory.Build.props) suppresses `NU1608` for the entire solution because of an FSharp.Formatting/FSharp.Core version constraint.

**Recommended change**

- Scope the suppression as narrowly as possible.
- Document the compatibility evidence in a package-management note or a package-specific project property.
- Re-evaluate it after dependency upgrades.
- Consider central package management for consistent versions if the number of projects grows.

The current suppression is understandable for this repository size, but solution-wide warning suppression can hide future package-drift problems unrelated to the original dependency conflict.

## Suggested target build flow

```text
CLI
  -> resolve SitePaths
  -> load/validate SiteConfiguration
  -> discover SourceDocuments
  -> parse/validate all documents
  -> load/validate ThemeCatalog
  -> render complete site into staging directory
  -> verify output links/manifests
  -> replace _public
```

Key implementation rules:

- one configuration load per command;
- no module reads `Directory.GetCurrentDirectory()` outside the CLI boundary;
- one shared pagination model for home and tag archives;
- all asset/URL generation based on final output paths;
- no output replacement until validation and rendering succeed;
- expected failures are returned as diagnostics, not `failwith`;
- every bundled theme is rendered by the test suite.

## Refactoring sequence

### Phase 1: stop current failures

1. Fix the `nextFileName`/`prevFileName` tag model mismatch.
2. Fix tag archive/index asset base paths.
3. Add the multi-page tag end-to-end regression test.
4. Fix the Architect heading and the highest-value canonical/social URL inconsistencies.
5. Restore CI triggers or update the README so documented behavior is accurate.

### Phase 2: establish reliable boundaries

1. Add `SitePaths` and a validated `BuildContext`.
2. Load config once and pass it explicitly.
3. Create shared typed Razor models and shared paging logic.
4. Consolidate document discovery and validation.
5. Add a reusable theme-rendering matrix test.

### Phase 3: make deployment atomic

1. Render into a staging directory.
2. Add output validation.
3. Replace `_public` only after success and support rollback.
4. Add CLI error handling and stable exit codes.

### Phase 4: consolidate and modernize

1. Centralize URLs, metadata, and JavaScript/JSON serialization.
2. Remove `Store` and other no-longer-useful abstractions.
3. Reduce repeated layout/file-system discovery by caching per-build data in `BuildContext`.
4. Simplify the preview server.
5. Tighten dependency warning policy and add formatting/static analysis if useful.

## Test additions with the highest return

1. Multi-page tag archive build and navigation-file assertions.
2. Asset-link resolution from root, post, page, tag, and tag-index outputs.
3. One build for every bundled theme and every page kind.
4. Failed build preserves the previous `_public` directory.
5. Malformed config/front matter reports all relevant errors before output replacement.
6. Config URL, slug collisions, unsupported files, empty slugs, and trailing-slash URL cases.
7. JavaScript/JSON embedding with hostile but syntactically valid values.
8. CLI exit-code and concise-error tests.
9. Preview server smoke tests.
10. CI-level generated-site smoke test.

## Strengths to preserve

- Small, explicit project split between core, console, and tests.
- F# records and module-oriented organization are straightforward for this domain.
- The content/build pipeline is conceptually simple and should remain so.
- Bulk `prepareAll` functions already avoid repeated list appends.
- Razor template caching is sensible and should be retained.
- Several existing tests capture valuable regressions around parsing, slugs, file overwrite protection, RSS handling, and asset subpaths.
- Bundled themes are content rather than compiled resources, which keeps publishing and inspection practical.

The goal of the refactor should be to make the existing pipeline explicit and verifiable, not to introduce a larger framework or change product behavior unnecessarily.
