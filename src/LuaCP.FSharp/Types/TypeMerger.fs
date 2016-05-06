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
    
    let rec Value (mode : BoundMode) (a : ValueType) (b : ValueType) : ValueType = 
        let a = a.Root
        let b = b.Root
        if a = b then a
        else 
            match a, b with
            | Literal lit, Primitive kind | Primitive kind, Literal lit -> 
                match mode with
                | BoundMode.Equal -> raise (BoundException(sprintf "Cannot equate %A and %A" a b))
                | BoundMode.Minimum -> 
                    if TypeComparison.isPrimitiveSubtype lit.Kind kind then Primitive kind
                    else Set.of2 a b |> Union
                | BoundMode.Maximum -> 
                    if TypeComparison.isPrimitiveSubtype lit.Kind kind then Literal lit
                    else raise (BoundException(sprintf "Cannot intersect %A and %A" lit kind))
                | mode -> invalidArg "mode" (sprintf "Invalid mode %A" mode)
            | Primitive a, Primitive b -> 
                match mode with
                | BoundMode.Equal -> raise (BoundException(sprintf "Cannot equate %A and %A" a b))
                | BoundMode.Minimum -> 
                    if TypeComparison.isPrimitiveSubtype a b then Primitive a
                    elif TypeComparison.isPrimitiveSubtype b a then Primitive b
                    else Set.of2 (Primitive a) (Primitive b) |> Union
                | BoundMode.Maximum -> 
                    if TypeComparison.isPrimitiveSubtype a b then Primitive b
                    elif TypeComparison.isPrimitiveSubtype b a then Primitive a
                    else raise (BoundException(sprintf "Cannot intersect %A and %A" a b))
                | mode -> invalidArg "mode" (sprintf "Invalid mode %A" mode)
            | Reference(IdentRef(Unbound) as tRef), ty | ty, Reference(IdentRef(Unbound) as tRef) -> 
                match mode with
                | BoundMode.Equal -> 
                    tRef.Value <- Link ty
                    ty
                | BoundMode.Minimum -> Set.of2 a b |> Union
                | BoundMode.Maximum -> Set.of2 a b |> Intersection
                | mode -> invalidArg "mode" (sprintf "Invalid mode %A" mode)
            | Function(aArgs, aRet), Function(bArgs, bRet) -> 
                Function(Tuple mode aArgs bArgs, Tuple (opposite mode) aArgs bArgs)
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
                
                let convert (_, field) = Seq.skip 1 field |> Seq.fold convertPair (Seq.head field)
                
                let fields = 
                    Seq.concat [ aFields; bFields ]
                    |> Seq.groupBy (fun x -> x.Key)
                    |> Seq.map convert
                    |> Set.ofSeq
                
                let ops = Array.map2 (Value mode) aOps bOps
                Table(fields, ops)
            | Reference(_), _ | _, Reference(_) -> 
                failwith (sprintf "Unexpected state intersecting %A and %A" a b)
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
                    Seq.append a b |> Set.ofSeq |> Union
                | BoundMode.Maximum -> 
                    Seq.append a b |> Set.ofSeq |> Intersection
                | mode -> invalidArg "mode" (sprintf "Invalid mode %A" mode)
            | Intersection items, ty | ty, Intersection items -> 
                match mode with
                | BoundMode.Equal -> raise (BoundException(sprintf "Cannot merge %A and %A" a b))
                // See above
                | BoundMode.Minimum -> 
                    Set.add ty items |> Union
                | BoundMode.Maximum -> 
                    Set.add ty items |> Intersection
                | mode -> invalidArg "mode" (sprintf "Invalid mode %A" mode)
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
                let args = List.map2 (Value mode) aArgs bArgs
                
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
            | TReference(_), _ | _, TReference(_) -> 
               failwith (sprintf "Unexpected state intersecting %A and %A" a b)

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

