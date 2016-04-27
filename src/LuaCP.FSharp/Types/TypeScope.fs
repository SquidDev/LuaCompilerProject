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

type TypeConstraint<'t>(ty : 't) = 
    let subtypes = new HashSet<'t>()
    let subtypeOf = new HashSet<'t>()
    let equal = new HashSet<'t>()
    member this.Type = ty
    member this.Subtypes = subtypes
    member this.SubtypeOf = subtypeOf
    member this.Equal = equal
    member this.Dump() = 
        let writer = new IndentedTextWriter(Console.Out)
        writer.WriteLine ty
        writer.Indent <- writer.Indent + 1
        for ty in subtypes do
            writer.WriteLine(">: {0}", ty)
        for ty in subtypeOf do
            writer.WriteLine("<: {0}", ty)
        // TODO: Remove this
        for ty in equal do
            writer.WriteLine("= {0}", ty)
        writer.Indent <- writer.Indent - 1

type TypeScope() = 
    let values = new Dictionary<IValue, TypeConstraint<ValueType>>()
    let tuples = new Dictionary<IValue, TypeConstraint<TupleType>>()
    let returns = new Dictionary<Function, TupleType>()
    let constraints = new HashSet<Constraint<ValueType, TupleType>>()
    let checker = new TypeProvider()
    let equator = new TypeEquator()
    member this.Checker = checker
    
    member this.Get(value : IValue) = 
        if value.Kind = ValueKind.Tuple then raise (ArgumentException "Expected value, got reference")
        if value :? Constant then ValueType.Literal (value :?> Constant).Literal
        else 
            let exists, ty = values.TryGetValue(value)
            if exists then ty.Type
            else 
                let ty = Reference(new IdentRef<_>(Unbound))
                values.Add(value, new TypeConstraint<_>(ty))
                ty
    
    member this.GetConstraint(value : IValue) = 
        if value.Kind = ValueKind.Tuple then raise (ArgumentException "Expected value, got tuple")
        if value :? Constant then raise (ArgumentException "Expected value, got constant")
        else 
            let exists, ty = values.TryGetValue(value)
            if exists then ty
            else 
                let ty = new TypeConstraint<_>(Reference(new IdentRef<_>(Unbound)))
                values.Add(value, ty)
                ty
    
    member this.TryGet(value : IValue) = 
        if value.Kind = ValueKind.Tuple then raise (ArgumentException "Expected value, got reference")
        if value :? Constant then Some(ValueType.Literal (value :?> Constant).Literal)
        else 
            let exists, ty = values.TryGetValue(value)
            if exists then Some(ty.Type)
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
            if exists then ty.Type
            else 
                let ty = TReference(IdentRef<_>(Unbound))
                tuples.Add(value, new TypeConstraint<_>(ty))
                ty
    
    member this.GetTupleConstraint(value : IValue) = 
        if value.IsNil() then raise (ArgumentException "Expected tuple, got nil")
        elif value.Kind <> ValueKind.Tuple then raise (ArgumentException "Expected tuple, got value")
        else 
            let exists, ty = tuples.TryGetValue(value)
            if exists then ty
            else 
                let ty = new TypeConstraint<_>(TReference(new IdentRef<_>(Unbound)))
                tuples.Add(value, ty)
                ty
    
    member this.EquateTupleWith (value : IValue) (ty : TupleType) = 
        let success, cons = tuples.TryGetValue value
        if success then cons.Equal.Add ty |> ignore
        else tuples.Add(value, new TypeConstraint<_>(ty))
    
    member this.EquateValueWith (value : IValue) (ty : ValueType) = 
        let success, cons = values.TryGetValue value
        if success then cons.Equal.Add ty |> ignore
        else values.Add(value, new TypeConstraint<_>(ty))
    
    member this.EquateValues (left : IValue) (right : IValue) = 
        match values.TryGetValue left, values.TryGetValue right with
        | (true, l), (true, r) -> 
            l.Equal.Add r.Type |> ignore
            r.Equal.Add l.Type |> ignore
        | (false, _), (true, r) -> values.Add(left, r)
        | (true, l), (false, _) -> values.Add(right, l)
        | (false, _), (false, _) -> 
            let cons = new TypeConstraint<_>(Reference(new IdentRef<_>(Unbound)))
            values.Add(left, cons)
            values.Add(right, cons)
    
    member this.ValueSubtype (value : IValue) (target : ValueType) = 
        (this.GetConstraint value).Subtypes.Add target |> ignore
    member this.ValueSubtypeOf (ty : ValueType) (target : IValue) = 
        (this.GetConstraint target).SubtypeOf.Add ty |> ignore
    member this.TupleSubtype (value : IValue) (target : TupleType) = 
        (this.GetTupleConstraint value).Subtypes.Add target |> ignore
    
    member this.DumpFunction(num : NodeNumberer) = 
        printfn "# Function: %A" num.Function
        printfn "## Values:"
        let belongs (value : KeyValuePair<IValue, _>) = 
            match value.Key with
            | :? IBelongs<Block> as b when b.Owner.Function = num.Function -> true
            | :? IBelongs<Function> as b when b.Owner = num.Function -> true
            | _ -> false
        for cons in Seq.filter belongs values do
            printf "%s : " (Formatter.Default.Choose(cons.Key, num))
            cons.Value.Dump()
            for item in Seq.filter (fun (x : Constraint<_, _>) -> x.ContainsValue cons.Value.Type) constraints do
                printfn "    %A" item
        for cons in Seq.filter belongs tuples do
            printf "%s : " (Formatter.Default.Choose(cons.Key, num))
            cons.Value.Dump()
        printfn "## Returns: %A" (this.ReturnGet num.Function)
    
    interface IScope with
        member this.CreateChild() = upcast this
        member this.CreateFunctionChild(func : Function) = upcast this
