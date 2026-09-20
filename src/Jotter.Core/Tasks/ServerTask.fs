namespace Jotter.Core

open System.IO
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Hosting
open Microsoft.Extensions.Hosting
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.FileProviders
open Microsoft.AspNetCore.Http
open Giraffe

module ServerTask =
    let run () =
        
        let webApp =
            choose [
                route "/"       >=> htmlFile "index.html"
            ]
            
        let configureApp (app : IApplicationBuilder) =
                     let path = Config.publicDirectory.Substring(1).TrimStart('/').TrimEnd('/')
                     let currentDir = Directory.GetCurrentDirectory()
                     let dir = Path.Combine(currentDir, path)
                     
                     printfn "Current local dir %s" (Directory.GetCurrentDirectory())
                     printfn "Current dir: %s" dir
                     
                     let options = StaticFileOptions()
                     options.FileProvider <- new PhysicalFileProvider(dir)
                     options.RequestPath <- PathString("")                   
                   
                     let defaultFileOptions = DefaultFilesOptions()
                     defaultFileOptions.DefaultFileNames.Clear()
                     defaultFileOptions.FileProvider <- new PhysicalFileProvider(dir)
                     defaultFileOptions.DefaultFileNames.Add("index.html")
                     
                     app.UseDefaultFiles(defaultFileOptions) |> ignore
                     app.UseStaticFiles(options) |> ignore
                     app.UseGiraffe webApp
                     
        let configureServices (services : IServiceCollection) =
                    // Add Giraffe dependencies
                    services.AddGiraffe() |> ignore

     
        Host.CreateDefaultBuilder()
           .ConfigureWebHostDefaults(
            fun webHostBuilder ->
                webHostBuilder
                    .Configure(configureApp)
                    .ConfigureServices(configureServices)
                    |> ignore)
          .Build()
          .Run()
        ()
