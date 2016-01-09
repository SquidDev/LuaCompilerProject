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
        // Advanded types
        | (Function(cArgs, cRet), Function(tArgs, tRet)) -> (IsTupleSubtype tArgs cArgs) && (IsTupleSubtype cRet tRet)
        | (Table(cFields, cOpcodes), Table(tFields, tOpcodes)) -> 
            Seq.forall (fun t -> Seq.exists (fun c -> IsFieldSubtype c t) cFields) tFields 
            && IsOperatorsSubtype cOpcodes tOpcodes
        | (Primitive current, Table(tFields, tOpcodes)) -> 
            Seq.isEmpty tFields && IsOperatorsSubtype (OperatorHandling.GetPrimitiveLookup current) tOpcodes
        | (Literal current, Table(tFields, tOpcodes)) -> 
            Seq.isEmpty tFields && IsOperatorsSubtype (OperatorHandling.GetPrimitiveLookup current.Kind) tOpcodes
        | Function(_, _), Table(tFields, tOpcodes) -> 
            if Seq.isEmpty tFields then 
                let ops : ValueType [] = Array.create OperatorExtensions.LastIndex Nil
                ops.[int Operator.Call] <- current
                IsOperatorsSubtype ops tOpcodes
            else false
        // These are the 'primitives', cannot be handled anywhere else. 
        | (_, Function(_, _)) | (_, Nil) | (_, Literal _) | (_, Primitive _) | (_, Table(_, _)) -> false

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

and IsBiwaySubtype (current : ValueType) (target : ValueType) : bool = 
    IsSubtype current target && IsSubtype target current

and IsFieldSubtype (current : TableField) (target : TableField) = 
    IsSubtype target.Key current.Key && match (current, target) with
                                        | ({ ReadOnly = false }, { ReadOnly = false }) -> 
                                            (IsBiwaySubtype current.Value target.Value) // The value type must be equal if we are converting
                                        | (_, { ReadOnly = true }) -> IsSubtype current.Key target.Key // We can assign any subtype to a readonly field
                                        | _ -> false

and IsOperatorSubtype (current : ValueType) (target : ValueType) = 
    if target = Nil then true
    else IsSubtype current target

and IsOperatorsSubtype (current : Operators) (target : Operators) = 
    Seq.zip current target |> Seq.forall (fun (x, y) -> IsOperatorSubtype x y)

let rec FindBestFunction (func : ValueType) (args : TupleType) = 
    let rec findBest (func : ValueType) (best : ValueType list) = 
        match func with
        | FunctionIntersection funcs -> findBests funcs best
        | Function(fArgs, _) -> 
            if IsTupleSubtype args fArgs then 
                if IsTupleSubtype fArgs args then Some(func), []
                else None, func :: best
            else None, best
        | Nil -> None, best
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