module LuaCP.Types.TypeFactory

open System
open System.Collections.Generic
open LuaCP.Collections
open LuaCP.Types

type TypeProvider with
    
    member this.Union(ty : ValueType list) = 
        let add (ty : ValueType) (types : ValueType list) = 
            let filtered = List.filter (fun x -> not (this.IsSubtype x ty)) types
            if List.exists (this.IsSubtype ty) filtered then filtered
            else ty :: filtered
        
        let rec collect (types : ValueType list) (builder : ValueType list) = 
            match types with
            | [] -> builder
            | ty :: remainder -> 
                match ty with
                | Union child -> collect remainder (collect child builder)
                | _ -> collect remainder (add ty builder)
        
        match collect ty [] with
        | [] -> raise (InvalidOperationException "Empty union")
        | [ item ] -> item
        | items -> Union items
    
    member this.Constrain (ty : ValueType) (constrain : ValueType) = 
        let rec filter (ty : ValueType) (constrain : ValueType) = 
            let (|Basic|_|) (ty : ValueType) = 
                match ty with
                | Nil | Primitive _ | Literal _ | Value -> Some()
                | _ -> None
            if this.IsSubtype ty constrain then Some ty
            else 
                match ty with
                | Dynamic -> Some Dynamic // Should be caught above
                | Value | Nil | Primitive _ | Literal _ -> None // Again: should be caught above
                | Union items -> 
                    match List.filter (fun x -> this.IsSubtype x constrain) items with
                    | [] -> None
                    | items -> Some(this.Union items)
                | Function(_, _) -> None
                | FunctionIntersection _ -> None
                | Table(_, _) -> None
                | Reference ref -> 
                    match ref.Value with
                    | Unbound -> raise (InvalidOperationException "Constraining on unbound type")
                    | Link dest -> filter dest constrain
        filter ty constrain