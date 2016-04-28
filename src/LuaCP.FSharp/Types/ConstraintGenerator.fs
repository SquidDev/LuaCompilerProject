module LuaCP.Types.ConstraintGenerator

open System
open System.Collections.Generic
open LuaCP.Collections
open LuaCP.Collections.Matching
open LuaCP.Graph
open LuaCP.Reporting
open LuaCP.IR
open LuaCP.IR.Components
open LuaCP.IR.Instructions
open LuaCP.IR.Instructions.Matching
open LuaCP.Types
open LuaCP.Types.Extensions
open LuaCP.Types.OperatorExtensions
open LuaCP.Types.TypeFactory

type OperatorException(message : string) = 
    inherit Exception(message)

let InferType (scope : TypeScope) (insn : Instruction) = 
    match insn with
    | BinaryOp insn -> scope.VConstraint(WithBinOp(insn.Opcode.AsOperator, insn.Left, insn.Right, upcast insn))
    | UnaryOp insn -> scope.VConstraint(WithUnOp(insn.Opcode.AsOperator, insn.Operand, upcast insn))
    | ValueCondition insn -> 
        scope.EquateValueWith insn (Union [ scope.Get insn.Success
                                            scope.Get insn.Failure ])
    | Return insn -> scope.TupleSubtype insn.Values (scope.ReturnGet insn.Block.Function)
    | TableGet insn -> 
        scope.ValueSubtype insn.Table (Table([ { Key = scope.Get insn.Key
                                                 Value = scope.Get insn
                                                 ReadOnly = true } ], OperatorHelpers.Empty))
    | TableSet insn -> 
        scope.ValueSubtype insn.Table (Table([ { Key = scope.Get insn.Key
                                                 Value = scope.Get insn.Value
                                                 ReadOnly = false } ], OperatorHelpers.Empty))
    | TableNew insn -> 
        let keys = 
            Seq.map (fun (x : KeyValuePair<_, _>) -> 
                { Key = scope.Get x.Key
                  Value = scope.Get x.Value
                  ReadOnly = false }) insn.HashPart
            |> Seq.toList
        // TODO: Handle array part
        scope.ValueSupertype (Table(keys, OperatorHelpers.Empty)) insn
    | Call insn -> scope.ValueSubtype insn.Method (Function(scope.TupleGet insn.Arguments, scope.TupleGet insn))
    | TupleNew insn when insn.Remaining.IsNil() -> 
        scope.EquateTupleWith insn (Single(Seq.map scope.Get insn.Values |> Seq.toList, None))
    | ReferenceGet insn -> scope.EquateValues insn.Reference insn
    | ReferenceSet insn -> scope.EquateValues insn.Value insn.Reference
    | ReferenceNew insn -> scope.EquateValues insn.Value insn
    | ClosureNew insn -> 
        Seq.iteri (fun i (x : IValue) -> scope.EquateValues x (insn.Function.OpenUpvalues.[i])) insn.OpenUpvalues
        Seq.iteri (fun i (x : IValue) -> scope.ValueSubtype x (scope.Get(insn.Function.ClosedUpvalues.[i]))) 
            insn.ClosedUpvalues
        let mapped = 
            List.map scope.Get (Seq.filter (fun (x : Argument) -> x.Kind = ValueKind.Value) insn.Function.Arguments
                                |> Seq.map (fun x -> x :> IValue)
                                |> Seq.toList)
        scope.EquateValueWith insn (Function(Single(mapped, None), scope.ReturnGet insn.Function))
    | Branch _ | BranchCondition _ -> ()
    | _ -> printfn "Cannot handle %A" insn

let InferTypes (scope : TypeScope) (func : Function) = 
    for block in func.EntryPoint.VisitPreorder() do
        for phi in block.PhiNodes do
            match phi.Kind with
            | ValueKind.Value -> scope.EquateValueWith phi (Union(Seq.map scope.Get phi.Source.Values |> Seq.toList))
            | ValueKind.Tuple -> () // TODO: Handle this
        for item in block do
            InferType scope item
