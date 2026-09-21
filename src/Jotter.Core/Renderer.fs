namespace Jotter.Core

open System
open System.Reflection
open System.Security.Cryptography
open System.Text
open RazorLight



module Renderer =

    // Built once and reused: RazorLightEngineBuilder().Build() is expensive, and a fresh engine
    // would also start with an empty template cache on every call.
    let private engine =
        let assembly = Assembly.GetExecutingAssembly()
        let vmType = typeof<Jotter.Core.Document.ILayoutViewModel>
        RazorLightEngineBuilder()
            .SetOperatingAssembly(assembly)
            .UseEmbeddedResourcesProject(vmType.GetType())
            .UseMemoryCachingProvider()
            .Build()

    // RazorLight's memory cache is keyed by this string, not by `content` - a stable hash of the
    // template lets identical templates (e.g. the same layout reused across every post) hit the
    // cache instead of being recompiled via Roslyn on every single render.
    let private templateKey (content: string) =
        content
        |> Encoding.UTF8.GetBytes
        |> SHA256.HashData
        |> Convert.ToHexString

    let render (content: string) (vm: obj) (renderer: string) =
        let key = templateKey content
        engine.CompileRenderStringAsync(key, content, vm) |> Async.AwaitTask |> Async.RunSynchronously

