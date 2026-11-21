namespace Jotter.Core

open Jotter.Core.Config
open Jotter.Core.Document

module ViewModels =
    type IndexTemplateModel =
        { current: int
          prev: int
          prevFileName: string
          next: int
          nextFileName: string
          content: Post list
          config: ConfigurationModel }

    type TagIndexListTemplateModel =
        { content: string array }

    type TagIndexLayoutModel =
        { content: string
          config: ConfigurationModel
          css: string
          js: string
          path: string
          filename: string }

    let createIndexModel (pageNum: int) (config: ConfigurationModel) (isLastPage: bool) (posts: Post list) =
        let (prev, prevFileName) = IndexPager.previousPage pageNum config
        let (next, nextFileName) = IndexPager.nextPage pageNum isLastPage config
        { current = pageNum
          prev = prev
          prevFileName = prevFileName
          next = next
          nextFileName = nextFileName
          content = posts
          config = config }

