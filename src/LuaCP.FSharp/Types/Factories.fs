module LuaCP.Types.TypeFactory

open System
open System.Collections.Generic
open LuaCP.Collections
open LuaCP.Types

type RelationshipChecker with
    member this.MakeUnion(ty : ValueType list) = 
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