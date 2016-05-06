open LuaCP.Parser
open System
open LuaCP
open LuaCP.IR.Instructions
open LuaCP.IR.Components
open LuaCP.Passes
open LuaCP.Lua.Tree
open System.Text
open LuaCP.CodeGen
open LuaCP.Types
open FParsec

type Path = IO.Path

let ReadFile (file : string) (language : Language) = 
    use reader = new IO.StreamReader(file)
    let contents = reader.ReadToEnd()
    match CharParsers.run language.Script contents with
    | Failure(messageA, _, _) -> 
        Console.Error.WriteLine("Cannot compile " + file + "\n" + messageA)
        None
    | Success(tree, _, _) -> Some(CLI.Commands.Build tree)

[<EntryPoint>]
let main argv = 
    let language = new Language()
    language.Get(fun x -> new Expression(x)) |> ignore
    language.Get(fun x -> new Statement(x)) |> ignore
    language.Get(fun x -> new Extensions.Adt(x)) |> ignore
    language.Get(fun x -> new Extensions.Lambda(x)) |> ignore
    language.Get(fun x -> new Extensions.OpEquals(x)) |> ignore
    language.Get(fun x -> new Lua.Parser.Extensions.Types(x)) |> ignore
    match argv with
    | [| file |] -> 
        match ReadFile file language with
        | None -> 1
        | Some(modu, builder) -> 
            let path = Path.Combine(Path.GetDirectoryName file, Path.GetFileNameWithoutExtension file) + ".out.lua"
            use writer = new IO.StreamWriter(path)
            CLI.Commands.Write modu builder writer
            0
    | [| file; command |] -> 
        match ReadFile file language with
        | None -> 1
        | Some(modu, builder) -> 
            CLI.Commands.RunCommand command modu builder
            0
    | [||] -> 
        CLI.REPL.Loop language
        0
    | _ -> 
        Console.Error.WriteLine("LuaCP.CLI [filename] [command]")
        1
