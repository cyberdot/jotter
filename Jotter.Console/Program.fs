// Learn more about F# at http://docs.microsoft.com/dotnet/fsharp

open System
open System.CommandLine
open System.CommandLine.Invocation


[<EntryPoint>]
let main argv =
    
    let root = RootCommand()
    
    let initCmd = Command("init", "Create the scaffolding for the static site")
    initCmd.Handler <- CommandHandler.Create(Action(fun _ -> Jotter.Core.Site.init()))
    root.AddCommand(initCmd)

    
    let buildCmd = Command("build", "Compile the site to the root weblog directory")
    buildCmd.Handler <- CommandHandler.Create(Action(fun _ -> Jotter.Core.BuildTask.run()))
    root.AddCommand(buildCmd)
    
    let pageCmd = Command("page", "Create new page")
    pageCmd.AddArgument(new Argument<string>("title", "Page title"))
    pageCmd.Handler <- CommandHandler.Create<string>(Action<string>(fun title -> Jotter.Core.PageTask.run title))
    root.AddCommand(pageCmd)
    
    let postCmd = Command("post", "Create new post")
    postCmd.AddArgument(new Argument<string>("title", "Post title"))
    postCmd.Handler <- CommandHandler.Create<string>(Action<string>(fun title -> Jotter.Core.PostTask.run title))
    root.AddCommand(postCmd)
      
    let draftCmd = Command("draft", "Create new draft")
    draftCmd.AddArgument(new Argument<string>("title", "Post title"))
    draftCmd.Handler <- CommandHandler.Create<string>(Action<string>(fun title -> Jotter.Core.DraftTask.run title))
    root.AddCommand(draftCmd)
    
    let serverCmd = Command("server", "Start a local server to view the blog")
    serverCmd.Handler <- CommandHandler.Create(Action(fun _ -> Jotter.Core.ServerTask.run()))
    root.AddCommand(serverCmd)
    
    let result = root.Invoke(argv)
    result