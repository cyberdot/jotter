namespace Jotter.Core

open Jotter.Core.Config
open Jotter.Core.Document

module Posts =
    
    let sortByDate (config: ConfigurationModel) (posts: Post list) =
        match config.sortPosts with
        | PostSort.Ascending -> posts |> List.sortBy (fun p -> p.dateCreated)
        | PostSort.Descending -> posts |> List.sortByDescending (fun p -> p.dateCreated)
       