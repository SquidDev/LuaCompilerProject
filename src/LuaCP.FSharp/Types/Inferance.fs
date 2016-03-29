module LuaCP.Types.Infer

open System
open LuaCP.Graph
open LuaCP.Reporting
open LuaCP.IR
open LuaCP.IR.Components
open LuaCP.IR.Instructions
open LuaCP.IR.Instructions.Matching
open LuaCP.Types
open LuaCP.Collections.Matching
open LuaCP.Types.Extensions
open LuaCP.Types.OperatorExtensions
open LuaCP.Types.TypeFactory

type OperatorException(message : string) = 
    inherit Exception(message)

let rec private mapTypes (scope : TypeScope) (items : IValue list) (builder : ValueType list) = 
    // TODO: Optimise so we don't have to convert to list
    match items with
    | [] -> Some builder
    | value :: remainder -> 
        match scope.TryGet value with
        | None -> None
        | Some ty when ty.IsUnbound -> None
        | Some ty -> mapTypes scope remainder (ty :: builder)

let InferType (scope : TypeScope) (insn : ValueInstruction) = 
    let known, result = scope.ValueTypes.TryGetValue insn
    if known && not result.IsUnbound then None
    else 
        match insn with
        | ReferenceGet ref -> Some(scope.Get ref.Reference)
        | ReferenceNew ref -> Some(scope.Get ref.Value)
        | UnaryOp insn -> 
            let ty = scope.Get insn.Operand
            if ty.HasUnbound then None
            else 
                let operator = insn.Opcode.AsOperator
                let operatorApply = scope.Checker.GetOperator ty operator
                match operatorApply with
                | Nil -> raise (OperatorException(sprintf "No known operator %A for %A" operator ty))
                | Function(args, ret) -> Some(ret.First)
                | _ -> 
                    raise 
                        (OperatorException(sprintf "No known operator %A for %A (got %A)" insn.Opcode ty operatorApply))
        | BinaryOp insn -> 
            let tyLeft = scope.Get insn.Left
            let tyRight = scope.Get insn.Right
            if tyLeft.HasUnbound || tyRight.HasUnbound then None
            else 
                let operator = insn.Opcode.AsOperator
                let operatorApply = scope.Checker.GetBinaryOperatory tyLeft tyRight operator
                match operatorApply with
                | Nil -> raise (OperatorException(sprintf "No known operator %A for %A and %A" operator tyLeft tyRight))
                | Function(args, ret) -> Some(ret.First)
                | FunctionIntersection bests -> 
                    Some(scope.Checker.MakeUnion(List.map (fun (x : ValueType) -> x.Return) bests))
                | _ -> 
                    raise 
                        (OperatorException
                             (sprintf "No known operator %A for %A and %A (got %A)" insn.Opcode tyLeft tyRight 
                                  operatorApply))
        | ClosureNew insn -> 
            let func = insn.Function
            
            let mapped = 
                mapTypes scope (Seq.filter (fun (x : Argument) -> x.Kind = ValueKind.Value) insn.Function.Arguments
                                |> Seq.map (fun x -> x :> IValue)
                                |> Seq.toList) []
            match mapped with
            | Some x -> Some(Function(Single(x, None), TupleType.Empty))
            | None -> None
        | _ -> raise (ArgumentException(sprintf "Cannot handle %A" insn))

let InferTuple (scope : TypeScope) (insn : ValueInstruction) = 
    if insn.Kind <> ValueKind.Tuple then 
        match InferType scope insn with
        | Some x -> Some(Single([ x ], None))
        | None -> None
    else 
        match insn with
        | TupleNew insn -> 
            match mapTypes scope (Seq.toList insn.Values) [] with
            | Some x -> 
                if insn.Remaining.IsNil() then Some(Single(x, None))
                else 
                    match scope.TryTupleGet insn.Remaining with
                    | None -> None
                    | Some(ty) -> 
                        let result, remainder = ty.Root
                        Some(Single(List.append x result, remainder))
            | None -> None
        | Call insn -> 
            match scope.TryGet insn.Method, scope.TryTupleGet insn.Arguments with
            | Some func, Some result -> 
                match scope.Checker.FindBestFunction func result with
                | Some(Function(args, ret)), _ -> Some ret
                | Some(x), _ -> raise (InvalidOperationException(sprintf "Unexpected function type %A" x))
                | None, [] -> 
                    raise (OperatorException(sprintf "No known function for %A (with arguments %A)" func result))
                | None, items -> 
                    raise 
                        (OperatorException
                             (sprintf "Multiple overloads for %A (with arguments %A): %A" func result items))
            | _ -> None
        | _ -> raise (ArgumentException(sprintf "Cannot handle %A" insn))

let InferTypes(scope : TypeScope) = 
    let visitBlock (block : Block) = 
        for phi in block.PhiNodes do
            match mapTypes scope (Seq.toList phi.Source.Values) [] with
            | None -> ()
            | Some items -> scope.Set phi (scope.Checker.MakeUnion items)
        for item in block do
            match item with
            | :? ValueInstruction as insn -> 
                try 
                    if insn.Kind = ValueKind.Tuple then 
                        match InferTuple scope insn with
                        | Some ty -> scope.TupleSet insn ty
                        | None -> ()
                    else 
                        match InferType scope insn with
                        | Some ty -> scope.Set insn ty
                        | None -> ()
                with :? OperatorException as e -> 
                    scope.Function.Module.Reporter.Report(ReportLevel.Error, e.Message, insn.Position)
            | _ -> ()
    for block in scope.Function.EntryPoint.VisitPreorder() do
        visitBlock block