module LuaCP.Types.TypeFactory

open System
open System.Collections.Generic
open LuaCP.Collections
open LuaCP.Collections.Matching
open LuaCP.Types

type TypeProvider with

    member this.Union(ty : ValueType seq) =
        let add (ty : ValueType) (types : ValueType Set) =
            let filtered = Set.filter (fun x -> not (this.IsSubtype x ty)) types
            if Set.exists (this.IsSubtype ty) filtered then filtered
            else filtered.Add ty

        let rec collect (builder : ValueType Set)  (ty : ValueType)=
            match ty with
            | Union child -> Seq.fold collect builder child
            | _ -> add ty builder

        match Seq.fold collect Set.empty ty with
        | EmptySet -> raise (InvalidOperationException "Empty union")
        | SingleSet item -> item
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
                    match Set.filter (fun x -> this.IsSubtype x constrain) items with
                    | EmptySet -> None
                    | items -> Some(this.Union items)
                | Function(_, _) -> None
                | Intersection _ -> None
                | Table(_, _) -> None
                | Reference ref ->
                    match ref.Value with
                    | Unbound -> raise (InvalidOperationException "Constraining on unbound type")
                    | Link dest -> filter dest constrain
        filter ty constrain
