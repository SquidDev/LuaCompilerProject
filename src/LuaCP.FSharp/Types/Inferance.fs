module LuaCP.Types.Infer

open System
open LuaCP.Graph
open LuaCP.Reporting
open LuaCP.IR
open LuaCP.IR.Components
open LuaCP.IR.Instructions
open LuaCP.IR.Instructions.Matching
open LuaCP.Types
open LuaCP.Types.Matching
open LuaCP.Types.OperatorExtensions
open LuaCP.Types.TypeFactory
open LuaCP.Types.OperatorHandling

type ValueType with
    member this.IsUnbound = 
        match this with
        | Reference(IdentRef Unbound) -> true
        | _ -> false

type OperatorException(message : string) = 
    inherit Exception(message)

let extractFirst (ty : TupleType) = 
    match ty with
    | (ty :: _, _) -> ty
    | (_, Some ty) -> ty
    | _ -> raise (Exception(sprintf "Cannot extract return from %A" ty))

let extractReturn (ty : ValueType) = 
    match ty with
    | Function(args, ret) -> extractFirst ret
    | _ -> raise (Exception(sprintf "Cannot extract function from %A" ty))

let inferType (scope : TypeScope) (value : IValue) = 
    let known, result = scope.Known.TryGetValue value
    if known && not result.IsUnbound then result
    else 
        match value with
        | :? Upvalue | :? Argument | :? Constant -> scope.Get value
        | :? Instruction as insn -> 
            match insn with
            | ReferenceGet ref -> scope.Get ref.Reference
            | ReferenceNew ref -> scope.Get ref.Value
            | UnaryOp insn -> 
                let ty = scope.Get insn.Operand
                if ty.IsUnbound then scope.Get insn
                else 
                    let operator = insn.Opcode.AsOperator
                    let operatorApply = GetOperator ty operator
                    match operatorApply with
                    | Nil -> raise (OperatorException(sprintf "No known operator %A for %A" operator ty))
                    | Function(args, ret) -> extractFirst ret
                    | _ -> 
                        raise 
                            (OperatorException
                                 (sprintf "No known operator %A for %A (got %A)" insn.Opcode ty operatorApply))
            | BinaryOp insn -> 
                let tyLeft = scope.Get insn.Left
                let tyRight = scope.Get insn.Right
                if tyLeft.IsUnbound || tyRight.IsUnbound then scope.Get insn
                else 
                    let operator = insn.Opcode.AsOperator
                    let operatorApply = GetBinaryOperatory tyLeft tyRight operator
                    match operatorApply with
                    | Nil -> 
                        raise (OperatorException(sprintf "No known operator %A for %A and %A" operator tyLeft tyRight))
                    | Function(args, ret) -> extractFirst ret
                    | FunctionIntersection _ -> 
                        match scope.Checker.FindBestFunction operatorApply ([ tyLeft; tyRight ], None) with
                        | Some func, _ -> extractReturn func
                        | None, [] -> 
                            raise 
                                (OperatorException(sprintf "No known operator %A for %A and %A" operator tyLeft tyRight))
                        | None, bests -> scope.Checker.MakeUnion(List.map extractReturn bests)
                    | _ -> 
                        raise 
                            (OperatorException
                                 (sprintf "No known operator %A for %A and %A (got %A)" insn.Opcode tyLeft tyRight 
                                      operatorApply))
            | TupleNew _ | TupleGet _ -> Nil
            | _ -> raise (ArgumentException(sprintf "Cannot handle %A" insn))
        | _ -> raise (ArgumentException(sprintf "Cannot handle %A" value))

let inferTypes (scope : TypeScope) = 
    let visitBlock (block : Block) = 
        for phi in block.PhiNodes do
            let items = Seq.map scope.Get phi.Source.Values |> Seq.toList
            scope.Set phi (scope.Checker.MakeUnion items)
        for item in block do
            try 
                match item with
                | :? ValueInstruction as insn -> scope.Set insn (inferType scope insn)
                | _ -> ()
            with :? OperatorException as e -> scope.Function.Module.Reporter.Report(ReportLevel.Error, e.Message)
    for block in scope.Function.EntryPoint.VisitPreorder() do
        visitBlock block