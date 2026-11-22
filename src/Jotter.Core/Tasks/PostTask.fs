namespace Jotter.Core

module PostTask =
    let run (paths: SitePaths) (title: string) =
        if System.String.IsNullOrWhiteSpace(title) then
            raise (BuildException "Cannot create new post without post name")
        else
            Post.create paths title false |> ignore

