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

type ValueType with
    
    member this.HasUnbound = 
        let rec hasUnbound (ty : ValueType) = 
            match ty with
            | Reference(IdentRef Unbound) -> true
            | Reference(_) -> true // TODO: Handle correctly
            | Primitive _ | Literal _ | Nil | Dynamic | Value -> false
            | FunctionIntersection items | Union items -> List.exists hasUnbound items
            | Table(tbl, ops) -> List.exists fieldUnbound tbl || Array.exists hasUnbound ops
            | Function(args, ret) -> tupleUnbound args || tupleUnbound ret
        
        and fieldUnbound (pair : TableField) = hasUnbound pair.Key || hasUnbound pair.Value
        
        and tupleUnbound ((items, remainder) : TupleType) = 
            List.exists hasUnbound items || match remainder with
                                            | None -> false
                                            | Some x -> hasUnbound x
        hasUnbound this
    
    member this.IsUnbound = 
        match this with
        | Reference(IdentRef Unbound) -> true
        | _ -> false

type OperatorException(message : string) = 
    inherit Exception(message)

let private extractFirst (ty : TupleType) = 
    match ty with
    | (ty :: _, _) -> ty
    | (_, Some ty) -> ty
    | _ -> raise (Exception(sprintf "Cannot extract return from %A" ty))

let private extractReturn (ty : ValueType) = 
    match ty with
    | Function(args, ret) -> extractFirst ret
    | _ -> raise (Exception(sprintf "Cannot extract function from %A" ty))

let InferType (scope : TypeScope) (value : IValue) = 
    let known, result = scope.Known.TryGetValue value
    if known && not result.IsUnbound then Some result
    else 
        match value with
        | :? Upvalue | :? Argument | :? Constant -> Some(scope.Get value)
        | :? Instruction as insn -> 
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
                    | Function(args, ret) -> Some(extractFirst ret)
                    | _ -> 
                        raise 
                            (OperatorException
                                 (sprintf "No known operator %A for %A (got %A)" insn.Opcode ty operatorApply))
            | BinaryOp insn -> 
                let tyLeft = scope.Get insn.Left
                let tyRight = scope.Get insn.Right
                if tyLeft.HasUnbound || tyRight.HasUnbound then None
                else 
                    let operator = insn.Opcode.AsOperator
                    let operatorApply = scope.Checker.GetBinaryOperatory tyLeft tyRight operator
                    match operatorApply with
                    | Nil -> 
                        raise (OperatorException(sprintf "No known operator %A for %A and %A" operator tyLeft tyRight))
                    | Function(args, ret) -> Some(extractFirst ret)
                    | FunctionIntersection _ -> 
                        match scope.Checker.FindBestFunction operatorApply ([ tyLeft; tyRight ], None) with
                        | Some func, _ -> Some(extractReturn func)
                        | None, [] -> 
                            raise 
                                (OperatorException(sprintf "No known operator %A for %A and %A" operator tyLeft tyRight))
                        | None, bests -> Some(scope.Checker.MakeUnion(List.map extractReturn bests))
                    | _ -> 
                        raise 
                            (OperatorException
                                 (sprintf "No known operator %A for %A and %A (got %A)" insn.Opcode tyLeft tyRight 
                                      operatorApply))
            | TupleNew _ | TupleGet _ -> None
            | _ -> raise (ArgumentException(sprintf "Cannot handle %A" insn))
        | _ -> raise (ArgumentException(sprintf "Cannot handle %A" value))

let InferTypes(scope : TypeScope) = 
    let visitBlock (block : Block) = 
        for phi in block.PhiNodes do
            let items = Seq.map scope.Get phi.Source.Values |> Seq.toList
            scope.Set phi (scope.Checker.MakeUnion items)
        for item in block do
            match item with
            | :? ValueInstruction as insn -> 
                try 
                    match InferType scope insn with
                    | Some ty -> 
                        printfn "%A <- %A (originally %A)" insn ty (scope.Get insn)
                        scope.Set insn ty
                    | None -> printfn "Got none for %A" insn
                with :? OperatorException as e -> 
                    scope.Function.Module.Reporter.Report(ReportLevel.Error, e.Message, insn.Position)
            | _ -> ()
    for block in scope.Function.EntryPoint.VisitPreorder() do
        visitBlock block