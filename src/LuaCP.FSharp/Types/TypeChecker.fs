module LuaCP.Types.TypeChecker

open System
open LuaCP.Types
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
        // Do unions/intersections first:
        | (Union contents, _) -> Seq.forall (fun v -> IsSubtype v target) contents
        | (_, Union contents) -> Seq.exists (fun v -> IsSubtype current v) contents
        | (FunctionIntersection contents, _) -> Seq.exists (fun v -> IsSubtype v target) contents
        | (_, FunctionIntersection contents) -> Seq.forall (fun v -> IsSubtype current v) contents
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
    let extractCurrent (x : Option<ValueType>) = 
        if x.IsNone then Nil
        else Union [ x.Value; Nil ]
    
    let extractTarget (x : Option<ValueType>) = 
        if x.IsNone then Union [ Value; Nil ]
        else Union [ x.Value; Nil ]
    
    let rec check current target = 
        match current, target with
        | [], [] -> true // TODO: Ma
        | cFirst :: cRem, tFirst :: tRem -> IsSubtype cFirst tFirst && check cRem tRem
        | [], tRem -> 
            let c = extractCurrent currentVar
            List.forall (IsSubtype c) tRem && IsSubtype c (extractTarget targetVar)
        | cRem, [] -> 
            let t = extractTarget targetVar
            List.forall (fun x -> IsSubtype x t) cRem && IsSubtype (extractCurrent currentVar) t
    
    check current target

let rec FindBestFunction (func : ValueType) (args : TupleType) = 
    let rec findBest (func : ValueType) (best : ValueType list) = 
        match func with
        | FunctionIntersection funcs -> findBests funcs best
        | Function(fArgs, _) -> 
            if IsTupleSubtype args fArgs then 
                if IsTupleSubtype fArgs args then Some(func), []
                else None, func :: best
            else None, best
        | _ -> raise (ArgumentException(sprintf "Expected function type, got %A" func, "func"))
    
    and findBests (funcs : ValueType list) (best : ValueType list) = 
        match funcs with
        | [] -> None, best
        | item :: remaining -> 
            let found, best = findBest item best
            if found.IsSome then found, []
            else findBests remaining best
    
    let func, bests = findBest func []
    match func, bests with
    | (None, [ item ]) -> Some item, []
    | _ -> func, bests