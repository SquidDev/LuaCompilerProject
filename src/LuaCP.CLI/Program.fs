open LuaCP.Parser
open System
open LuaCP
open LuaCP.IR.Components
open LuaCP.Passes
open LuaCP.Lua.Tree
open System.Text
open LuaCP.CodeGen
open LuaCP.Types
open FParsec

let ReadFile (file : string) (language : Language) = 
    use reader = new IO.StreamReader(file)
    let contents = reader.ReadToEnd()
    match CharParsers.run language.Script contents with
    | Failure(messageA, _, _) -> 
        Console.Error.WriteLine("Cannot compile " + file + "\n" + messageA)
        None
    | Success(tree, _, _) -> 
        let modu = new Module()
        let builder = new FunctionBuilder(modu)
        builder.Accept(tree) |> ignore
        PassManager.Run(modu, PassExtensions.Default, true)
        Some(modu, builder)

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
        | Some(modu, _) -> 
            use writer = new IO.StreamWriter(IO.Path.GetFileNameWithoutExtension(file) + ".out.lua")
            (new Lua.FunctionCodeGen(modu.EntryPoint, new IndentedTextWriter(writer))).Write()
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
