namespace LuaCP.Types

open System
open System.Collections.Generic
open LuaCP.Types
open LuaCP.IR
open LuaCP.IR.Components
open LuaCP.Tree

type TypeManager() = 
    let functions = new Dictionary<Function, TypeScope>()
    member this.Create(func : Function, parent : Function option) = 
        let exists, scope = functions.TryGetValue(func)
        if exists then scope
        else 
            let scope = 
                new TypeScope(match parent with
                              | Some x -> Some functions.[x]
                              | None -> None)
            functions.Add(func, scope)
            scope

and TypeScope(parent : TypeScope option) = 
    // If this is the root node
    let root = parent.IsNone
    let values = new Dictionary<IValue, ValueType>()
    
    member this.Get(value : IValue) = 
        let exists, ty = values.TryGetValue(value)
        if exists then ty
        else 
            let ty = Reference(new IdentRef<VariableType>(Unbound))
            values.Add(value, ty)
            ty
    
    member this.Set(value : IValue, ty : ValueType) = 
        let exists, old = values.TryGetValue(value)
        if exists then 
            match old with
            | Reference ref -> ref.Value <- Link ty
            | _ -> raise (ArgumentException(sprintf "%A Already has type %A" value old))
        else values.Add(value, ty)
    
    member this.Create(value : IValue) = this.Get value |> ignore
    interface IScope with
        member this.CreateChild() = upcast new TypeScope(Some this)
        member this.CreateFunctionChild(func : Function) = upcast new TypeScope(None)