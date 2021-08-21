namespace Jotter.Core

module DraftTask =
    
    let run (title: string) =
        if System.String.IsNullOrWhiteSpace(title) then
            failwith "Cannot create new draft without post name"
        else
            Draft.create title
        

