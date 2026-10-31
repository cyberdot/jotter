namespace Jotter.Core

open System
open System.IO
open Jotter.Core.Config

module IndexPager =
    
    let lastPage pages = (pages |> Seq.length) = 0
        
    let withIndexNum (page: string) (num: int) =
        if page = null && num = 1 then (1, "index.html")
        elif page = null && num > 1 then (num, $"index{num}.html")
        elif page <> null && num = 1 then (1, page)
        else
            let rootPath = Path.ChangeExtension(page, null)
            let ext = Path.GetExtension(page)
            (num, $"{rootPath}{num}{ext}")
    
    let previousPage (pageNum: int) (config: ConfigurationModel) =
        
        if pageNum = 1 then (0, String.Empty)
        else
            let blogIndex = config.blogIndex
            withIndexNum blogIndex (pageNum - 1)
            
    
    let nextPage (pageNum: int) (isLast: bool) (config: ConfigurationModel) =

        if isLast = true then (0, String.Empty)
        else
            let blogIndex = config.blogIndex
            withIndexNum blogIndex (pageNum + 1)

    // Index.fs and Tags.fs used to each carry their own copy of this page-splitting
    // recursion - identical except for what they did with each page's items. Keeping it in
    // one place means the postsPerPage <= 0 guard only has to exist (and be trusted) once.
    // An empty blog must still get a page 1 - the "/" route has nothing to serve otherwise -
    // but once past page 1, running out of items means we're done.
    let rec paginate (writePage: int -> bool -> 'a list -> unit) (itemsPerPage: int) (pageNum: int) (items: 'a list) =
        if items.Length = 0 && pageNum > 1 then ()
        else
            let pageItems = List.truncate itemsPerPage items
            let remainder =
                if itemsPerPage <= List.length items then List.skip itemsPerPage items
                else List.empty

            writePage pageNum (lastPage remainder) pageItems
            if items.Length > 0 then
                paginate writePage itemsPerPage (pageNum + 1) remainder
            
            
   
            
    
            

