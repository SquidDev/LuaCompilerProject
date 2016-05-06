namespace LuaCP.Types

open System
open LuaCP.IR
open LuaCP.Collections
open LuaCP.Collections.Matching

[<StructuredFormatDisplay("{AsString}")>]
type ValueType = 
    | Literal of Literal
    | Primitive of LiteralKind
    | Nil
    | Value
    | Dynamic
    | Function of TupleType * TupleType
    | Table of TableField Set * Operators
    | Union of ValueType Set
    | Intersection of ValueType Set
    | Reference of IdentRef<VariableType<ValueType>>
    
    static member Format (this : ValueType) (alloc : StringAllocator<int>) (brackets : bool) = 
        let formatB x = ValueType.Format x alloc true
        let format x = ValueType.Format x alloc false
        match this with
        | Primitive x -> x.ToString().ToLowerInvariant()
        | Literal x -> x.ToString()
        | Nil -> "nil"
        | Value -> "value"
        | Dynamic -> "any"
        | Union(items) -> 
            let str = (String.concat " | " (Seq.map formatB items))
            if brackets then "(" + str + ")"
            else str
        | Intersection(items) -> 
            let str = (String.concat " & " (Seq.map formatB items))
            if brackets then "(" + str + ")"
            else str
        | Function(args, ret) -> 
            let formatTuple x = TupleType.Format x alloc
            formatTuple args + "->" + formatTuple ret
        | Table(items, meta) -> 
            let fields = 
                items |> Seq.map (fun item -> 
                             (if item.ReadOnly then "readonly "
                              else "") + format item.Key + ":" + format item.Value)
            
            let items = 
                meta
                |> Seq.mapi (fun x y -> x, y)
                |> Seq.filter (fun (x, y) -> y <> Nil)
                |> Seq.map (fun (x, y) -> "meta " + (enum<Operator> (x)).ToString() + ":" + format y)
            
            "{" + String.concat ", " (Seq.append fields items) + "}"
        | Reference ref -> 
            match ref.Value with
            | Unbound -> "'0x" + ref.GetHashCode().ToString("X8") + "?"
            | Link ty -> "'0x" + ref.GetHashCode().ToString("X8")
    
    override this.ToString() = ValueType.Format this (new StringAllocator<int>()) false
    member this.AsString = this.ToString()
    member this.WithLabel() = 
        match this with
        | Reference(IdentRef(Link item)) -> this.ToString() + " : " + item.ToString()
        | _ -> this.ToString()

and VariableType<'t> = 
    | Unbound
    | Link of 't

and [<StructuredFormatDisplay("{AsString}")>] TupleType = 
    | Single of ValueType list * ValueType option
    | TReference of IdentRef<VariableType<TupleType>>
    static member Empty = Single([], None)
    
    static member Format (this : TupleType) (alloc : StringAllocator<int>) : string = 
        let format x = ValueType.Format x alloc false
        match this with
        | Single([], Some x) -> "(" + format x + "...)"
        | Single(items, Some x) -> "(" + (String.concat ", " (Seq.map format items)) + ", " + format x + "...)"
        | Single(items, None) -> "(" + (String.concat ", " (Seq.map format items)) + ")"
        | TReference ref -> 
            match ref.Value with
            | Unbound -> "'0x" + ref.GetHashCode().ToString("X8") + "?"
            | Link ty -> "'0x" + ref.GetHashCode().ToString("X8")
    
    override this.ToString() = TupleType.Format this (new StringAllocator<int>())
    member this.AsString = this.ToString()
    member this.WithLabel() = 
        match this with
        | TReference(IdentRef(Link item)) -> this.ToString() + " : " + item.ToString()
        | _ -> this.ToString()

and TableField = 
    { Key : ValueType
      Value : ValueType
      ReadOnly : bool }

and Operators = ValueType []

module Primitives = 
    let Number = Primitive LiteralKind.Number
    let Integer = Primitive LiteralKind.Integer
    let String = Primitive LiteralKind.String
    let Boolean = Primitive LiteralKind.Boolean
