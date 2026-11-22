namespace Jotter.Core

open System
open System.IO
open System.Text.RegularExpressions

module OutputValidation =
    let private requiredFiles =
        [ "index.html"; Rss.rssFileName; "sitemap.xml" ]

    let private referencePattern =
        Regex(
            """(?:href|src)\s*=\s*["'](?<path>[^"'#]+)["']""",
            RegexOptions.IgnoreCase ||| RegexOptions.Compiled)

    let private basePattern =
        Regex(
            """<base\s+href\s*=\s*["'][^"']+["']""",
            RegexOptions.IgnoreCase ||| RegexOptions.Compiled)

    let private resolveReference (publicDirectory: string) (htmlFile: string) (hasBase: bool) (reference: string) =
        let withoutQuery = reference.Split('?', 2).[0]
        if String.IsNullOrWhiteSpace(withoutQuery)
           || withoutQuery.StartsWith("//")
           || withoutQuery.Contains("{")
           || withoutQuery.Contains("}") then
            None
        else
            let lower = withoutQuery.ToLowerInvariant()
            if lower.StartsWith("http:")
               || lower.StartsWith("https:")
               || lower.StartsWith("mailto:")
               || lower.StartsWith("tel:")
               || lower.StartsWith("javascript:")
               || lower.StartsWith("data:") then
                None
            elif withoutQuery = "/" then
                Some(Path.Combine(publicDirectory, "index.html"))
            elif withoutQuery.StartsWith("/") then
                Some(Path.GetFullPath(Path.Combine(publicDirectory, withoutQuery.TrimStart('/'))))
            else
                let baseDirectory =
                    if hasBase then publicDirectory
                    else Path.GetDirectoryName(htmlFile)
                Some(Path.GetFullPath(Path.Combine(baseDirectory, withoutQuery)))

    let validate (paths: SitePaths) =
        let errors = ResizeArray<string>()

        for relativePath in requiredFiles do
            let path = SitePaths.inPublic paths relativePath
            if not (File.Exists(path)) then
                errors.Add($"Required output is missing: {relativePath}")

        for htmlFile in Directory.GetFiles(paths.publicDirectory, "*.html", SearchOption.AllDirectories) do
            let content = File.ReadAllText(htmlFile)
            let hasBase = basePattern.IsMatch(content)
            for referenceMatch in referencePattern.Matches(content) do
                let reference = referenceMatch.Groups.["path"].Value.Trim()
                match resolveReference paths.publicDirectory htmlFile hasBase reference with
                | Some target ->
                    let target =
                        if Path.GetExtension(target) = String.Empty && Directory.Exists(target) then
                            Path.Combine(target, "index.html")
                        else
                            target
                    if not (File.Exists(target)) then
                        let relativeHtml =
                            Path.GetRelativePath(paths.publicDirectory, htmlFile).Replace('\\', '/')
                        errors.Add(
                            $"Broken local reference in {relativeHtml}: '{reference}' does not resolve to a generated file.")
                | None -> ()

        if errors.Count > 0 then
            Error(List.ofSeq errors)
        else
            Ok()

