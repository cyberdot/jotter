namespace Jotter.Core

open System
open System.IO
open Newtonsoft.Json.Linq
open Jotter.Core.Config

module IndexPager =
    
    let lastPage pages =
        let len = pages |> Seq.length
        len = 0
        
    let withIndexNum (page: string) (num: int) =
        if page = null && num = 1 then (1, "index.html")
        elif page = null && num > 1 then (num, $"index{num}.html")
        elif page <> null && num = 1 then (1, page)
        else
            let rootPath = Path.ChangeExtension(page, null)
            let ext = Path.GetExtension(page)
            (num, $"{rootPath}{num}.{ext}")
    
    let totalPages (postsCount: int) =
        let config = Config.data
        let postsPerPage = config.postsPerPage
        let result: float = Math.Ceiling(float(postsCount) / float(postsPerPage))
        Math.Truncate(result)
        
    let previousPage (pageNum: int) (config: ConfigurationModel) =
        if config = null then config = Config.data
        else config = ConfigurationModel()
        
        if pageNum = 1 then (0, String.Empty)
        else
            let blogIndex = config.blogIndex
            withIndexNum blogIndex (pageNum - 1)
            
    
    let nextPage (pageNum: int) (isLast: bool) (config: ConfigurationModel) =
        if config = null then config = Config.data
        else config = ConfigurationModel()       
        
        if isLast = true then (0, String.Empty)
        else
            let blogIndex = config.blogIndex
            withIndexNum blogIndex (pageNum + 1)
            
            
   
            
    
            

