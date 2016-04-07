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
    | ValueSubtype of current : 'tv * target : 'tv
    | WithBinOp of Operator * left : 'tv * right : 'tv * result : 'tv
    | WithUnOp of Operator * operand : 'tv * result : 'tv
    | TupleSubtype of current : 'tt * target : 'tt
    
    override this.ToString() = 
        match this with
        | ValueSubtype(current, target) -> sprintf "%A :> %A" current target
        | TupleSubtype(current, target) -> sprintf "%A :> %A" current target
        | WithUnOp(op, operand, result) -> sprintf "%A %A :> %A" op operand result
        | WithBinOp(op, left, right, result) -> sprintf "%A(%A %A) :> %A" op left right result
    
    member this.AsString = this.ToString()
    
    member this.ContainsValue ty = 
        match this with
        | ValueSubtype(a, b) | WithUnOp(_, a, b) -> a = ty || b = ty
        | TupleSubtype(_, _) -> false
        | WithBinOp(_, a, b, c) -> a = ty || b = ty || c = ty
    
    member this.ContainsTuple ty = 
        match this with
        | ValueSubtype(_, _) | WithUnOp(_, _, _) | WithBinOp(_, _, _, _) -> false
        | TupleSubtype(a, b)  -> a = ty || b = ty

type TypeScope() = 
    let values = new Dictionary<IValue, ValueType>()
    let tuples = new Dictionary<IValue, TupleType>()
    let returns = new Dictionary<Function, TupleType>()
    let constraints = new HashSet<Constraint<ValueType, TupleType>>()
    let checker = new TypeProvider()
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
        | ValueSubtype(current, target) -> this.Constraint(ValueSubtype(this.Get current, this.Get target))
        | WithUnOp(opcode, operand, result) -> this.Constraint(WithUnOp(opcode, this.Get operand, this.Get result))
        | WithBinOp(opcode, left, right, result) -> 
            this.Constraint(WithBinOp(opcode, this.Get left, this.Get right, this.Get result))
        | TupleSubtype(current, target) -> this.Constraint(TupleSubtype(this.TupleGet current, this.TupleGet target))
    
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
    
    member this.EquateValues (a : IValue) (b : IValue) = 
        match this.TryGet a, this.TryGet b with
        | None, None -> values.[a] <- this.Get b
        | None, Some ty -> 
            values.Add(a, ty)
            (System.Diagnostics.Debug.Assert(ty != null))
        | Some ty, None -> 
            values.Add(b, ty)
            (System.Diagnostics.Debug.Assert(ty != null))
        | Some a, Some b -> this.EquateValueTypes a b
    
    member this.EquateTupleTypes (a : TupleType) (b : TupleType) = 
        let replace a b = 
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
        | None -> 
            values.Add(a, b)
            (System.Diagnostics.Debug.Assert(b != null))
        | Some a -> this.EquateValueTypes a b
    
    member this.EquateTuplesWith (a : IValue) (b : TupleType) = 
        match this.TryTupleGet a with
        | None -> tuples.Add(a, b)
        | Some a -> this.EquateTupleTypes a b
    
    member this.DumpFunction(num : NodeNumberer) = 
        printfn "# Function: %A" num.Function
        printfn "## Values:"
        let valueItem (pair : KeyValuePair<IValue, ValueType>) = 
            Formatter.Default.Value(pair.Key, Console.Out, num)
            printfn " : %s => %A" (pair.Value.WithLabel()) 
                (Seq.filter (fun (x : Constraint<_, _>) -> x.ContainsValue pair.Value) constraints |> Seq.toList)
        
        let tupleItem (pair : KeyValuePair<IValue, TupleType>) = 
            Formatter.Default.Value(pair.Key, Console.Out, num)
            printfn " : %s => %A" (pair.Value.WithLabel()) 
                (Seq.filter (fun (x : Constraint<_, _>) -> x.ContainsTuple pair.Value) constraints |> Seq.toList)
        
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
