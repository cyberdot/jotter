namespace Jotter.Core

open Document
open System
open System.Collections.Generic


module Store =
    
    let addPosts (postsStore: Post list) (posts: Post list) = postsStore @ posts
        
    let addPages (pagesStore: Page list) (pages: Page list) = pagesStore @ pages    
    