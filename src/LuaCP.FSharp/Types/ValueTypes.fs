namespace LuaCP.Types

open LuaCP.IR

[<StructuredFormatDisplay("{AsString}")>]
type ValueType = 
    | Literal of Literal
    | Primitive of LiteralKind
    | Nil
    | Value
    | Dynamic
    | Function of TupleType * TupleType
    // | Table of (ValueType * ValueType) list
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
    
    static member FormatTuple(x : TupleType) = 
        match x with
        | items, Some x -> 
            "(" + (String.concat ", " (Seq.map ValueType.Format items)) + ", " + ValueType.Format x + "...)"
        | items, None -> "(" + (String.concat ", " (Seq.map ValueType.Format items)) + ")"
    
    override this.ToString() = ValueType.Format this
    member this.AsString = this.ToString()

// | Apply of ValueType * ValueType list
// | Var of VarType ref
// and VarType = 
//     | Unbound of id : int * level : int * bool
//     | Link of ValueType
//     | Generic of id : int
and TupleType = ValueType list * Option<ValueType>

module Primitives = 
    let Number = Primitive LiteralKind.Number
    let Integer = Primitive LiteralKind.Integer
    let String = Primitive LiteralKind.String
    let Boolean = Primitive LiteralKind.Boolean