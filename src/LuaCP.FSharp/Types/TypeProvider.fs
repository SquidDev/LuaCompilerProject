namespace LuaCP.Types

open System
open System.Collections.Generic
open LuaCP.Types
open LuaCP.Types.Extensions
open LuaCP.IR
open LuaCP.Collections

type private SubtypeResult = 
    /// <summary>The type is assignable</summary>
    | Success
    /// <summary>The type is recursive and so still being worked on</summary>
    | InProgress
    /// <summary>The types are not assignable</summary>
    | Failure
    
    static member ForAll<'t> (predicate : 't -> SubtypeResult) (items : seq<'t>) = 
        let rec exec (items : IEnumerator<'t>) = 
            if items.MoveNext() then 
                match predicate items.Current with
                | Success | InProgress -> exec items
                | Failure -> Failure
            else Success
        exec (items.GetEnumerator())
    
    static member Exists<'t> (predicate : 't -> SubtypeResult) (items : seq<'t>) = 
        let rec exec (items : IEnumerator<'t>) = 
            if items.MoveNext() then 
                match predicate items.Current with
                | Success -> Success
                | Failure | InProgress -> exec items
            else Failure
        exec (items.GetEnumerator())
    
    member this.ToBoolean() = 
        match this with
        | Success -> true
        | Failure -> false
        | InProgress -> raise (InvalidOperationException "Cannot convert InProgress to boolean")

type TypeProvider() = 
    let valueMap = new Dictionary<ValueType * ValueType, SubtypeResult>()
    let tupleMap = new Dictionary<TupleType * TupleType, SubtypeResult>()
    let unaryMap = new Dictionary<Operator * ValueType, ValueType>()
    let binaryMap = new Dictionary<Operator * ValueType * ValueType, ValueType>()
    
    let isBaseSubtype (current : LiteralKind) (target : LiteralKind) : bool = 
        match (current, target) with
        | (LiteralKind.Integer, LiteralKind.Number) -> true // Integers are a subtype of number
        | _ -> current = target
    
    let rec isSubtype (current : ValueType) (target : ValueType) : SubtypeResult = 
        if Object.ReferenceEquals(current, target) then Success
        else 
            let (exists, result) = valueMap.TryGetValue((current, target))
            if exists then result
            else 
                let result = 
                    match (current, target) with
                    // Do unions/intersections first:
                    | (Union contents, _) -> SubtypeResult.ForAll (fun v -> isSubtype v target) contents
                    | (_, Union contents) -> SubtypeResult.Exists (fun v -> isSubtype current v) contents
                    | (Intersection contents, _) -> SubtypeResult.Exists (fun v -> isSubtype v target) contents
                    | (_, Intersection contents) -> SubtypeResult.ForAll (fun v -> isSubtype current v) contents
                    // Any type can be cast to/from anything
                    | (_, Dynamic) | (Dynamic, _) -> Success
                    | (Nil, Nil) | (Value, Value) -> Success
                    | (_, Value) -> 
                        // Nil is the only thing that cannot be converted to from value
                        match current with
                        | Nil -> Failure
                        | _ -> Success
                    // Primitives
                    | (Literal a, Literal b) -> 
                        if a = b then Success
                        else Failure
                    | (Literal a, Primitive b) -> 
                        if isBaseSubtype a.Kind b then Success
                        else Failure
                    | (Primitive a, Primitive b) -> 
                        if isBaseSubtype a b then Success
                        else Failure
                    // Advanded types
                    | (Function(cArgs, cRet), Function(tArgs, tRet)) -> 
                        if isTupleSubtype tArgs cArgs = Failure then Failure
                        else isTupleSubtype cRet tRet
                    | (Table(cFields, cOpcodes), Table(tFields, tOpcodes)) -> 
                        if SubtypeResult.ForAll (fun t -> SubtypeResult.Exists (fun c -> isFieldSubtype c t) cFields) 
                               tFields = Failure then Failure
                        else isOperatorsSubtype cOpcodes tOpcodes
                    | (Primitive current, Table(tFields, tOpcodes)) -> 
                        if List.isEmpty tFields then 
                            isOperatorsSubtype (OperatorHelpers.GetPrimitiveLookup current) tOpcodes
                        else Failure
                    | (Literal current, Table(tFields, tOpcodes)) -> 
                        if List.isEmpty tFields then 
                            isOperatorsSubtype (OperatorHelpers.GetPrimitiveLookup current.Kind) tOpcodes
                        else Failure
                    | Function(_, _), Table(tFields, tOpcodes) -> 
                        if List.isEmpty tFields then 
                            let ops : ValueType [] = Array.create OperatorExtensions.LastIndex Nil
                            ops.[int Operator.Call] <- current
                            isOperatorsSubtype ops tOpcodes
                        else Failure
                    // Reference types
                    | (Reference r, _) -> 
                        match r.Value with
                        | Link rTarget -> 
                            valueMap.Add((current, target), InProgress)
                            isSubtype current target
                        | _ -> raise (ArgumentException(sprintf "Cannot check conversion from %A" current))
                    | (_, Reference r) -> 
                        match r.Value with
                        | Link rCurrent -> 
                            valueMap.Add((current, target), InProgress)
                            isSubtype rCurrent target
                        | _ -> raise (ArgumentException(sprintf "Cannot check conversion to %A" current))
                    // These are the 'primitives', cannot be handled anywhere else.
                    | (_, Function(_, _)) | (_, Nil) | (_, Literal _) | (_, Primitive _) | (_, Table(_, _)) -> Failure
                
                let result = 
                    match result with
                    | InProgress -> Success
                    | x -> x
                
                valueMap.[(current, target)] <- result
                result
    and isTupleSubtype (current : TupleType) (target : TupleType) : SubtypeResult = 
        if current = target then Success
        else 
            let exists, result = tupleMap.TryGetValue((current, target))
            if exists then result
            else 
                let result = 
                    match current, target with
                    | (TReference r, _) -> 
                        match r.Value with
                        | Link rCurrent -> 
                            tupleMap.Add((current, target), InProgress)
                            isTupleSubtype current target
                        | _ -> raise (ArgumentException(sprintf "Cannot check conversion from %A" current))
                    | (_, TReference r) -> 
                        match r.Value with
                        | Link rTarget -> 
                            tupleMap.Add((current, target), InProgress)
                            isTupleSubtype current rTarget
                        | _ -> raise (ArgumentException(sprintf "Cannot check conversion to %A" current))
                    | Single(current, currentVar), Single(target, targetVar) -> 
                        let extractCurrent (x : Option<ValueType>) = 
                            if x.IsNone then Nil
                            else Union [ x.Value; Nil ]
                        
                        let extractTarget (x : Option<ValueType>) = 
                            if x.IsNone then Union [ Value; Nil ]
                            else Union [ x.Value; Nil ]
                        
                        let rec check current target = 
                            match current, target with
                            | [], [] -> Success // TODO: Check remainder
                            | cFirst :: cRem, tFirst :: tRem -> 
                                if isSubtype cFirst tFirst = Failure then Failure
                                else check cRem tRem
                            | [], tRem -> 
                                let c = extractCurrent currentVar
                                if SubtypeResult.ForAll (isSubtype c) tRem = Failure then Failure
                                else isSubtype c (extractTarget targetVar)
                            | cRem, [] -> 
                                let t = extractTarget targetVar
                                if SubtypeResult.ForAll (fun x -> isSubtype x t) cRem = Failure then Failure
                                else isSubtype (extractCurrent currentVar) t
                        
                        check current target
                
                let result = 
                    match result with
                    | InProgress -> Success
                    | x -> x
                
                tupleMap.[(current, target)] <- result
                result
    and isBiwaySubtype (current : ValueType) (target : ValueType) : SubtypeResult = 
        if isSubtype current target = Failure then Failure
        else isSubtype target current
    and isFieldSubtype (current : TableField) (target : TableField) = 
        if isSubtype target.Key current.Key = Failure then Failure
        else 
            match (current, target) with
            | ({ ReadOnly = false }, { ReadOnly = false }) -> isBiwaySubtype current.Value target.Value // The value type must be equal if we are converting
            | (_, { ReadOnly = true }) -> isSubtype current.Value target.Value // We can assign any subtype to a readonly field
            | _ -> Failure
    and isOperatorSubtype (current : ValueType) (target : ValueType) : SubtypeResult = 
        if target = Nil then Success
        else isSubtype current target
    and isOperatorsSubtype (current : Operators) (target : Operators) : SubtypeResult = 
        Seq.zip current target |> SubtypeResult.ForAll(fun (x, y) -> isOperatorSubtype x y)
    
    let rec getOperator (ty : ValueType) (op : Operator) = 
        let key = op, ty
        let exists, result = unaryMap.TryGetValue key
        if exists then result
        else 
            let res = 
                match ty with
                | Primitive prim -> OperatorHelpers.GetOperatorPrimitive prim op
                | Literal lit -> OperatorHelpers.GetOperatorPrimitive lit.Kind op
                | Dynamic -> OperatorHelpers.Dynamic.[int op]
                | Value | Nil -> Nil
                | Function(_, _) | Intersection _ -> 
                    if op = Operator.Call then ty
                    else Nil
                | Table(_, ops) -> ops.[int op]
                | Union items -> Union(List.map (fun x -> getOperator x op) items)
                | Reference item -> 
                    match item.Value with
                    | Link ty -> 
                        // Push an item to the cache to prevent recursive lookups
                        // TODO: What happens when a type is converted from an unbound to a link?
                        let cache = new IdentRef<VariableType<_>>(Unbound)
                        unaryMap.Add(key, Reference item)
                        let res = getOperator ty op
                        cache.Value <- Link res
                        res
                    | _ -> raise (ArgumentException(sprintf "Cannot get operator for %A" ty))
            unaryMap.[key] <- res
            res
    
    static member IsPrimitiveSubtype current target = 
        match (current, target) with
        | (LiteralKind.Integer, LiteralKind.Number) -> true // Integers are a subtype of number
        | _ -> current = target
    
    member this.IsBaseSubtype current target = isBaseSubtype current target
    member this.IsTypeEqual current target = (isBiwaySubtype current target).ToBoolean()
    member this.IsSubtype current target = (isSubtype current target).ToBoolean()
    member this.IsTupleSubtype current target = (isTupleSubtype current target).ToBoolean()
    member this.IsTupleEqual current target = 
        (isTupleSubtype current target).ToBoolean() && (isTupleSubtype target current).ToBoolean()
    
    /// <summary>Find the best function given a set of arguments</summary>
    member this.FindBestFunction (func : ValueType) (args : TupleType) = 
        let rec findBest (func : ValueType) (best : ValueType list) = 
            match func with
            | Intersection funcs -> findBests funcs best
            | Function(fArgs, _) -> 
                if isTupleSubtype args fArgs <> Failure then 
                    if isTupleSubtype fArgs args <> Failure then Some(func), []
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
        | (Some _, _) | (None, []) -> func, bests
        | (None, [ item ]) -> Some item, []
        | (None, _) -> 
            // Strip constants
            let literalToPrim x = 
                match x with
                | Literal x -> Primitive x.Kind
                | x -> x
            
            let args, remainder = args.Base
            
            let mapped : TupleType = 
                Single((List.map literalToPrim args), 
                       match remainder with
                       | Some x -> Some(literalToPrim x)
                       | None -> None)
            
            let equal (func : ValueType) = 
                match func with
                | Function(fArgs, _) -> isTupleSubtype fArgs mapped <> Failure
                | _ -> raise (ArgumentException(sprintf "Expected function type, got %A" func, "func"))
            
            match List.tryFind equal bests with
            | Some x -> Some x, []
            | None -> func, bests
    
    member this.FindBestPair (fields : TableField list) (key : ValueType) = 
        let rec findBest (fields : TableField list) (best : TableField list) = 
            match fields with
            | [] -> None, best
            | field :: rem -> 
                if isSubtype key (field.Key) <> Failure then 
                    if isSubtype (field.Key) key <> Failure then Some(field), best
                    else findBest rem (field :: best)
                else findBest rem best
        
        let field, bests = findBest fields []
        match field, bests with
        | Some _, _ | None, [] -> field, bests
        | None, [ item ] | Some item, [] -> Some item, []
        | (None, _) -> 
            let literalToPrim x = 
                match x with
                | Literal x -> Primitive x.Kind
                | x -> x
            
            let equal (field : TableField) = isSubtype (literalToPrim field.Key) key <> Failure
            match List.tryFind equal bests with
            | Some x -> Some x, []
            | None -> field, bests
    
    member this.GetOperator (ty : ValueType) (op : Operator) = getOperator ty op
    member this.GetBinaryOperatory (left : ValueType) (right : ValueType) (op : Operator) = 
        let handleRight() = 
            let rightOp = getOperator right op
            if rightOp <> Nil then 
                let best, remainder = this.FindBestFunction rightOp (Single([ left; right ], None))
                match best, remainder with
                | Some x, _ -> x
                | None, [] -> Nil
                | None, items -> Intersection items
            else Nil
        
        let leftOp = getOperator left op
        if leftOp <> Nil then 
            let best, remainder = this.FindBestFunction leftOp (Single([ left; right ], None))
            match best, remainder with
            | Some x, _ -> x
            | None, [] -> handleRight()
            | None, items -> Intersection items
        else handleRight()
