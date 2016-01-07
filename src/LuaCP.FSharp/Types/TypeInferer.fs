module LuaCP.Types.Inferer

open System.Collections.Generic
open LuaCP.IR

type TypeManager() = 
    let id = ref 0
    
    let new_var (level : int) (isDynamic : bool) = 
        let current = !id
        id := current + 1
        Var(ref (Unbound(current, level, isDynamic)))
    
    member this.Instantate (dynamic : bool) (level : int) (ty : ValueType) = 
        let idMap = Dictionary<int, ValueType>()
        
        let rec value (ty : ValueType) = 
            match ty with
            | Literal _ | Primitive _ | Nil | Value -> ty
            | Var { contents = Link ty } -> value ty
            | Var { contents = Generic id } -> 
                let found, var = idMap.TryGetValue(id)
                if found then var
                else 
                    let var = new_var level false
                    idMap.Add(id, var)
                    var
            | Var { contents = Unbound _ } -> ty
            | Dynamic -> 
                if dynamic then new_var level true
                else Dynamic
            | Apply(ty, argList) -> Apply(value ty, List.map value argList)
            | Union types -> Union(List.map value types)
            | Function(args, ret) -> Function(tuple args, tuple ret)
        
        and tuple ((ty, var) : TupleType) = 
            List.map value ty, 
            if var.IsSome then Some(value var.Value)
            else None
        
        value ty