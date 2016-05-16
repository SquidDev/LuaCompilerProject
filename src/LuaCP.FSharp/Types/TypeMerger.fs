namespace LuaCP.Types

open System
open System.Text
open System.Collections.Generic
open LuaCP.Collections
open LuaCP.Collections.Matching
open LuaCP.Types
open LuaCP.Types.Extensions

type BoundMode = 
    | Equal = 0
    | Minimum = 1
    | Maximum = 2

type BoundException(message : string) = 
    inherit Exception(message)

module TypeBounds = 
    let opposite (mode : BoundMode) = 
        match mode with
        | BoundMode.Equal -> BoundMode.Equal
        | BoundMode.Minimum -> BoundMode.Maximum
        | BoundMode.Maximum -> BoundMode.Minimum
        | mode -> invalidArg "mode" (sprintf "Invalid mode %A" mode)
    
    let private canMerge (a : ValueType) (b : ValueType) : bool = 
        let a = a.Root
        let b = b.Root
        if a = b then true
        else 
            match a, b with
            | (Nil | Value | Primitive _ | Literal _), (Nil | Value | Primitive _ | Literal _) -> true
            | Reference _, _ | _, Reference _ -> false
            | Function(aArgs, aRet), Function(bArgs, bRet) -> true
            | Table(aFields, aOps), Table(bFields, bOps) -> true
            | Dynamic, _ | _, Dynamic -> true
            | _, _ -> false
    
    let rec Value (mode : BoundMode) (a : ValueType) (b : ValueType) : ValueType = 
        let a = a.Root
        let b = b.Root
        if a = b then a
        else 
            match a, b with
            | (Nil | Value | Primitive _ | Literal _), (Nil | Value | Primitive _ | Literal _) -> 
                match mode with
                | BoundMode.Equal -> raise (BoundException(sprintf "Cannot equate %A and %A" a b))
                | BoundMode.Minimum -> 
                    if TypeComparison.isBasicSubtype a b then b
                    elif TypeComparison.isBasicSubtype b a then a
                    else Set.of2 a b |> Union
                | BoundMode.Maximum -> 
                    if TypeComparison.isBasicSubtype a b then a
                    elif TypeComparison.isBasicSubtype b a then b
                    else raise (BoundException(sprintf "Cannot intersect %A and %A" a b))
                | mode -> invalidArg "mode" (sprintf "Invalid mode %A" mode)
            | Reference(IdentRef(Unbound) as tRef), ty | ty, Reference(IdentRef(Unbound) as tRef) -> 
                match mode with
                | BoundMode.Equal -> 
                    tRef.Value <- Link ty
                    ty
                | BoundMode.Minimum -> 
                    match ty with
                    | Union other -> Set.add (Reference tRef) other |> Union
                    | ty -> Set.of2 (Reference tRef) ty |> Union
                | BoundMode.Maximum -> 
                    match ty with
                    | Intersection other -> Set.add (Reference tRef) other |> Intersection
                    | ty -> Set.of2 (Reference tRef) ty |> Intersection
                | mode -> invalidArg "mode" (sprintf "Invalid mode %A" mode)
            | Function(aArgs, aRet), Function(bArgs, bRet) -> 
                // TODO: Intersect functions instead
                Function(Tuple (opposite mode) aArgs bArgs, Tuple mode aRet bRet)
            | Table(aFields, aOps), Table(bFields, bOps) -> 
                let flag a b = 
                    match mode with
                    | BoundMode.Equal -> a || b
                    | BoundMode.Minimum -> a && b
                    | BoundMode.Maximum -> a || b
                    | mode -> invalidArg "mode" (sprintf "Invalid mode %A" mode)
                
                let convertPair a b = 
                    assert (a.Key = b.Key)
                    { Key = a.Key
                      Value = Value mode a.Value b.Value
                      ReadOnly = flag a.ReadOnly b.ReadOnly }
                
                let convert (key, left, right) = 
                    match left, right with
                    | EmptySet, EmptySet -> None
                    | EmptySet, other | other, EmptySet -> 
                        match mode with
                        | BoundMode.Equal -> 
                            raise (BoundException(sprintf "Unmatched values for key %A in types %A and %A" key a b))
                        | BoundMode.Minimum -> None
                        | BoundMode.Maximum -> Seq.foldHead convertPair other |> Some
                        | mode -> invalidArg "mode" (sprintf "Invalid mode %A" mode)
                    | left, right -> 
                        Seq.append left right
                        |> Seq.foldHead convertPair
                        |> Some
                
                let fields = 
                    Seq.groupBy2 (fun x -> x.Key) aFields bFields
                    |> Seq.choose convert
                    |> Set.ofSeq
                
                let ops = Array.map2 (Value mode) aOps bOps
                Table(fields, ops)
            | Reference(_), _ | _, Reference(_) -> failwith (sprintf "Unexpected state intersecting %A and %A" a b)
            | Dynamic, other | other, Dynamic -> 
                match mode with
                | BoundMode.Equal -> 
                    if a = b then a
                    else raise (BoundException(sprintf "Cannot merge %A and %A" a b))
                | BoundMode.Minimum -> other
                | BoundMode.Maximum -> other
                | mode -> invalidArg "mode" (sprintf "Invalid mode %A" mode)
            | Intersection a, Intersection b -> 
                match mode with
                | BoundMode.Equal -> Intersection a // TODO: Check that they are the same
                // This is wrong: we should be getting distinct ones and intersecting them.
                // Minimum of (a & b) and a is a
                | BoundMode.Minimum -> 
                    Seq.append a b
                    |> Set.ofSeq
                    |> Union
                | BoundMode.Maximum -> 
                    Seq.append a b
                    |> Set.ofSeq
                    |> Intersection
                | mode -> invalidArg "mode" (sprintf "Invalid mode %A" mode)
            | Intersection a, b | b, Intersection a when mode = BoundMode.Maximum -> 
                let b = 
                    match b with
                    | Intersection b -> b
                    | b -> Set.singleton b
                
                let mutable result = a
                for ty in b do
                    if not (Set.contains ty result) then 
                        match Seq.tryFind (canMerge ty) result with
                        | Some other -> result <- Set.remove other result |> Set.add (Value mode other ty)
                        | None _ -> result <- Set.add ty result
                if result.Count = 1 then result.MinimumElement
                else Intersection result
            | Union a, b | b, Union a when mode = BoundMode.Minimum -> 
                let b = 
                    match b with
                    | Union b -> b
                    | b -> Set.singleton b
                
                let mutable result = a
                for ty in b do
                    if not (Set.contains ty result) then 
                        match Seq.tryFind (canMerge ty) result with
                        | Some a -> result <- Set.remove a result |> Set.add (Value mode a ty)
                        | None _ -> result <- Set.add ty result
                if result.Count = 1 then result.MinimumElement
                else Union result
            | _, _ -> 
                printfn "TODO: %A of %A and %A" mode a b
                match mode with
                | BoundMode.Equal -> a
                | BoundMode.Minimum -> Set.of2 a b |> Union
                | BoundMode.Maximum -> Set.of2 a b |> Intersection
                | mode -> invalidArg "mode" (sprintf "Invalid mode %A" mode)
    
    and Tuple (mode : BoundMode) (a : TupleType) (b : TupleType) = 
        let a = a.Root
        let b = b.Root
        if a = b then a
        else 
            match a, b with
            | TReference(IdentRef(Unbound) as tRef), ty | ty, TReference(IdentRef(Unbound) as tRef) -> 
                match mode with
                | BoundMode.Equal -> 
                    tRef.Value <- Link ty
                    ty
                | _ -> 
                    printfn "TODO: %A of %A and %A" mode a b
                    ty
            | Single(aArgs, aRem), Single(bArgs, bRem) -> 
                let rec check (aLi : ValueType list) (bLi : ValueType list) = 
                    match aLi, bLi with
                    | [], [] -> []
                    | aFirst :: aRem, bFirst :: bRem -> Value mode aFirst bFirst :: check aRem bRem
                    | [], other | other, [] -> 
                        match mode with
                        | BoundMode.Equal -> 
                            raise (BoundException(sprintf "Cannot intersect %A and %A, not the same length" a b))
                        | BoundMode.Minimum -> []
                        | BoundMode.Maximum -> other // Make union with nil and oppositeRem?
                        | mode -> invalidArg "mode" (sprintf "Invalid mode %A" mode)
                
                let args : ValueType list = check aArgs bArgs
                
                let rem = 
                    match aRem, bRem with
                    | Some aRem, Some bRem -> Some(Value mode aRem bRem)
                    | None, None -> None
                    | Some rem, None | None, Some rem -> 
                        match mode with
                        | BoundMode.Equal -> raise (BoundException(sprintf "Cannot intersect %A and %A" a b))
                        | BoundMode.Maximum -> Some rem
                        | BoundMode.Minimum -> None
                        | mode -> invalidArg "mode" (sprintf "Invalid mode %A" mode)
                Single(args, rem)
            | TReference(_), _ | _, TReference(_) -> failwith (sprintf "Unexpected state intersecting %A and %A" a b)

