namespace Jotter.Core

module PublishTask =
    let run (title: string) =
        if System.String.IsNullOrWhiteSpace(title) then
            failwith "Cannot publish a draft without its title"
        else
            Draft.publish title
