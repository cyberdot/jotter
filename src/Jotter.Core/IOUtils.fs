namespace Jotter.Core

open System
open System.Globalization
open System.IO;
open System.Text
open System.Text.RegularExpressions
open Newtonsoft.Json

module IOUtils =

     // Bundled assets (themes, site_files) ship next to the executable, not in the current
     // working directory - that's where the generated site lives instead.
     let resolveAppPath (relativePath: string) = Path.Combine(AppContext.BaseDirectory, relativePath)

     let rec private copyAll (source: DirectoryInfo) (destination: DirectoryInfo) =
        
        let sourceFiles = source.GetFiles()
        for file in sourceFiles do
            let destinationFile = Path.Combine(destination.FullName, file.Name)
            file.CopyTo(destinationFile, true) |> ignore
            
        for dir in source.GetDirectories() do
            let subDir = destination.CreateSubdirectory(dir.Name)
            copyAll dir subDir
    
     let copyDirectory (source: string) (destination: string) =
        let sourceInfo = new DirectoryInfo(source)
        let destinationInfo = Directory.CreateDirectory(destination)
        
        copyAll sourceInfo destinationInfo
        
     // Only spaces used to be replaced, so `?`, `:`, `/`, `#`, quotes and accented characters
     // all ended up in file names and URLs - and on Windows `:` or `?` makes File.WriteAllText
     // throw. Strip diacritics, special-case `#` (otherwise it would just be dropped, turning
     // "C#" and "F#" into the same slug), then collapse anything else non-alphanumeric to `-`.
     let urlSlug (title: string) =
        let withoutSharp = title.Replace("#", "sharp")
        let decomposed = withoutSharp.Normalize(NormalizationForm.FormD)
        let withoutDiacritics =
            decomposed
            |> Seq.filter (fun c -> CharUnicodeInfo.GetUnicodeCategory(c) <> UnicodeCategory.NonSpacingMark)
            |> Seq.toArray
            |> fun chars -> System.String(chars)
        let lowered = withoutDiacritics.ToLowerInvariant()
        Regex.Replace(lowered, "[^a-z0-9]+", "-").Trim('-')

     let discoverMarkdownFiles (directory: string) =
         if not (Directory.Exists(directory)) then
             [||]
         else
             let files =
                 Directory.GetFiles(directory, "*", SearchOption.AllDirectories)
                 |> Array.sort
             let unsupported =
                 files
                 |> Array.filter (fun path ->
                     let extension = Path.GetExtension(path).ToLowerInvariant()
                     extension <> ".markdown" && extension <> ".md")
             if unsupported.Length > 0 then
                 let names = String.Join(", ", unsupported)
                 raise
                     (BuildException
                         $"Unsupported files in content directory '{directory}': {names}. Expected .markdown or .md files.")
             files

     let slugFileName (title: string) =
         let slug = urlSlug title
         if String.IsNullOrWhiteSpace(slug) then
             raise (BuildException $"The title '{title}' does not contain any characters that can form a file name.")
         $"{slug}.markdown"

     let filenameFromTitle (directory: string) (title: string) =
         $"{directory}/{slugFileName title}"

     // File.WriteAllText overwrites silently, so two titles that slug to the same filename
     // (the same title typed twice, or "My Post" vs "my post!") would destroy the earlier
     // file's content with no warning - the same class of problem as the guard already on
     // `init` for overwriting a whole site, just one level down.
     let writeNewFile (path: string) (content: string) =
         if File.Exists(path) then
             raise (BuildException $"{path} already exists. Choose a different title, or edit the existing file directly.")
         File.WriteAllText(path, content)

     let protectInlineJson (value: string) =
         value
             .Replace("<", "\\u003C")
             .Replace(">", "\\u003E")
             .Replace("&", "\\u0026")
             .Replace("\u2028", "\\u2028")
             .Replace("\u2029", "\\u2029")

     let serializeForInlineScript (value: string) =
         JsonConvert.SerializeObject(value) |> protectInlineJson
     
   
            
            
        