and TypeMerger() = 
    let values = new RefLookup<ValueType>()
    let tuples = new RefLookup<TupleType>()
    let provider = new TypeProvider()
    let valueNil = Set.of2 Value Nil |> Union
    
    let rec mergeValues (vVisited : VisitedList<_>) (tVisited : VisitedList<_>) (current : ValueType) 
            (target : ValueType) = 
        if vVisited.Add(current, target) then 
            let current = current.Root
            let target = target.Root
            if current <> target then 
                match current, target with
                | Reference(IdentRef(Unbound) as currentRef), Reference(IdentRef(Unbound) as targetRef) -> 
                    values.[currentRef].AddSubtype target |> ignore
                    values.[targetRef].AddSupertype current |> ignore
                | Reference(IdentRef(Unbound) as current), target -> values.[current].AddSubtype target |> ignore
                | current, Reference(IdentRef(Unbound) as target) -> values.[target].AddSupertype current |> ignore
                | Reference(_), _ | _, Reference(_) -> 
                    failwith (sprintf "Unexpected state merging %A :> %A" current target)
                | Union current, _ -> Set.iter (fun v -> mergeValues vVisited tVisited v target) current
                | _, Intersection target -> Set.iter (fun v -> mergeValues vVisited tVisited current v) target
                | (Nil | Value | Dynamic | Primitive _ | Literal _), (Nil | Value | Dynamic | Primitive _ | Literal _) -> 
                    if not (TypeComparison.isBasicSubtype current target) then 
                        raise (Exception(sprintf "Cannot cast %A to %A" current target))
                | Table(currentFields, currentOps), Table(targetFields, targetOps) -> 
                    for targetField in targetFields do
                        match Seq.tryFind (fun x -> x.Key = targetField.Key) currentFields with
                        | Some currentField -> mergeValues vVisited tVisited currentField.Value targetField.Value
                        | None -> printfn "Missing field %A in %A" targetField.Key current
                | Function(currentA, currentR), Function(targetA, targetR) -> 
                    mergeTuples vVisited tVisited targetA currentA
                    mergeTuples vVisited tVisited currentR targetR
                | _, _ -> printfn "TODO: %A :> %A" current target // TODO: Implement other types
    and mergeTuples (vVisited : VisitedList<_>) (tVisited : VisitedList<_>) (current : TupleType) (target : TupleType) = 
        if tVisited.Add(current, target) then 
            let current = current.Root
            let target = target.Root
            if current <> target then 
                match current, target with
                | TReference(IdentRef(Unbound) as currentRef), TReference(IdentRef(Unbound) as targetRef) -> 
                    tuples.[currentRef].AddSubtype target |> ignore
                    tuples.[targetRef].AddSupertype current |> ignore
                | TReference(IdentRef(Unbound) as current), target -> tuples.[current].AddSubtype target |> ignore
                | current, TReference(IdentRef(Unbound) as target) -> tuples.[target].AddSupertype current |> ignore
                | TReference(_), _ | _, TReference(_) -> 
                    failwith (sprintf "Unexpected state merging %A :> %A" current target)
                | Single(current, currentVar), Single(target, targetVar) -> 
                    let extractCurrent (x : Option<ValueType>) = 
                        if x.IsNone then Nil
                        else Set.of2 x.Value Nil |> Union
                    
                    let extractTarget (x : Option<ValueType>) = 
                        if x.IsNone then valueNil
                        else Set.of2 x.Value Nil |> Union
                    
                    let rec check current target = 
                        match current, target with
                        | [], [] -> ()
                        | cFirst :: cRem, tFirst :: tRem -> 
                            mergeValues vVisited tVisited cFirst tFirst
                            check cRem tRem
                        | [], tRem -> 
                            let c = extractCurrent currentVar
                            List.iter (mergeValues vVisited tVisited c) tRem
                            mergeValues vVisited tVisited c (extractTarget targetVar)
                        | cRem, [] -> 
                            let t = extractTarget targetVar
                            List.iter (fun x -> mergeValues vVisited tVisited x t) cRem
                            mergeValues vVisited tVisited (extractCurrent currentVar) t
                    
                    check current target
    
    let mergeValuesImpl current target = mergeValues (new VisitedList<_>()) (new VisitedList<_>()) current target
    let mergeTuplesImpl current target = mergeTuples (new VisitedList<_>()) (new VisitedList<_>()) current target
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
