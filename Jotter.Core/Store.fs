namespace Jotter.Core

open Document
open System
open System.Collections.Generic


module Store =
    
    let addPosts (postsStore: FrontMatter list) (posts: FrontMatter list) = postsStore @ posts
        
    let addPages (pagesStore: FrontMatter list) (pages: FrontMatter list) = pagesStore @ pages    
    