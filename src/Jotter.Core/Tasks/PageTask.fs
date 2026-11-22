namespace Jotter.Core

module PageTask =
    let run (paths: SitePaths) (title: string) =
        if System.String.IsNullOrWhiteSpace(title) then
            raise (BuildException "Cannot create a new page without page name")
        else
            Page.create paths title

