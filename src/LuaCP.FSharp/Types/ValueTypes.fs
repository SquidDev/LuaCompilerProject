namespace LuaCP.Types

open LuaCP.IR

type ValueType = 
    | Literal of Literal
    | Primitive of LiteralKind
    | Nil
    | Value
    | Dynamic
    | Function of TupleType * TupleType
    // | Table of (ValueType * ValueType) list
    | Union of ValueType list
    // | Intersection of ValueType list
    // | Apply of ValueType * ValueType list
    // | Var of VarType ref

// and VarType = 
//     | Unbound of id : int * level : int * bool
//     | Link of ValueType
//     | Generic of id : int

and TupleType = ValueType list * Option<ValueType>