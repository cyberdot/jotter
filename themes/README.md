# Jotter themes

This folder is the source of truth for themes bundled with Jotter.

Each catalog entry in `themes.json` points to a Git-tracked theme directory containing:

- `theme.json` — validated manifest;
- `layout/` — Razor templates;
- `assets/` — styles, scripts, images, and fonts.

## Adding a theme

1. Create a folder using a stable lowercase ID.
2. Add `theme.json`.
3. Add the mandatory `base`, `index`, `post`, and `page` templates.
4. Declare every automatically linked CSS and JavaScript asset in manifest order.
5. Add an entry to `themes.json`.
6. Run:

```text
dotnet run --project src/Jotter.Console -- theme validate
dotnet test Jotter.slnx
```

The manifest and catalog use repository-relative paths. Absolute paths, `..`, duplicate IDs,
unregistered directories, missing files, and malformed JSON are rejected.

See [the theme design document](../doc/themes.md) for the full schema and migration design.
