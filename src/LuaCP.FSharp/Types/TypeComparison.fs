module LuaCP.Types.TypeComparison

open LuaCP.IR

/// <summary>
/// Check if a primitive is assignable to another primitive
/// </summary>
let isPrimitiveSubtype (current : LiteralKind) (target : LiteralKind) : bool = 
    match (current, target) with
    | (LiteralKind.Integer, LiteralKind.Number) -> true // Integers are a subtype of number
    | _ -> current = target

/// <summary>
/// Check if a basic type is assignable to another basic type
/// </summary>
let isBasicSubtype (current : ValueType) (target : ValueType) : bool = 
    match current, target with
    | _, Dynamic | Dynamic, _ -> true
    | Nil, Nil | Value, Value -> true
    | _, Value -> 
        // Nil is the only thing that cannot be converted to from value
        match current with
        | Nil -> false
        | _ -> true
    // Primitives
    | Literal a, Literal b -> a = b
    | Literal a, Primitive b -> isPrimitiveSubtype a.Kind b
    | Primitive a, Primitive b -> isPrimitiveSubtype a b
    | Value, _ -> false
    | _ -> invalidArg "current or target" (sprintf "Is not a primitive (%A or %A)" current target)
