// Learn more about F# at http://docs.microsoft.com/dotnet/fsharp

open System
open System.CommandLine


[<EntryPoint>]
let main argv =

    let root = RootCommand("Jotter static site generator")

    let initCmd = Command("init", "Create the scaffolding for the static site")
    let themeArg = Argument<string>("theme")
    themeArg.Description <- "Blog theme"

    initCmd.Add(themeArg)
    initCmd.SetAction(Action<ParseResult>(fun pr -> Jotter.Core.Site.init (pr.GetValue(themeArg))))
    root.Add(initCmd)


    let buildCmd = Command("build", "Compile the site to the root weblog directory")
    buildCmd.SetAction(Action<ParseResult>(fun _ -> Jotter.Core.BuildTask.run()))
    root.Add(buildCmd)

    let pageCmd = Command("page", "Create new page")
    let pageTitleArg = Argument<string>("title")
    pageTitleArg.Description <- "Page title"

    pageCmd.Add(pageTitleArg)
    pageCmd.SetAction(Action<ParseResult>(fun pr -> Jotter.Core.PageTask.run (pr.GetValue(pageTitleArg))))
    root.Add(pageCmd)

    let postCmd = Command("post", "Create new post")
    let postTitleArg = Argument<string>("title")
    postTitleArg.Description <- "Post title"
    postCmd.Add(postTitleArg)
    postCmd.SetAction(Action<ParseResult>(fun pr -> Jotter.Core.PostTask.run (pr.GetValue(postTitleArg))))
    root.Add(postCmd)

    let draftCmd = Command("draft", "Create new draft")
    let draftTitleArg = Argument<string>("title")
    draftTitleArg.Description <- "Post title"

    draftCmd.Add(draftTitleArg)
    draftCmd.SetAction(Action<ParseResult>(fun pr -> Jotter.Core.DraftTask.run (pr.GetValue(draftTitleArg))))
    root.Add(draftCmd)

    let serverCmd = Command("server", "Start a local server to view the blog")
    serverCmd.SetAction(Action<ParseResult>(fun _ -> Jotter.Core.ServerTask.run()))
    root.Add(serverCmd)

    root.Parse(argv).Invoke()