module LuaCP.CLI.Commands

open System
open System.Text
open LuaCP
open LuaCP.CodeGen
open LuaCP.Debug
open LuaCP.IR.Components
open LuaCP.Lua.Tree
open LuaCP.Passes
open LuaCP.Tree
open LuaCP.Types

let Build(tree : INode) = 
    let modu = new Module()
    let builder = new FunctionBuilder(modu)
    builder.Accept(tree) |> ignore
    let types = builder.EntryPoint.Scopes.Get<TypeScope>()
    let variables = builder.EntryPoint.Scopes.Get<IVariableScope>()
    types.Constraint(ValueSubtype(types.Get(variables.Globals), StandardLibraries.Base))
    for func in modu.Functions do
        ConstraintGenerator.InferTypes types func
    // try 
    // PassManager.Run(modu, PassExtensions.Default, true)
    // with :? VerificationException as e -> printfn "Cannot verify: %A" e
    modu, builder

let RunCommand (command : string) (modu : Module) (builder : FunctionBuilder) = 
    match command with
    | "help" -> 
        Console.WriteLine("!help:   Print this help")
        Console.WriteLine("!dump:   Dump the previous source")
        Console.WriteLine("!graph:  Plot the CFG of the previous source")
        Console.WriteLine("!lasm:   Dump LASM code of the module")
        Console.WriteLine("!lua:    Dump Lua code of the module")
        Console.WriteLine("!types:  Dump the types and constraints of all values")
        Console.WriteLine("!branch: Dump a simplified branching model of the code")
    | "dump" -> (new Exporter(Console.Out)).ModuleLong(modu)
    | "graph" -> DotExporter.Write(modu)
    | "lua" -> (new Lua.FunctionCodeGen(modu.EntryPoint, new IndentedTextWriter(Console.Out))).Write()
    | "lasm" -> 
        let builder = new StringBuilder()
        use x = new Bytecode.LasmBytecodeWriter(builder, Bytecode.VarargType.Exists)
        (new Bytecode.BytecodeCodegen(x, modu.EntryPoint)).Write()
        Console.WriteLine(builder)
    | "types" -> 
        let scope = builder.EntryPoint.Scopes.Get<TypeScope>()
        for func in modu.Functions do
            let numberer = new NodeNumberer(builder.Function)
            scope.DumpFunction numberer
        scope.DumpConstraints()
    | "branch" -> 
        let group = (new Analysis.BranchAnalysis(modu.EntryPoint)).Group
        let writer = new IndentedTextWriter(Console.Out)
        let num = new NodeNumberer(modu.EntryPoint)
        group.Dump(num, writer)
    | line -> Console.WriteLine("Unknown command " + line)
