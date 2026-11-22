namespace Jotter.Core

module PublishTask =
    let run (paths: SitePaths) (title: string) =
        if System.String.IsNullOrWhiteSpace(title) then
            raise (BuildException "Cannot publish a draft without its title")
        else
            Draft.publish paths title
