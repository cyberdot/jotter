namespace Jotter.Core

open Jotter.Core.Config
open Jotter.Core.Document

module Posts =
    
    let sortByDate (posts: Post list) =
        let data = Config.data()
        let result = match data.sortPosts with
                       | "ascending" -> posts |> List.sortBy (fun p -> p.dateCreated)
                       | "descending" -> posts |> List.sortByDescending (fun p -> p.dateCreated)
                       | _ -> posts
        result
       