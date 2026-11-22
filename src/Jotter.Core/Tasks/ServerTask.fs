namespace Jotter.Core

open System.IO
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Hosting
open Microsoft.Extensions.FileProviders
open Microsoft.Extensions.Hosting

module ServerTask =
    let run (paths: SitePaths) (urls: string) =
        if not (Directory.Exists(paths.publicDirectory)) then
            raise (BuildException $"Public directory not found: {paths.publicDirectory}. Run 'build' first.")

        let configure (app: IApplicationBuilder) =
            let provider = new PhysicalFileProvider(paths.publicDirectory)
            let options = FileServerOptions()
            options.FileProvider <- provider
            options.EnableDefaultFiles <- true
            options.EnableDirectoryBrowsing <- false
            app.UseFileServer(options) |> ignore

        Host.CreateDefaultBuilder()
            .ConfigureWebHostDefaults(fun webHost ->
                webHost
                    .UseUrls(if System.String.IsNullOrWhiteSpace(urls) then "http://localhost:5000" else urls)
                    .Configure(configure)
                    |> ignore)
            .Build()
            .Run()
