namespace LuaCP.Types

open System
open System.Collections.Generic
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
    let valueMin = new Dictionary<ValueType, ValueType>() // Current constraint on a type
    let tupleMin = new Dictionary<TupleType, TupleType>() // Current constraint on a type
    let values = new Dictionary<IValue, ValueType>()
    let tuples = new Dictionary<IValue, TupleType>()
    let returns = new Dictionary<Function, TupleType>()
    let constraints = new HashSet<Constraint<ValueType, TupleType>>()
    let checker = new TypeProvider()
    let equator = new TypeEquator(checker)
    member this.Checker = checker
    
    member this.Get(value : IValue) = 
        if value.Kind = ValueKind.Tuple then raise (ArgumentException "Expected value, got reference")
        if value :? Constant then ValueType.Literal (value :?> Constant).Literal
        else 
            let exists, ty = values.TryGetValue(value)
            if exists then ty
            else 
                let ty = Reference(new IdentRef<_>(Unbound))
                values.Add(value, ty)
                ty
    
    member this.TryGet(value : IValue) = 
        if value.Kind = ValueKind.Tuple then raise (ArgumentException "Expected value, got reference")
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
            let ty = TReference(IdentRef<_>(Unbound))
            returns.Add(func, ty)
            ty
    
    member this.TupleGet(value : IValue) = 
        if value.IsNil() then Single([], None)
        elif value.Kind <> ValueKind.Tuple then Single([ this.Get value ], None)
        else 
            let exists, ty = tuples.TryGetValue value
            if exists then ty
            else 
                let ty = TReference(IdentRef<_>(Unbound))
                tuples.Add(value, ty)
                ty
    
    member this.TryTupleGet(value : IValue) = 
        if value.IsNil() then Some(Single([], None))
        elif value.Kind <> ValueKind.Tuple then Some(Single([ this.Get value ], None))
        else 
            let exists, ty = tuples.TryGetValue value
            if exists then Some ty
            else None
    
    member this.EquateValueTypes (a : ValueType) (b : ValueType) = 
        let replace a b = 
            let exists, min = valueMin.TryGetValue b
            if exists then this.Subtype a min
            match a with
            | Reference(IdentRef(Link child) as childRef) -> childRef.Value <- Link b
            | Reference(IdentRef(Unbound) as childRef) -> childRef.Value <- Link b
            | _ -> raise (ArgumentException(sprintf "Cannot replace %A with %A" a b))
        match a.Root, b.Root with
        | Reference(IdentRef(Unbound)), (Reference(IdentRef(Unbound)) as ty) -> replace a ty
        | Reference(IdentRef(Unbound)), ty -> replace a ty
        | ty, Reference(IdentRef(Unbound)) -> replace b ty
        | a, b -> 
            if not (checker.IsTypeEqual a b) then printfn "Not equal %A and %A" a b
    
    member this.Subtype (current : ValueType) (target : ValueType) = 
        let success, cons = valueMin.TryGetValue current
        if success then equator.Value target cons
        else valueMin.Add(current, target)
    
    member this.EquateValues (a : IValue) (b : IValue) = 
        match this.TryGet a, this.TryGet b with
        | None, None -> values.[a] <- this.Get b
        | None, Some ty -> values.Add(a, ty)
        | Some ty, None -> values.Add(b, ty)
        | Some a, Some b -> this.EquateValueTypes a b
    
    member this.EquateTupleTypes (a : TupleType) (b : TupleType) = 
        let replace a b = 
            let exists, min = tupleMin.TryGetValue b
            if exists then this.TupleSubtype a min
            match a with
            | TReference(IdentRef(Link child) as childRef) -> childRef.Value <- Link b
            | TReference(IdentRef(Unbound) as childRef) -> childRef.Value <- Link b
            | _ -> raise (ArgumentException(sprintf "Cannot replace %A with %A" a b))
        match a.Root, b.Root with
        | TReference(IdentRef(Unbound)), (TReference(IdentRef(Unbound)) as ty) -> replace a ty
        | TReference(IdentRef(Unbound)), ty -> replace a ty
        | ty, TReference(IdentRef(Unbound)) -> replace b ty
        | a, b -> 
            if not (checker.IsTupleEqual a b) then printfn "Not equal %A and %A" a b
    
    member this.EquateValuesWith (a : IValue) (b : ValueType) = 
        match this.TryGet a with
        | None -> values.Add(a, b)
        | Some a -> this.EquateValueTypes a b
    
    member this.EquateTuplesWith (a : IValue) (b : TupleType) = 
        match this.TryTupleGet a with
        | None -> tuples.Add(a, b)
        | Some a -> this.EquateTupleTypes a b
    
    member this.TupleSubtype (current : TupleType) (target : TupleType) = 
        let success, cons = tupleMin.TryGetValue current
        if success then equator.Tuple target cons
        else tupleMin.Add(current, target)
    
    member this.DumpFunction(num : NodeNumberer) = 
        printfn "# Function: %A" num.Function
        printfn "## Values:"
        let valueItem (pair : KeyValuePair<IValue, ValueType>) = 
            Formatter.Default.Value(pair.Key, Console.Out, num)
            printfn " : %s" (pair.Value.WithLabel())
            printfn "    %A" 
                (Seq.filter (fun (x : Constraint<_, _>) -> x.ContainsValue pair.Value) constraints |> Seq.toList)
            printfn "    %A" 
                (Seq.filter (fun (x : KeyValuePair<_, _>) -> x.Key = pair.Value || x.Value = pair.Value) valueMin 
                 |> Seq.toList)
        
        let tupleItem (pair : KeyValuePair<IValue, TupleType>) = 
            Formatter.Default.Value(pair.Key, Console.Out, num)
            printfn " : %s" (pair.Value.WithLabel())
            printfn "    %A" 
                (Seq.filter (fun (x : KeyValuePair<_, _>) -> x.Key = pair.Value || x.Value = pair.Value) tupleMin 
                 |> Seq.toList)
        
        for pair in values do
            match pair.Key with
            | :? IBelongs<Block> as b when b.Owner.Function = num.Function -> valueItem pair
            | :? IBelongs<Function> as b when b.Owner = num.Function -> valueItem pair
            | _ -> ()
        for pair in tuples do
            match pair.Key with
            | :? IBelongs<Block> as b when b.Owner.Function = num.Function -> tupleItem pair
            | :? IBelongs<Function> as b when b.Owner = num.Function -> tupleItem pair
            | _ -> ()
        printfn "## Returns: %A" (this.ReturnGet num.Function)
    
    member this.DumpConstraints() = 
        printfn "# Constraints:"
        for cons in constraints do
            printfn "%A" cons
    
    interface IScope with
        member this.CreateChild() = upcast this
        member this.CreateFunctionChild(func : Function) = upcast this
