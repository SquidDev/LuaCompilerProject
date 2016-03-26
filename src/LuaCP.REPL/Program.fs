open LuaCP.Parser
open System
open LuaCP.CodeGen.Lua
open LuaCP.Debug
open LuaCP.IR.Components
open LuaCP.Parser
open LuaCP.Passes
open LuaCP.Tree
open LuaCP.Lua.Tree
open LuaCP.CodeGen.Bytecode
open System.Text
open LuaCP.CodeGen
open LuaCP.Passes.Analysis
open LuaCP.Types
open FParsec

[<EntryPoint>]
let main argv = 
    let language = new Language()
    language.Get(fun x -> new Expression(x)) |> ignore
    language.Get(fun x -> new Statement(x)) |> ignore
    language.Get(fun x -> new Extensions.Adt(x)) |> ignore
    language.Get(fun x -> new Extensions.Lambda(x)) |> ignore
    language.Get(fun x -> new Extensions.OpEquals(x)) |> ignore
    language.Get(fun x -> new LuaCP.Lua.Parser.Extensions.Types(x)) |> ignore
    let rec parse str : INode option = 
        if String.IsNullOrWhiteSpace str then None
        else if str.StartsWith "=" then parse ("return " + str.Substring(1))
        else 
            let result = CharParsers.run language.Script str
            match result with
            | Success(item, _, _) -> Some(item)
            | Failure(messageA, stateA, _) -> 
                let expr = CharParsers.run (language.Source language.Expression) str
                match expr with
                | Success(item, _, _) -> Some(upcast item)
                | Failure(messageB, stateB, _) -> 
                    Console.WriteLine("Error {0}", 
                                      if stateA.Position.Index >= stateB.Position.Index then messageA
                                      else messageB)
                    None
    
    let mutable modu = null
    let mutable builder : FunctionBuilder = null
    while true do
        printf "> "
        let line = Console.ReadLine()
        if line.StartsWith("!") then 
            if modu = null then Console.WriteLine("No module")
            else 
                match line.Substring 1 with
                | "help" -> 
                    Console.WriteLine("!help:  Print this help")
                    Console.WriteLine("!dump:  Dump the previous source")
                    Console.WriteLine("!graph: Plot the CFG of the previous source")
                    Console.WriteLine("!lasm:  Dump LASM code of the module")
                | "dump" -> (new Exporter(Console.Out)).ModuleLong(modu)
                | "graph" -> DotExporter.Write(modu)
                | "code" -> (new FunctionCodegen(modu.EntryPoint, new IndentedTextWriter(Console.Out))).Write()
                | "lasm" -> 
                    let builder = new StringBuilder()
                    use x = new LasmBytecodeWriter(builder, VarargType.Exists)
                    (new BytecodeCodegen(x, modu.EntryPoint)).Write()
                    Console.WriteLine(builder)
                | "types" -> 
                    let manager = builder.EntryPoint.Scopes.Get<TypeScope>()
                    let numberer = new NodeNumberer(builder.Function)
                    for pair in manager.Known do
                        Formatter.Default.Value(pair.Key, Console.Out, numberer)
                        printfn " : %s" (pair.Value.WithLabel())
                | line -> Console.WriteLine("Unknown command " + line)
        else 
            match parse line with
            | None -> ()
            | Some(item) -> 
                modu <- new Module()
                builder <- new FunctionBuilder(modu)
                builder.Accept(item) |> ignore
                let scope = builder.EntryPoint.Scopes.Get<TypeScope>()
                Infer.InferTypes (scope)
                scope.Simplify()
                try 
                    ()
                // PassManager.Run(modu, PassExtensions.Default, true)
                with :? VerificationException as e -> printfn "Cannot verify: %A" e
                (new Exporter(Console.Out)).ModuleLong(modu)
    0
