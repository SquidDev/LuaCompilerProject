namespace LuaCP.Types

open System
open System.Collections.Generic
open LuaCP.IR

type TypeManager(storage : Dictionary<IValue, ValueType>) = 
    member this.Add(value : IValue, ty : ValueType) = storage.Add(value, ty)
