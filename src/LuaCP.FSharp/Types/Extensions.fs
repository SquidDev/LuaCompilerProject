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
    | Intersection items | Union items -> List.exists (hasUnbound visited) items
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

let rec private valueFlatten (visitedV : Dictionary<_, _>) (visitedT : Dictionary<_, _>) ty = 
    match ty with
    | Nil | Value | Dynamic | Primitive _ | Literal _ | Reference(IdentRef(Unbound)) -> ty
    | Table(fields, ops) -> 
        Table(List.map (fieldFlatten visitedV visitedT) fields, Array.map (valueFlatten visitedV visitedT) ops)
    | Union items -> Union(List.map (valueFlatten visitedV visitedT) items |> List.distinct)
    | Intersection items -> Intersection(List.map (valueFlatten visitedV visitedT) items |> List.distinct)
    | Function(args, ret) -> Function(tupleFlatten visitedV visitedT args, tupleFlatten visitedV visitedT ret)
    | Reference(IdentRef(Link child) as tRef) -> 
        let exists, cached = visitedV.TryGetValue tRef
        if exists then cached
        else 
            let link = new IdentRef<_>(Unbound)
            visitedV.Add(tRef, Reference link)
            let flattened = valueFlatten visitedV visitedT child
            link.Value <- Link cached
            flattened

and fieldFlatten (visitedV : Dictionary<_, _>) (visitedT : Dictionary<_, _>) ty = 
    { Key = valueFlatten visitedV visitedT ty.Key
      Value = valueFlatten visitedV visitedT ty.Value
      ReadOnly = ty.ReadOnly }

and tupleFlatten (visitedV : Dictionary<_, _>) (visitedT : Dictionary<_, _>) ty = 
    match ty with
    | Single(items, Some rem) -> 
        Single(List.map (valueFlatten visitedV visitedT) items, Some(valueFlatten visitedV visitedT rem))
    | Single(items, None) -> Single(List.map (valueFlatten visitedV visitedT) items, None)
    | TReference(IdentRef(Unbound)) -> ty
    | TReference(IdentRef(Link child) as tRef) -> 
        let exists, cached = visitedT.TryGetValue tRef
        if exists then cached
        else 
            let link = new IdentRef<_>(Unbound)
            visitedT.Add(tRef, TReference link)
            let flattened = tupleFlatten visitedV visitedT child
            link.Value <- Link cached
            flattened

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

let private baseTuple (this : TupleType) = 
    let rec doRoot ty = 
        match ty with
        | TReference(IdentRef Unbound) -> raise (ArgumentException("Cannot get root of unbound"))
        | TReference(IdentRef(Link x)) when x = this -> raise (ArgumentException("Cycle in type"))
        | TReference(IdentRef(Link x)) -> doRoot x
        | Single(args, rem) -> args, rem
    doRoot this

let rec private first (ty : TupleType) = 
    let ty, rem = baseTuple ty
    nth ty rem 0

type ValueType with
    member this.HasUnbound = hasUnbound (new HashSet<_>()) this
    
    member this.IsUnbound = 
        match this with
        | Reference(IdentRef Unbound) -> true
        | _ -> false
    
    member this.Flattened = valueFlatten (new Dictionary<_, _>()) (new Dictionary<_, _>()) this
    
    member this.Root = 
        let rec root (ty : ValueType) = 
            match ty with
            | Reference(IdentRef(Link x)) when x = this -> raise (ArgumentException("Cycle in type"))
            | Reference(IdentRef(Link x)) -> root x
            | ty -> ty
        root this
    
    member this.Return = 
        match this.Root with
        | Function(args, ret) -> first ret
        | _ -> raise (Exception(sprintf "Cannot extract function from %A" this))

type TableField with
    member this.HasUnbound = tableUnbound (new HashSet<_>()) this
    member this.Flattened = fieldFlatten (new Dictionary<_, _>()) (new Dictionary<_, _>()) this

type TupleType with
    member this.First = first this
    
    member this.Nth n = 
        let ty, rem = baseTuple this
        nth ty rem n
    
    member this.Base = baseTuple this
    
    member this.Root = 
        let rec root (ty : TupleType) = 
            match ty with
            | TReference(IdentRef(Link x)) when x = this -> raise (ArgumentException("Cycle in type"))
            | TReference(IdentRef(Link x)) -> root x
            | ty -> ty
        root this
    
    member this.HasUnbound = tupleUnbound (new HashSet<_>()) this
    member this.Flattened = tupleFlatten (new Dictionary<_, _>()) (new Dictionary<_, _>()) this
    member this.IsUnbound = 
        match this with
        | TReference(IdentRef Unbound) -> true
        | _ -> false

let (|BasicType|_|) (ty : ValueType) = 
    match ty with
    | Nil | Value | Dynamic | Primitive _ | Literal _ -> Some(ty)
    | _ -> None