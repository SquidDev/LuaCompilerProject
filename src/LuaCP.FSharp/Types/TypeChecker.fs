module LuaCP.Types.TypeChecker

open LuaCP.IR

// let Assignable (from : ValueType) (target : ValueType) = 
let IsBaseSubtype (current : LiteralKind) (target : LiteralKind) : bool = 
    match (current, target) with
    | (LiteralKind.Integer, LiteralKind.Number) -> true // Integers are a subtype of number
    | _ -> current = target

let rec IsSubtype (current : ValueType) (target : ValueType) : bool = 
    if current = target then true
    else 
        match (current, target) with
        // Do unions first:
        | (Union contents, _) -> Seq.forall (fun v -> IsSubtype v target) contents
        | (_, Union contents) -> Seq.exists (fun v -> IsSubtype current v) contents
        // Any type can be cast to/from anything
        | (_, Dynamic) | (Dynamic, _) -> true
        | (Nil, Nil) | (Value, Value) -> true
        | (_, Value) -> 
            // Nil is the only thing that cannot be converted to from value
            match current with
            | Nil -> false
            | _ -> true
        // Primitives
        | (Literal a, Literal b) -> a = b
        | (Literal a, Primitive b) -> IsBaseSubtype a.Kind b // You can convert "Hello" to string
        | (Primitive a, Primitive b) -> IsBaseSubtype a b
        // Function
        | (Function(cArgs, cRet), Function(tArgs, tRet)) -> (IsTupleSubtype tArgs cArgs) && (IsTupleSubtype cRet tRet)
        // These are the 'primitives', cannot be handled anywhere else. 
        | (_, Function(_, _)) | (_, Nil) | (_, Literal _) | (_, Primitive _) -> false

and IsTupleSubtype ((current, currentVar) : TupleType) ((target, targetVar) : TupleType) : bool = 
    let extract (x : Option<ValueType>) = 
        if x.IsNone then Nil
        else Union [ x.Value; Nil ]
    
    let rec check current target = 
        match current, target with
        | [], [] -> true // TODO: Ma
        | cFirst :: cRem, tFirst :: tRem -> IsSubtype cFirst tFirst && check cRem tRem
        | [], tRem -> 
            let c = extract currentVar
            List.forall (IsSubtype c) tRem && IsSubtype c (extract targetVar)
        | cRem, [] -> 
            let t = extract targetVar
            List.forall (fun x -> IsSubtype x t) cRem && IsSubtype (extract currentVar) t
    
    check current target