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

    let themeCmd = Command("theme", "List, validate, copy, and create themes")

    let themeListCmd = Command("list", "List bundled themes")
    themeListCmd.SetAction(Func<ParseResult, int>(fun _ ->
        execute(fun () ->
            Jotter.Core.Theme.loadCatalog paths
            |> List.iter (fun theme -> Console.WriteLine($"{theme.id}\t{theme.name}\t{theme.description}")))))
    themeCmd.Add(themeListCmd)

    let themeValidateCmd = Command("validate", "Validate bundled and project-local themes")
    let validateThemeArg = Argument<string>("theme")
    validateThemeArg.Arity <- ArgumentArity.ZeroOrOne
    themeValidateCmd.Add(validateThemeArg)
    themeValidateCmd.SetAction(Func<ParseResult, int>(fun pr ->
        execute(fun () ->
            let id = pr.GetValue(validateThemeArg)
            Jotter.Core.Theme.validate paths (if String.IsNullOrWhiteSpace(id) then None else Some id)
            match id with
            | null -> Console.WriteLine("All available themes are valid.")
            | value -> Console.WriteLine($"Theme '{value}' is valid."))))
    themeCmd.Add(themeValidateCmd)

    let themeCopyCmd = Command("copy", "Copy a bundled theme into this site's workspace")
    let copyThemeArg = Argument<string>("theme")
    themeCopyCmd.Add(copyThemeArg)
    themeCopyCmd.SetAction(Func<ParseResult, int>(fun pr ->
        execute(fun () -> Jotter.Core.Theme.copyToProject paths (pr.GetValue(copyThemeArg)))))
    themeCmd.Add(themeCopyCmd)

    let themeNewCmd = Command("new", "Create a new project-local theme")
    let newThemeArg = Argument<string>("theme")
    themeNewCmd.Add(newThemeArg)
    themeNewCmd.SetAction(Func<ParseResult, int>(fun pr ->
        execute(fun () ->
            let id = pr.GetValue(newThemeArg)
            Jotter.Core.Theme.createProjectTheme paths id
            let createdPath = Jotter.Core.SitePaths.inWorkspace paths (Path.Combine("themes", id))
            Console.WriteLine($"Created theme at {createdPath}"))))
    themeCmd.Add(themeNewCmd)

    let themeUseCmd = Command("use", "Select an installed project-local theme")
    let useThemeArg = Argument<string>("theme")
    themeUseCmd.Add(useThemeArg)
    themeUseCmd.SetAction(Func<ParseResult, int>(fun pr ->
        execute(fun () ->
            let id = pr.GetValue(useThemeArg)
            Jotter.Core.Theme.useProjectTheme paths id
            Console.WriteLine($"Theme '{id}' selected."))))
    themeCmd.Add(themeUseCmd)

    let themeUpdateCmd = Command("update", "Refresh the catalog or one cached theme")
    let updateThemeArg = Argument<string>("theme")
    updateThemeArg.Arity <- ArgumentArity.ZeroOrOne
    themeUpdateCmd.Add(updateThemeArg)
    themeUpdateCmd.SetAction(Func<ParseResult, int>(fun pr ->
        execute(fun () ->
            let id = pr.GetValue(updateThemeArg)
            let requested = if String.IsNullOrWhiteSpace(id) then None else Some id
            Jotter.Core.Theme.refresh paths requested
            match requested with
            | Some value -> Console.WriteLine($"Catalog and theme '{value}' refreshed.")
            | None -> Console.WriteLine("Theme catalog refreshed."))))
    themeCmd.Add(themeUpdateCmd)

    root.Add(themeCmd)

    root.Parse(argv).Invoke()