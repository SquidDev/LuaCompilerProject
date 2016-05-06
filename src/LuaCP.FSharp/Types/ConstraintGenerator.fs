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
        scope.EquateValueWith insn (Set.of2 (scope.Get insn.Success) (scope.Get insn.Failure) |> Union)
    | Return insn -> scope.TupleSubtype insn.Values (scope.ReturnGet insn.Block.Function)
    | TableGet insn -> 
        scope.ValueSubtype insn.Table (Table(Set.singleton { Key = scope.Get insn.Key
                                                             Value = scope.Get insn
                                                             ReadOnly = true }, OperatorHelpers.Empty))
    | TableSet insn -> 
        scope.ValueSubtype insn.Table (Table(Set.singleton { Key = scope.Get insn.Key
                                                             Value = scope.Get insn.Value
                                                             ReadOnly = false }, OperatorHelpers.Empty))
    | TableNew insn -> 
        let keys = 
            Seq.map (fun (x : KeyValuePair<_, _>) -> 
                { Key = scope.Get x.Key
                  Value = scope.Get x.Value
                  ReadOnly = false }) insn.HashPart
            |> Set.ofSeq
        // TODO: Handle array part
        scope.ValueSupertype (Table(keys, OperatorHelpers.Empty)) insn
    | Call insn -> scope.ValueSubtype insn.Method (Function(scope.TupleGet insn.Arguments, scope.TupleGet insn))
    | TupleNew insn when insn.Remaining.IsNil() -> 
        scope.EquateTupleWith insn (Single(Seq.map scope.Get insn.Values |> Seq.toList, None))
    | TupleNew insn when insn.Values.Count = 0 -> scope.EquateTupleWith insn (scope.TupleGet insn.Remaining)
    | ReferenceGet insn -> scope.EquateValues insn.Reference insn
    | ReferenceSet insn -> scope.ValueAssign insn.Value insn.Reference
    | ReferenceNew insn -> scope.ValueAssign insn.Value insn
    | ClosureNew insn -> 
        Seq.iteri (fun i (x : IValue) -> scope.EquateValues x (insn.Function.OpenUpvalues.[i])) insn.OpenUpvalues
        Seq.iteri (fun i (x : IValue) -> scope.ValueAssign x insn.Function.ClosedUpvalues.[i]) insn.ClosedUpvalues
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
            | ValueKind.Value -> 
                scope.EquateValueWith phi (Seq.map scope.Get phi.Source.Values
                                           |> Set.ofSeq
                                           |> Union)
            | ValueKind.Tuple -> () // TODO: Handle this
            | ValueKind.Reference -> failwith "Cannot have reference in phi node"
            | kind -> failwith (sprintf "Unknown mode %A in phi node" kind)
        for item in block do
            InferType scope item
