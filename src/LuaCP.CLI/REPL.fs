module LuaCP.CLI.REPL

open System
open LuaCP.Debug
open LuaCP.IR.Components
open LuaCP.Lua.Tree
open LuaCP.Parser
open LuaCP.Passes
open LuaCP.Tree
open LuaCP.Types
open FParsec

let rec Parse (language : Language) str : INode option =
    if String.IsNullOrWhiteSpace str then None
    else if str.StartsWith "=" then Parse language ("return " + str.Substring(1))
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

let Loop(language : Language) =
    let mutable modu = null
    let mutable builder : FunctionBuilder = null
    while true do
        printf "> "
        let line = Console.ReadLine()
        if line = null then ()
        elif line.StartsWith("!") then
            if modu = null then Console.WriteLine("No module")
            else Commands.RunCommand (line.Substring 1) modu builder
        else
            match Parse language line with
            | None -> ()
            | Some(item) ->
                let builtModule, builtBuilder = Commands.Build item
                modu <- builtModule
                builder <- builtBuilder
                (new Exporter(Console.Out)).ModuleLong(modu)