[<StructuredFormatDisplay("{AsString}")>]
type TypeConstraint<'t>(ty : 't, bound : BoundMode -> 't -> 't -> 't, merge : 't -> 't -> unit) = 
    let mutable ty = ty
    let mutable minimum : 't option = None
    let mutable maximum : 't option = None
    let subtypes = new HashSet<'t>()
    let supertypes = new HashSet<'t>()
    let equal = new HashSet<'t>()
    member this.Type = ty
    member this.Equal = equal
    member this.Minimum = minimum
    member this.Maximum = maximum
    
    member this.AddSubtype sub = 
        if subtypes.Add sub then 
            let min = 
                match minimum with
                | None -> sub
                | Some min -> bound (BoundMode.Maximum) sub min
            minimum <- Some min
            merge ty min
            match maximum with
            | Some max -> merge max min
            | None -> ()
        minimum.Value
    
    member this.AddSupertype sup = 
        if supertypes.Add sup then 
            let max = 
                match maximum with
                | None -> sup
                | Some max -> bound (BoundMode.Minimum) sup max
            maximum <- Some max
            merge max ty
            match minimum with
            | Some min -> merge max min
            | None -> ()
        maximum.Value
    
    member this.Equate nTy = 
        match maximum with
        | None -> ()
        | Some max -> merge max nTy
        match minimum with
        | None -> ()
        | Some min -> merge nTy min
        ty <- nTy
    
    override this.ToString() = 
        let writer = new StringBuilder()
        writer.AppendFormat("{0}\n", ty) |> ignore
        // Minimum
        match minimum with
        | Some ty -> writer.AppendFormat("    :> {0}\n", ty) |> ignore
        | None -> ()
        for ty in subtypes do
            writer.AppendFormat("        :> {0}\n", ty) |> ignore
        // Maximum
        match maximum with
        | Some ty -> writer.AppendFormat("    <: {0}\n", ty) |> ignore
        | None -> ()
        for ty in supertypes do
            writer.AppendFormat("        <: {0}\n", ty) |> ignore
        // TODO: Remove this
        for ty in equal do
            writer.AppendFormat("    = {0}\n", ty) |> ignore
        writer.ToString()
    
    member this.AsString = this.ToString()

