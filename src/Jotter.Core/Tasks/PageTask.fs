namespace Jotter.Core

module PageTask =
    let run (title: string) =
        if System.String.IsNullOrWhiteSpace(title) then
            failwith "Cannot create a new page without post name"
        else
            Page.create title

