namespace Jotter.Core

open System.IO;

module IOUtils =
    
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
        
     let filenameFromTitle (directory: string) (title: string) =
         let titlePart = title.ToLowerInvariant().Replace(" ", "-")
         $"{directory}/{titlePart}.markdown"
         
     let urlSlug (title: string) = title.ToLowerInvariant().Replace(" ", "-")
     
   
            
            
        


