namespace LuaCP.Types

open System
open LuaCP.IR
open LuaCP.Collections

/// <summary>A reference that does not implement</summary>
type IdentRef<'t>(value : 't) = 
    let mutable x = value
    
    member this.Value 
        with get () = x
        and set (value) = x <- value

[<StructuredFormatDisplay("{AsString}")>]
type ValueType = 
    | Literal of Literal
    | Primitive of LiteralKind
    | Nil
    | Value
    | Dynamic
    | Function of TupleType * TupleType
    | Table of TableField list * Operators
    | Union of ValueType list
    | FunctionIntersection of ValueType list
    | Reference of IdentRef<VariableType>
    
    static member Format (this : ValueType) (alloc : StringAllocator<int>) = 
        let format x = ValueType.Format x alloc
        match this with
        | Primitive x -> x.ToString().ToLowerInvariant()
        | Literal x -> x.ToString()
        | Nil -> "nil"
        | Value -> "value"
        | Dynamic -> "any"
        | Union(items) -> (String.concat " | " (Seq.map format items))
        | FunctionIntersection(items) -> (String.concat " & " (Seq.map format items))
        | Function(args, ret) -> 
            let formatTuple x = ValueType.FormatTuple x alloc
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
            | Generic id -> alloc.[id]
            | Unbound -> "'0x" + ref.GetHashCode().ToString("X8")
            | Link ty -> "'a0x" + ref.GetHashCode().ToString("X8")
    
    static member FormatTuple (this : TupleType) (alloc : StringAllocator<int>) : string = 
        let format x = ValueType.Format x alloc
        match this with
        | items, Some x -> "(" + (String.concat ", " (Seq.map format items)) + ", " + format x + "...)"
        | items, None -> "(" + (String.concat ", " (Seq.map format items)) + ")"
    
    override this.ToString() = ValueType.Format this (new StringAllocator<int>())
    member this.AsString = this.ToString()

and VariableType = 
    | Unbound
    | Link of ValueType
    | Generic of int

and TupleType = ValueType list * Option<ValueType>

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