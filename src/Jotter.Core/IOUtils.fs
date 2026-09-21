namespace Jotter.Core

open System
open System.IO;

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
        
     let urlSlug (title: string) = title.ToLowerInvariant().Replace(" ", "-")

     let slugFileName (title: string) = $"{urlSlug title}.markdown"

     let filenameFromTitle (directory: string) (title: string) =
         $"{directory}/{slugFileName title}"
     
   
            
            
        


