namespace LuaCP.Types

open System
open System.Collections.Generic
open LuaCP.Types
open LuaCP.IR
open LuaCP.IR.Components
open LuaCP.Tree

type TypeManager() = 
    let functions = new Dictionary<Function, TypeScope>()
    member this.Create(func : Function) = 
        let exists, scope = functions.TryGetValue(func)
        if exists then scope
        else 
            let scope = new TypeScope(this, func)
            functions.Add(func, scope)
            scope

and TypeScope(manager : TypeManager, func : Function) = 
    let values = new Dictionary<IValue, ValueType>()
    member this.Manager = manager
    member this.Function = func
    
    member this.Get(value : IValue) = 
        if value :? Constant then ValueType.Literal (value :?> Constant).Literal
        else 
            let exists, ty = values.TryGetValue(value)
            if exists then ty
            else 
                let ty = Reference(new IdentRef<VariableType>(Unbound))
                values.Add(value, ty)
                ty
    
    member this.Set(value : IValue, ty : ValueType) = 
        if value :? Constant then raise (ArgumentException "Cannot set type of constant")
        else 
            let exists, old = values.TryGetValue(value)
            if exists then 
                match old with
                | Reference ref -> ref.Value <- Link ty
                | _ -> raise (ArgumentException(sprintf "%A Already has type %A" value old))
            else values.Add(value, ty)
    
    member this.Create(value : IValue) = this.Get value |> ignore
    member this.Known : IReadOnlyDictionary<IValue, ValueType> = upcast values
    interface IScope with
        member this.CreateChild() = upcast this
        member this.CreateFunctionChild(func : Function) = upcast manager.Create func