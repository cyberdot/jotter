// Learn more about F# at http://docs.microsoft.com/dotnet/fsharp

open System
open System.IO
open System.CommandLine


[<EntryPoint>]
let main argv =
    let paths = Jotter.Core.SitePaths.fromRoot(Directory.GetCurrentDirectory())
    let execute (action: unit -> unit) =
        try
            action()
            0
        with
        | Jotter.Core.BuildException message ->
            Console.Error.WriteLine($"error: {message}")
            1
        | :? IOException as ex ->
            Console.Error.WriteLine($"error: {ex.Message}")
            1

    let root = RootCommand("Jotter static site generator")

    let initCmd = Command("init", "Create the scaffolding for the static site")
    let themeArg = Argument<string>("theme")
    themeArg.Description <- "Blog theme"
    let forceOption = Option<bool>("--force")
    forceOption.Description <- "Overwrite an existing site, deleting all posts, drafts and pages"

    initCmd.Add(themeArg)
    initCmd.Add(forceOption)
    initCmd.SetAction(Func<ParseResult, int>(fun pr ->
        execute(fun () -> Jotter.Core.Site.init paths (pr.GetValue(themeArg)) (pr.GetValue(forceOption)))))
    root.Add(initCmd)


    let buildCmd = Command("build", "Compile the site to the root weblog directory")
    buildCmd.SetAction(Func<ParseResult, int>(fun _ -> execute(fun () -> Jotter.Core.BuildTask.run paths)))
    root.Add(buildCmd)

    let pageCmd = Command("page", "Create new page")
    let pageTitleArg = Argument<string>("title")
    pageTitleArg.Description <- "Page title"

    pageCmd.Add(pageTitleArg)
    pageCmd.SetAction(Func<ParseResult, int>(fun pr ->
        execute(fun () -> Jotter.Core.PageTask.run paths (pr.GetValue(pageTitleArg)))))
    root.Add(pageCmd)

    let postCmd = Command("post", "Create new post")
    let postTitleArg = Argument<string>("title")
    postTitleArg.Description <- "Post title"
    postCmd.Add(postTitleArg)
    postCmd.SetAction(Func<ParseResult, int>(fun pr ->
        execute(fun () -> Jotter.Core.PostTask.run paths (pr.GetValue(postTitleArg)))))
    root.Add(postCmd)

    let draftCmd = Command("draft", "Create new draft")
    let draftTitleArg = Argument<string>("title")
    draftTitleArg.Description <- "Post title"

    draftCmd.Add(draftTitleArg)
    draftCmd.SetAction(Func<ParseResult, int>(fun pr ->
        execute(fun () -> Jotter.Core.DraftTask.run paths (pr.GetValue(draftTitleArg)))))
    root.Add(draftCmd)

    let publishCmd = Command("publish", "Move a draft into published posts")
    let publishTitleArg = Argument<string>("title")
    publishTitleArg.Description <- "Draft title"

    publishCmd.Add(publishTitleArg)
    publishCmd.SetAction(Func<ParseResult, int>(fun pr ->
        execute(fun () -> Jotter.Core.PublishTask.run paths (pr.GetValue(publishTitleArg)))))
    root.Add(publishCmd)

    let serverCmd = Command("server", "Start a local server to view the blog")
    let urlsOption = Option<string>("--urls")
    urlsOption.Description <- "Listening URL, for example http://localhost:8080"
    serverCmd.Add(urlsOption)
    serverCmd.SetAction(Func<ParseResult, int>(fun pr ->
        execute(fun () -> Jotter.Core.ServerTask.run paths (pr.GetValue(urlsOption)))))
    root.Add(serverCmd)

    root.Parse(argv).Invoke()