and RefLookup<'t> = Dictionary<IdentRef<VariableType<'t>>, TypeConstraint<'t>>

type VisitedList<'t> = HashSet<'t * 't>

type SubtypeMap<'t when 't : comparison> = Set<'t * 't>

type TypeMap = SubtypeMap<ValueType> * SubtypeMap<TupleType>

and TypeMerger() = 
    let values = new RefLookup<ValueType>()
    let tuples = new RefLookup<TupleType>()
    let provider = new TypeProvider()
    let valueNil = Set.of2 Value Nil |> Union
    
    let rec mergeValues (vVisited : VisitedList<_>) (tVisited : VisitedList<_>) (current : ValueType) 
            (target : ValueType) (typeMap : TypeMap) : TypeMap option = 
        if vVisited.Add(current, target) then 
            let current = current.Root
            let target = target.Root
            if current <> target then 
                match current, target with
                | Reference(IdentRef(Unbound)), _ | _, Reference(IdentRef(Unbound)) -> 
                    let values, tuples = typeMap
                    Some(Set.add (current, target) values, tuples)
                | Reference(_), _ | _, Reference(_) -> 
                    failwith (sprintf "Unexpected state merging %A :> %A" current target)
                | Union current, _ -> 
                    Seq.foldAbort (fun s v -> mergeValues vVisited tVisited v target s) typeMap current
                | _, Intersection target -> 
                    Seq.foldAbort (fun s v -> mergeValues vVisited tVisited current v s) typeMap target
                | _, Union target -> 
                    Seq.foldOption (fun s v -> mergeValues vVisited tVisited current v s) typeMap target
                | Intersection current, _ -> 
                    Seq.foldOption (fun s v -> mergeValues vVisited tVisited v target s) typeMap current
                | (Nil | Value | Dynamic | Primitive _ | Literal _), (Nil | Value | Dynamic | Primitive _ | Literal _) -> 
                    if not (TypeComparison.isBasicSubtype current target) then 
                        raise (Exception(sprintf "Cannot cast %A to %A" current target))
                    else Some typeMap
                | Table(currentFields, currentOps), Table(targetFields, targetOps) -> 
                    let handle (values, tuples) targetField = 
                        match Seq.tryFind (fun x -> x.Key = targetField.Key) currentFields with
                        | Some currentField -> 
                            mergeValues vVisited tVisited currentField.Value targetField.Value (values, tuples)
                        | None -> 
                            printfn "Missing field %A in %A" targetField.Key current
                            Some(values, tuples)
                    Seq.foldAbort handle typeMap targetFields
                | Function(currentA, currentR), Function(targetA, targetR) -> 
                    match mergeTuples vVisited tVisited targetA currentA typeMap with
                    | Some typeMap -> mergeTuples vVisited tVisited currentR targetR typeMap
                    | None -> None
                | _, _ -> 
                    printfn "TODO: %A :> %A" current target // TODO: Implement other types
                    Some typeMap
            else Some typeMap
        else Some typeMap
    and mergeTuples (vVisited : VisitedList<_>) (tVisited : VisitedList<_>) (current : TupleType) (target : TupleType) 
        ((values, tuples) : TypeMap) : TypeMap option = 
        if tVisited.Add(current, target) then 
            let current = current.Root
            let target = target.Root
            if current <> target then 
                match current, target with
                | TReference(IdentRef(Unbound)), _ | _, TReference(IdentRef(Unbound)) -> 
                    Some(values, Set.add (current, target) tuples)
                | TReference(_), _ | _, TReference(_) -> 
                    failwith (sprintf "Unexpected state merging %A :> %A" current target)
                | Single(current, currentVar), Single(target, targetVar) -> 
                    let extractCurrent (x : Option<ValueType>) = 
                        if x.IsNone then Nil
                        else Set.of2 x.Value Nil |> Union
                    
                    let extractTarget (x : Option<ValueType>) = 
                        if x.IsNone then valueNil
                        else Set.of2 x.Value Nil |> Union
                    
                    let rec check current target (typeMap : TypeMap) : TypeMap option = 
                        match current, target with
                        | [], [] -> Some typeMap
                        | cFirst :: cRem, tFirst :: tRem -> 
                            match mergeValues vVisited tVisited cFirst tFirst typeMap with
                            | Some typeMap -> check cRem tRem typeMap
                            | None -> None
                        | [], tRem -> 
                            let c = extractCurrent currentVar
                            match Seq.foldAbort (fun s x -> mergeValues vVisited tVisited c x s) typeMap tRem with
                            | Some typeMap -> mergeValues vVisited tVisited c (extractTarget targetVar) typeMap
                            | None -> None
                        | cRem, [] -> 
                            let t = extractTarget targetVar
                            match Seq.foldAbort (fun s x -> mergeValues vVisited tVisited x t s) typeMap cRem with
                            | Some typeMap -> mergeValues vVisited tVisited (extractCurrent currentVar) t typeMap
                            | None -> None
                    
                    check current target (values, tuples)
            else Some(values, tuples)
        else Some(values, tuples)
    
    let applyMappings (valueMapped, tupleMapped) = 
        for current, target in valueMapped do
            match current, target with
            | Reference(IdentRef(Unbound) as currentRef), Reference(IdentRef(Unbound) as targetRef) -> 
                values.[currentRef].AddSubtype target |> ignore
                values.[targetRef].AddSubtype current |> ignore
            | Reference(IdentRef(Unbound) as current), target -> values.[current].AddSubtype target |> ignore
            | current, Reference(IdentRef(Unbound) as target) -> values.[target].AddSupertype current |> ignore
            | _, _ -> failwith (sprintf "Unexpected state merging %A :> %A" current target)
        for current, target in tupleMapped do
            match current, target with
            | TReference(IdentRef(Unbound) as currentRef), TReference(IdentRef(Unbound) as targetRef) -> 
                tuples.[currentRef].AddSubtype target |> ignore
                tuples.[targetRef].AddSubtype current |> ignore
            | TReference(IdentRef(Unbound) as current), target -> tuples.[current].AddSubtype target |> ignore
            | current, TReference(IdentRef(Unbound) as target) -> tuples.[target].AddSupertype current |> ignore
            | _, _ -> failwith (sprintf "Unexpected state merging %A :> %A" current target)
    
    let mergeValuesImpl current target = 
        match mergeValues (new VisitedList<_>()) (new VisitedList<_>()) current target (Set.empty, Set.empty) with
        | None -> raise (Exception(sprintf "Cannot merge %A :> %A" current target))
        | Some typeMap -> applyMappings typeMap
    
    let mergeTuplesImpl current target = 
        match mergeTuples (new VisitedList<_>()) (new VisitedList<_>()) current target (Set.empty, Set.empty) with
        | None -> raise (Exception(sprintf "Cannot merge %A :> %A" current target))
        | Some typeMap -> applyMappings typeMap
    
    member this.ValueGet ref = values.[ref]
    member this.TupleGet ref = tuples.[ref]
    
    member this.ValueNew() = 
        let ref = new IdentRef<_>(Unbound)
        let cons = new TypeConstraint<_>(Reference ref, TypeBounds.Value, mergeValuesImpl)
        values.Add(ref, cons)
        cons
    
    member this.TupleNew() = 
        let ref = new IdentRef<_>(Unbound)
        let cons = new TypeConstraint<_>(TReference ref, TypeBounds.Tuple, mergeTuplesImpl)
        tuples.Add(ref, cons)
        cons
    
    member this.Value = TypeBounds.Value
    member this.MergeValues = mergeValuesImpl
    member this.Tuple = TypeBounds.Tuple
    member this.MergeTuples = mergeTuplesImpl
    member this.Bake() = 
        let bindV (cons : TypeConstraint<_>) tRef (ty : ValueType) existing = 
            match ty.Root with
            | Reference(IdentRef(Unbound) as oRef) when oRef = tRef -> existing
            | ty -> 
                tRef.Value <- Link ty
                cons.Equate ty
                true
        
        let rec applyV existing (cons : TypeConstraint<_>) = 
            match cons.Type, cons.Maximum, cons.Minimum with
            | Reference(IdentRef(Unbound) as tRef), Some ty, _ -> 
                // If we have a maximum then use that.
                // TODO: Simply check if this is compatible with minimum 
                // and use minimum instead
                bindV cons tRef ty existing
            | Reference(IdentRef(Unbound) as tRef), None, Some ty -> 
                // If we have no maximum, then we should use this
                bindV cons tRef ty existing
            | _ -> existing
        
        let bindT (cons : TypeConstraint<_>) tRef (ty : TupleType) existing = 
            match ty.Root with
            | TReference(IdentRef(Unbound) as oRef) when oRef = tRef -> existing
            | ty -> 
                tRef.Value <- Link ty
                cons.Equate ty
                true
        
        let rec applyT existing (cons : TypeConstraint<_>) = 
            match cons.Type, cons.Maximum, cons.Minimum with
            | TReference(IdentRef(Unbound) as tRef), Some ty, _ -> 
                // If we have a maximum then use that.
                // TODO: Simply check if this is compatible with minimum 
                // and use minimum instead
                bindT cons tRef ty existing
            | TReference(IdentRef(Unbound) as tRef), None, Some ty -> 
                // If we have no maximum, then we should use this
                bindT cons tRef ty existing
            | _ -> existing
        
        let rec applyRec() = 
            let values = Seq.fold applyV false values.Values
            let tuples = Seq.fold applyT false tuples.Values
            if values || tuples then applyRec()
            else ()
        
        applyRec()
