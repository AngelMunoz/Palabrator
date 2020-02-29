#r "paket: groupref build //"
#load "./.fake/build.fsx/intellisense.fsx"

#if !FAKE
#r "netstandard"
#r "Facades/netstandard" // https://github.com/ionide/ionide-vscode-fsharp/issues/839#issuecomment-396296095
#endif

open System

open Fake.Core
open Fake.DotNet
open Fake.IO

Target.initEnvironment ()

let serverPath = Path.getFullName "./src/Server"
let deployDir = Path.getFullName "./deploy"

let platformTool tool winTool =
    let tool = if Environment.isUnix then tool else winTool
    match ProcessUtils.tryFindFileOnPath tool with
    | Some t -> t
    | _ ->
        let errorMsg =
            tool + " was not found in path. " +
            "Please install it and make sure it's available from your path. " +
            "See https://safe-stack.github.io/docs/quickstart/#install-pre-requisites for more info"
        failwith errorMsg

let runTool cmd args workingDir =
    let arguments = args |> String.split ' ' |> Arguments.OfArgs
    Command.RawCommand (cmd, arguments)
    |> CreateProcess.fromCommand
    |> CreateProcess.withWorkingDirectory workingDir
    |> CreateProcess.ensureExitCode
    |> Proc.run
    |> ignore

let runDotNet cmd workingDir =
    let result =
        DotNet.exec (DotNet.Options.withWorkingDirectory workingDir) cmd ""
    if result.ExitCode <> 0 then failwithf "'dotnet %s' failed in %s" cmd workingDir

let runToolWithOutput cmd args workingDir =
    let arguments = args |> String.split ' ' |> Arguments.OfArgs
    let result =
        Command.RawCommand (cmd, arguments)
        |> CreateProcess.fromCommand
        |> CreateProcess.withWorkingDirectory workingDir
        |> CreateProcess.ensureExitCode
        |> CreateProcess.redirectOutput
        |> Proc.run
    result.Result.Output |> (fun s -> s.TrimEnd())

Target.create "Clean" (fun _ ->
    Shell.cleanDir deployDir
)

Target.create "Build" (fun _ ->
    runDotNet "build" serverPath
)

Target.create "Run" (fun _ ->
    runDotNet "watch run" serverPath
)



Target.create "Configure" (fun args ->
    let gitTool = platformTool "git" "git.exe"
    let herokuTool = platformTool "heroku" "heroku.cmd"
    let arguments =  ("apps:create"::args.Context.Arguments) |> String.concat " "
    let output = runToolWithOutput herokuTool arguments __SOURCE_DIRECTORY__
    let app = (output.Split '|').[0]
    printfn "app created in %s" (app.Trim())
    let appName = app.[8..(app.IndexOf(".")-1)]
    runTool gitTool "init" __SOURCE_DIRECTORY__
    let gitCmd = sprintf "git:remote --app %s" appName
    runTool herokuTool gitCmd __SOURCE_DIRECTORY__
    runTool herokuTool "buildpacks:set -i 1 https://github.com/SAFE-Stack/SAFE-buildpack" __SOURCE_DIRECTORY__
    runTool gitTool "add ." __SOURCE_DIRECTORY__
    runTool gitTool "commit -m initial" __SOURCE_DIRECTORY__
)

Target.create "Bundle" (fun _ ->
    let serverDir = Path.combine deployDir "Server"
    let publishArgs = sprintf "publish -c Release -o \"%s\" --runtime linux-x64" serverDir
    runDotNet publishArgs serverPath
    let procFile = "web: cd ./deploy/Server/ && ./Server"
    File.writeNew "Procfile" [procFile]
)

Target.create "Deploy" (fun _ ->
    let gitTool = platformTool "git" "git.exe"
    runTool gitTool "push heroku master" __SOURCE_DIRECTORY__
    let herokuTool = platformTool "heroku" "heroku.cmd"
    runTool herokuTool "open" __SOURCE_DIRECTORY__
)
open Fake.Core.TargetOperators

"Clean"
    ==> "Build"
    ==> "Bundle"


"Clean"
    ==> "Run"

Target.runOrDefaultWithArguments "Bundle"
