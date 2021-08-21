namespace Jotter.Core

module PostTask =
    let run (title: string) =
        if System.String.IsNullOrWhiteSpace(title) then
            failwith "Cannot create new post without post name"
        else
            Post.create title false |> ignore

