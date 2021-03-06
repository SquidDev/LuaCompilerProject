namespace LuaCP.Types

open System
open System.Collections.Generic
open LuaCP
open LuaCP.Types
open LuaCP.IR
open LuaCP.Debug
open LuaCP.Collections
open LuaCP.Collections.Matching
open LuaCP.IR.Components
open LuaCP.IR.User
open LuaCP.Tree
open LuaCP.Types.TypeFactory
open LuaCP.Types.Extensions

[<StructuredFormatDisplay("{AsString}")>]
type Constraint<'tv, 'tt when 'tv : equality and 'tt : equality> = 
    | WithBinOp of Operator * left : 'tv * right : 'tv * result : 'tv
    | WithUnOp of Operator * operand : 'tv * result : 'tv
    
    override this.ToString() = 
        match this with
        | WithUnOp(op, operand, result) -> sprintf "%A %A :> %A" op operand result
        | WithBinOp(op, left, right, result) -> sprintf "%A(%A %A) :> %A" op left right result
    
    member this.AsString = this.ToString()
    member this.ContainsValue ty = 
        match this with
        | WithUnOp(_, a, b) -> a = ty || b = ty
        | WithBinOp(_, a, b, c) -> a = ty || b = ty || c = ty

type TypeScope() = 
    let values = new Dictionary<IValue, ValueType>()
    let tuples = new Dictionary<IValue, TupleType>()
    let returns = new Dictionary<Function, TupleType>()
    let constraints = new HashSet<Constraint<ValueType, TupleType>>()
    let equator = new TypeMerger()
    
    member this.Get(value : IValue) = 
        if value.Kind = ValueKind.Tuple then raise (ArgumentException "Expected value, got tuple")
        if value :? Constant then ValueType.Literal (value :?> Constant).Literal
        else 
            let exists, ty = values.TryGetValue(value)
            if exists then ty
            else 
                let ty = equator.ValueNew().Type
                values.Add(value, ty)
                ty
    
    member this.TryGet(value : IValue) = 
        if value.Kind = ValueKind.Tuple then raise (ArgumentException "Expected value, got tuple")
        if value :? Constant then Some(ValueType.Literal (value :?> Constant).Literal)
        else 
            let exists, ty = values.TryGetValue(value)
            if exists then Some(ty)
            else None
    
    member this.Create(value : IValue) = this.Get value |> ignore
    member this.Constraint cons = constraints.Add cons |> ignore
    
    member this.VConstraint cons = 
        match cons with
        | WithUnOp(opcode, operand, result) -> this.Constraint(WithUnOp(opcode, this.Get operand, this.Get result))
        | WithBinOp(opcode, left, right, result) -> 
            this.Constraint(WithBinOp(opcode, this.Get left, this.Get right, this.Get result))
    
    member this.ReturnGet(func : Function) = 
        let exists, ty = returns.TryGetValue func
        if exists then ty
        else 
            let ty = equator.TupleNew()
            returns.Add(func, ty.Type)
            ty.Type
    
    member this.TupleGet(value : IValue) = 
        if value.IsNil() then Single([], None)
        elif value.Kind <> ValueKind.Tuple then Single([ this.Get value ], None)
        else 
            let exists, ty = tuples.TryGetValue value
            if exists then ty
            else 
                let ty = equator.TupleNew().Type
                tuples.Add(value, ty)
                ty
    
    member this.EquateTupleWith (value : IValue) (ty : TupleType) = 
        let success, cons = tuples.TryGetValue value
        match success, cons with
        | true, TReference ref -> (equator.TupleGet ref).Equal.Add ty |> ignore
        | true, _ -> raise (ArgumentException(sprintf "%A is not a reference" ty))
        | false, _ -> tuples.Add(value, ty)
    
    member this.EquateValueWith (value : IValue) (ty : ValueType) = 
        let success, cons = values.TryGetValue value
        match success, cons with
        | true, Reference ref -> (equator.ValueGet ref).Equal.Add ty |> ignore
        | true, _ -> raise (ArgumentException(sprintf "%A is not a reference" ty))
        | false, _ -> values.Add(value, ty)
    
    member this.EquateValues (left : IValue) (right : IValue) = 
        match values.TryGetValue left, values.TryGetValue right with
        | (true, l), (true, r) -> 
            // TODO: Equating types
            printfn "TODO: Equating %A and %A" l r
        | (false, _), (true, r) -> values.Add(left, r)
        | (true, l), (false, _) -> values.Add(right, l)
        | (false, _), (false, _) -> 
            let cons = equator.ValueNew().Type
            values.Add(left, cons)
            values.Add(right, cons)
    
    member this.ValueSubtype (value : IValue) (target : ValueType) = equator.MergeValues (this.Get value) target
    member this.ValueSupertype (ty : ValueType) (target : IValue) = equator.MergeValues ty (this.Get target)
    member this.ValueAssign (current : IValue) (target : IValue) = 
        equator.MergeValues (this.Get current) (this.Get target)
    member this.TupleSubtype (value : IValue) (target : TupleType) = equator.MergeTuples (this.TupleGet value) target
    member this.TupleSupertype (value : TupleType) (target : IValue) = equator.MergeTuples value (this.TupleGet target)
    member this.TupleAssign (current : IValue) (target : IValue) = 
        equator.MergeTuples (this.TupleGet current) (this.TupleGet target)
    member this.Bake() = equator.Bake()
    
    member this.Flatten() = 
        for key in (System.Linq.Enumerable.ToList values.Keys) do
            values.[key] <- values.[key].Flattened
        for key in (System.Linq.Enumerable.ToList tuples.Keys) do
            tuples.[key] <- tuples.[key].Flattened
    
    member this.DumpFunction(num : NodeNumberer) = 
        printfn "# Function: %A" num.Function
        printfn "  Values:"
        let belongs (value : KeyValuePair<IValue, _>) = 
            match value.Key with
            | :? IBelongs<Block> as b when b.Owner.Function = num.Function -> true
            | :? IBelongs<Function> as b when b.Owner = num.Function -> true
            | _ -> false
        for cons in Seq.filter belongs values do
            printfn "%s : %s" (Formatter.Default.Choose(cons.Key, num)) (cons.Value.WithLabel())
            match cons.Value with
            | Reference(IdentRef(Unbound) as tRef) -> printfn "%A" (equator.ValueGet tRef)
            | _ -> ()
            for item in Seq.filter (fun (x : Constraint<_, _>) -> x.ContainsValue cons.Value) constraints do
                printfn "    %A" item
        for cons in Seq.filter belongs tuples do
            printfn "%s : %s" (Formatter.Default.Choose(cons.Key, num)) (cons.Value.WithLabel())
            match cons.Value with
            | TReference(IdentRef(Unbound) as tRef) -> printfn "%A" (equator.TupleGet tRef)
            | _ -> ()
        printfn "  Returns: %s" ((this.ReturnGet num.Function).WithLabel())
        match this.ReturnGet num.Function with
        | TReference(IdentRef(Unbound) as tRef) -> printfn "%A" (equator.TupleGet tRef)
        | _ -> ()
    
    interface IScope with
        member this.CreateChild() = upcast this
        member this.CreateFunctionChild(func : Function) = upcast this
