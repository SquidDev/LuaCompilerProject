module LuaCP.Types.Extensions

open System
open System.Collections.Generic
open LuaCP.Collections
open LuaCP.Types
open LuaCP.Collections.Matching

let rec private hasUnbound (visited : HashSet<Object>) (ty : ValueType) = 
    match ty with
    | Reference(IdentRef Unbound) -> true
    | Reference(IdentRef(Link(item)) as ref) -> visited.Add(ref :> Object) && hasUnbound visited item
    | Primitive _ | Literal _ | Nil | Dynamic | Value -> false
    | FunctionIntersection items | Union items -> List.exists (hasUnbound visited) items
    | Table(tbl, ops) -> List.exists (tableUnbound visited) tbl || Array.exists (hasUnbound visited) ops
    | Function(args, ret) -> (tupleUnbound visited) args || tupleUnbound visited ret

and tupleUnbound (visited : HashSet<Object>) (ty : TupleType) = 
    match ty with
    | Single(items, remainder) -> 
        List.exists (hasUnbound visited) items || match remainder with
                                                  | None -> false
                                                  | Some x -> (hasUnbound visited) x
    | TReference(IdentRef Unbound) -> true
    | TReference(IdentRef(Link(item)) as ref) -> visited.Add(ref :> Object) && tupleUnbound visited item

and tableUnbound (visited : HashSet<Object>) (ty : TableField) = 
    hasUnbound visited ty.Key || hasUnbound visited ty.Value

let rec private nth ty rem n = 
    match n with
    | 0 -> 
        match ty, rem with
        | [], Some x -> Union [ x; Nil ]
        | [], None -> Nil
        | item :: _, _ -> item
    | n -> 
        match ty, rem with
        | [], Some x -> Union [ x; Nil ]
        | [], None -> Nil
        | _ :: rest, _ -> nth rest rem (n - 1)

let rec private root (ty : TupleType) = 
    match ty with
    | TReference(IdentRef Unbound) -> raise (ArgumentException("Cannot get root of unbound"))
    | TReference(IdentRef(Link x)) -> root ty
    | Single(args, rem) -> args, rem

let rec private first (ty : TupleType) = 
    let ty, rem = root ty
    nth ty rem 0

type ValueType with
    member this.HasUnbound = hasUnbound (new HashSet<_>()) this
    
    member this.IsUnbound = 
        match this with
        | Reference(IdentRef Unbound) -> true
        | _ -> false
    
    member this.Root = 
        let rec root (ty : ValueType) = 
            match ty with
            | Reference(IdentRef Unbound) -> raise (ArgumentException("Cannot get root of unbound"))
            | Reference(IdentRef(Link x)) -> root ty
            | ty -> ty
        root this
    
    member this.Return = 
        match this.Root with
        | Function(args, ret) -> first ret
        | _ -> raise (Exception(sprintf "Cannot extract function from %A" this))

type TableField with
    member this.HasUnbound = tableUnbound (new HashSet<_>()) this

type TupleType with
    member this.First = first this
    
    member this.Nth n = 
        let ty, rem = root this
        nth ty rem n
    
    member this.Root = root this
    member this.HasUnbound = tupleUnbound (new HashSet<_>()) this
    member this.IsUnbound = 
        match this with
        | TReference(IdentRef Unbound) -> true
        | _ -> false
