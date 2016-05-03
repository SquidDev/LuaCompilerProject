module LuaCP.CLI.Commands

open System
open System.Diagnostics
open System.Text
open LuaCP
open LuaCP.CodeGen
open LuaCP.Debug
open LuaCP.IR.Components
open LuaCP.IR.Instructions
open LuaCP.Lua.Tree
open LuaCP.Passes
open LuaCP.Passes.Analysis
open LuaCP.Tree
open LuaCP.Types
open LuaCP.Types.Extensions

let Build(tree : INode) = 
    let modu = new Module()
    let builder = new FunctionBuilder(modu)
    builder.Accept(tree) |> ignore
    let types = builder.EntryPoint.Scopes.Get<TypeScope>()
    let variables = builder.EntryPoint.Scopes.Get<IVariableScope>()
    types.EquateValueWith variables.Globals StandardLibraries.Base
    for func in modu.Functions do
        ConstraintGenerator.InferTypes types func
    // try
    // PassManager.Run(modu, PassExtensions.Default, true)
    // with :? VerificationException as e -> printfn "Cannot verify: %A" e
    modu, builder

let Write (modu : Module) (builder : FunctionBuilder) stream = 
    let types = builder.EntryPoint.Scopes.Get<TypeScope>()
    types.Bake()
    
    let decorator (insn : Instruction) = 
        match insn with
        | :? ValueInstruction as value when value.Kind = IR.ValueKind.Tuple -> (types.TupleGet value).Root.ToString()
        | :? ValueInstruction as value -> (types.Get value).Root.ToString()
        | _ -> null
    
    let writer = new IndentedTextWriter(stream)
    let gen = new Lua.FunctionCodeGen(modu.EntryPoint, writer, new Func<_, _>(decorator))
    gen.Write()

let RunCommand (command : string) (modu : Module) (builder : FunctionBuilder) = 
    match command with
    | "help" -> 
        Console.WriteLine("!help:   Print this help")
        Console.WriteLine("!dump:   Dump the previous source")
        Console.WriteLine("!graph:  Plot the CFG of the previous source")
        Console.WriteLine("!lasm:   Dump LASM code of the module")
        Console.WriteLine("!lua:    Dump Lua code of the module")
        Console.WriteLine("!types:  Dump the types and constraints of all values")
        Console.WriteLine("!bake:   Make as many types as possible concrete")
        Console.WriteLine("!branch: Dump a simplified branching model of the code")
    | "dump" -> (new Exporter(Console.Out)).ModuleLong(modu)
    | "graph" -> DotExporter.Write(modu)
    | "lua" -> Write modu builder Console.Out
    | "exec" -> 
        let startInfo = new ProcessStartInfo()
        startInfo.FileName <- "lua5.2"
        startInfo.Arguments <- "-"
        startInfo.UseShellExecute <- false
        startInfo.CreateNoWindow <- true
        startInfo.RedirectStandardInput <- true
        use proc = Process.Start(startInfo)
        using proc.StandardInput 
            (fun input -> (new Lua.FunctionCodeGen(modu.EntryPoint, new IndentedTextWriter(input))).Write())
        proc.WaitForExit()
    | "lasm" -> 
        let builder = new StringBuilder()
        use x = new Bytecode.LasmBytecodeWriter(builder, Bytecode.VarargType.Exists)
        (new Bytecode.BytecodeCodegen(x, modu.EntryPoint)).Write()
        Console.WriteLine(builder)
    | "types" -> 
        let scope = builder.EntryPoint.Scopes.Get<TypeScope>()
        for func in modu.Functions do
            let numberer = new NodeNumberer(func)
            scope.DumpFunction numberer
    | "bake" -> 
        let scope = builder.EntryPoint.Scopes.Get<TypeScope>()
        scope.Bake()
        for func in modu.Functions do
            let numberer = new NodeNumberer(func)
            scope.DumpFunction numberer
    | "branch" -> 
        let group = (new Analysis.BranchAnalysis(modu.EntryPoint)).EntryPoint
        let writer = new IndentedTextWriter(Console.Out)
        let num = new NodeNumberer(modu.EntryPoint)
        group.Dump(num, writer)
    | line -> Console.WriteLine("Unknown command " + line)
