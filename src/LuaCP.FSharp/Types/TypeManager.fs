namespace LuaCP.Types

open System
open System.Collections.Generic
open LuaCP.Types
open LuaCP.IR
open LuaCP.IR.Components
open LuaCP.Tree
open LuaCP.Types.TypeFactory
open LuaCP.Types.Extensions

type TypeManager() = 
    let functions = new Dictionary<Function, TypeScope>()
    let checker = new TypeProvider()
    
    member this.Create(func : Function) = 
        let exists, scope = functions.TryGetValue(func)
        if exists then scope
        else 
            let scope = new TypeScope(this, func)
            functions.Add(func, scope)
            scope
    
    member this.Checker = checker

and TypeScope(manager : TypeManager, func : Function) = 
    let values = new Dictionary<IValue, ValueType>()
    let tuples = new Dictionary<IValue, TupleType>()
    member this.Manager = manager
    member this.Function = func
    member this.Checker = manager.Checker
    
    member this.Get(value : IValue) = 
        if value.Kind = ValueKind.Tuple then raise (ArgumentException "Expected value, got reference")
        if value :? Constant then ValueType.Literal (value :?> Constant).Literal
        else 
            let exists, ty = values.TryGetValue(value)
            if exists then ty
            else 
                let ty = Reference(new IdentRef<VariableType>(Unbound))
                values.Add(value, ty)
                ty
    
    member this.Set (value : IValue) (ty : ValueType) = 
        if value.Kind = ValueKind.Tuple then raise (ArgumentException "Expected value, got reference")
        if value :? Constant then raise (ArgumentException "Cannot set type of constant")
        else 
            let exists, old = values.TryGetValue(value)
            if exists then 
                match old with
                | Reference ref -> ref.Value <- Link ty
                | _ -> raise (ArgumentException(sprintf "%A Already has type %A" value old))
            else values.Add(value, ty)
    
    member this.TryGet(value : IValue) = 
        if value.Kind = ValueKind.Tuple then raise (ArgumentException "Expected value, got reference")
        if value :? Constant then Some(ValueType.Literal (value :?> Constant).Literal)
        else 
            let exists, ty = values.TryGetValue(value)
            if exists then Some ty
            else None
    
    member this.Create(value : IValue) = this.Get value |> ignore
    
    member this.TupleGet(value : IValue) = 
        if value.IsNil() then [], None
        elif value.Kind <> ValueKind.Tuple then [ this.Get value ], None
        else tuples.[value]
    
    member this.TryTupleGet(value : IValue) = 
        if value.IsNil() then Some([], None)
        elif value.Kind <> ValueKind.Tuple then 
            match this.TryGet value with
            | None -> None
            | Some ty when ty.IsUnbound -> None
            | Some ty -> Some([ ty ], None)
        else 
            let exists, result = tuples.TryGetValue value
            if exists then Some result
            else None
    
    member this.TupleSet (value : IValue) (ty : TupleType) = tuples.[value] <- ty
    member this.ValueTypes : IReadOnlyDictionary<IValue, ValueType> = upcast values
    member this.TupleTypes : IReadOnlyDictionary<IValue, TupleType> = upcast tuples
    
    member this.Simplify() = 
        let modify = new List<KeyValuePair<IValue, ValueType>>()
        for pair in values do
            match pair.Value with
            | Union items -> modify.Add(new KeyValuePair<_, _>(pair.Key, this.Checker.MakeUnion items))
            | _ -> ()
        for pair in modify do
            values.[pair.Key] <- pair.Value
    
    interface IScope with
        member this.CreateChild() = upcast this
        member this.CreateFunctionChild(func : Function) = upcast manager.Create func