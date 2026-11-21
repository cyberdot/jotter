namespace Jotter.Core

module DraftTask =
    
    let run (paths: SitePaths) (title: string) =
        if System.String.IsNullOrWhiteSpace(title) then
            raise (BuildException "Cannot create new draft without post name")
        else
            Draft.create paths title
        

