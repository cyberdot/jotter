namespace Jotter.Core

open System
open RazorEngine
open RazorEngine.Templating


module Renderer =
    
    let render (content: string) (vm: obj) (renderer: string) =
        let templateKey = Guid.NewGuid.ToString()
        Engine.Razor.RunCompile(content, templateKey, null, vm)
