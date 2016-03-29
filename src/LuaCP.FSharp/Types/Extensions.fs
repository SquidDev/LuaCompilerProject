module LuaCP.Types.Extensions

open System
open LuaCP.Types
open LuaCP.Collections.Matching

let rec private hasUnbound (ty : ValueType) = 
    match ty with
    | Reference(IdentRef Unbound) -> true
    | Reference(_) -> true // TODO: Handle correctly
    | Primitive _ | Literal _ | Nil | Dynamic | Value -> false
    | FunctionIntersection items | Union items -> List.exists hasUnbound items
    | Table(tbl, ops) -> List.exists tableUnbound tbl || Array.exists hasUnbound ops
    | Function(args, ret) -> tupleUnbound args || tupleUnbound ret

and tupleUnbound (ty : TupleType) = 
    match ty with
    | Single(items, remainder) -> 
        List.exists hasUnbound items || match remainder with
                                        | None -> false
                                        | Some x -> hasUnbound x
    | TReference(IdentRef Unbound) -> true
    | TReference(_) -> false // TODO: Handle corretly

and tableUnbound (ty : TableField) = hasUnbound ty.Key || hasUnbound ty.Value

let rec private first (ty : TupleType) = 
    match ty with
    | Single([], Some x) -> Union [ x; Nil ]
    | Single([], None) -> Nil
    | Single(item :: rem, _) -> item
    | TReference(IdentRef Unbound) -> raise (ArgumentException("Cannot get return of unbound"))
    | TReference(IdentRef(Link x)) -> first ty

type ValueType with
    member this.HasUnbound = hasUnbound this
    
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
    member this.HasUnbound = tableUnbound this

type TupleType with
    member this.First = first this
    
    member this.Root = 
        let rec root (ty : TupleType) = 
            match ty with
            | TReference(IdentRef Unbound) -> raise (ArgumentException("Cannot get root of unbound"))
            | TReference(IdentRef(Link x)) -> root ty
            | Single(args, rem) -> args, rem
        root this
    
    member this.HasUnbound = tupleUnbound this
    member this.IsUnbound = 
        match this with
        | TReference(IdentRef Unbound) -> true
        | _ -> false