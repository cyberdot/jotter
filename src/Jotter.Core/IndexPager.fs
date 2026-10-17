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
            
            
   
            
    
            

