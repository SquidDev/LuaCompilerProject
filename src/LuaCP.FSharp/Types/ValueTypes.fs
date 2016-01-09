namespace LuaCP.Types

open System
open LuaCP.IR

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
    
    static member Format(x : ValueType) = 
        match x with
        | Primitive x -> x.ToString().ToLowerInvariant()
        | Literal x -> x.ToString()
        | Nil -> "nil"
        | Value -> "value"
        | Dynamic -> "any"
        | Union(items) -> (String.concat " | " (Seq.map ValueType.Format items))
        | FunctionIntersection(items) -> (String.concat " & " (Seq.map ValueType.Format items))
        | Function(args, ret) -> ValueType.FormatTuple args + "->" + ValueType.FormatTuple ret
        | Table(items, meta) -> 
            let fields = 
                items |> Seq.map (fun item -> 
                             (if item.ReadOnly then "readonly "
                              else "") + ValueType.Format item.Key + ":" + ValueType.Format item.Value)
            
            let items = 
                meta
                |> Seq.mapi (fun x y -> x, y)
                |> Seq.filter (fun (x, y) -> y <> Nil)
                |> Seq.map (fun (x, y) -> "meta " + (enum<Operator> (x)).ToString() + ":" + ValueType.Format y)
            
            "{" + String.concat ", " (Seq.append fields items) + "}"
    
    static member FormatTuple(x : TupleType) = 
        match x with
        | items, Some x -> 
            "(" + (String.concat ", " (Seq.map ValueType.Format items)) + ", " + ValueType.Format x + "...)"
        | items, None -> "(" + (String.concat ", " (Seq.map ValueType.Format items)) + ")"
    
    override this.ToString() = ValueType.Format this
    member this.AsString = this.ToString()

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