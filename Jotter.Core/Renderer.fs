namespace Jotter.Core

open System
open System.Reflection
open RazorLight



module Renderer =
  
    let render (content: string) (vm: obj) (renderer: string) =
        let templateKey = Guid.NewGuid().ToString()
        let assembly = Assembly.GetExecutingAssembly()
        let vmType = typeof<Jotter.Core.Document.ILayoutViewModel>
        let engine = RazorLightEngineBuilder()
                         .SetOperatingAssembly(assembly)
                         .UseEmbeddedResourcesProject(vmType.GetType())
                         .UseMemoryCachingProvider()
                         .Build()
        let result = engine.CompileRenderStringAsync(templateKey, content, vm) |> Async.AwaitTask |> Async.RunSynchronously
        result
        
        
