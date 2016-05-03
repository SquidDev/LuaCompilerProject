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

module TypeBounds = 
    let opposite (mode : BoundMode) = 
        match mode with
        | BoundMode.Equal -> BoundMode.Equal
        | BoundMode.Minimum -> BoundMode.Maximum
        | BoundMode.Maximum -> BoundMode.Minimum
    
    let rec Value (mode : BoundMode) (a : ValueType) (b : ValueType) : ValueType = 
        let a = a.Root
        let b = b.Root
        if a = b then a
        else 
            printfn "Finding %A of %A and %A" mode a b
            match a, b with
            | Literal lit, Primitive kind | Primitive kind, Literal lit -> 
                match mode with
                | BoundMode.Equal -> raise (Exception(sprintf "Cannot equate %A and %A" a b))
                | BoundMode.Minimum -> 
                    if TypeProvider.IsPrimitiveSubtype lit.Kind kind then Primitive kind
                    else raise (Exception(sprintf "Cannot assign %A to %A" lit kind))
                | BoundMode.Maximum -> 
                    if TypeProvider.IsPrimitiveSubtype lit.Kind kind then Literal lit
                    else raise (Exception(sprintf "Cannot assign %A to %A" lit kind))
            | Primitive a, Primitive b -> 
                match mode with
                | BoundMode.Equal -> raise (Exception(sprintf "Cannot equate %A and %A" a b))
                | BoundMode.Minimum -> 
                    if TypeProvider.IsPrimitiveSubtype a b then Primitive a
                    elif TypeProvider.IsPrimitiveSubtype b a then Primitive b
                    else raise (Exception(sprintf "Cannot merge %A and %A" a b))
                | BoundMode.Maximum -> 
                    if TypeProvider.IsPrimitiveSubtype a b then Primitive b
                    elif TypeProvider.IsPrimitiveSubtype b a then Primitive a
                    else raise (Exception(sprintf "Cannot merge %A and %A" a b))
            | Reference(IdentRef(Unbound) as tRef), ty | ty, Reference(IdentRef(Unbound) as tRef) -> 
                match mode with
                | BoundMode.Equal -> 
                    tRef.Value <- Link ty
                    ty
                | BoundMode.Minimum -> Union [ a; b ]
                | BoundMode.Maximum -> Intersection [ a; b ]
            | Function(aArgs, aRet), Function(bArgs, bRet) -> 
                Function(Tuple mode aArgs bArgs, Tuple (opposite mode) aArgs bArgs)
            | Table(aFields, aOps), Table(bFields, bOps) -> 
                // TODO: Actually do modes correctly
                let convertPair a b = 
                    { Key = Value BoundMode.Equal a.Key b.Key
                      Value = Value mode a.Value b.Value
                      ReadOnly = a.ReadOnly || b.ReadOnly }
                
                let convert (_, field) = Seq.skip 1 field |> Seq.fold convertPair (Seq.head field)
                
                let fields = 
                    Seq.concat [ aFields; bFields ]
                    |> Seq.groupBy (fun x -> x.Key)
                    |> Seq.map convert
                    |> Seq.toList
                
                let ops = Array.map2 (Value mode) aOps bOps
                Table(fields, ops)
            | Reference(_), _ | _, Reference(_) -> 
                raise (Exception(sprintf "Unexpected state intersecting %A and %A" a b))
            | _, _ -> 
                printfn "TODO: %A and %A" a b
                a
    
    and Tuple (mode : BoundMode) (a : TupleType) (b : TupleType) = 
        let a = a.Root
        let b = b.Root
        if a = b then a
        else 
            printfn "Finding %A of %A and %A" mode a b
            match a, b with
            | TReference(IdentRef(Unbound) as tRef), ty | ty, TReference(IdentRef(Unbound) as tRef) -> 
                match mode with
                | BoundMode.Equal -> 
                    tRef.Value <- Link ty
                    ty
                | _ -> 
                    printfn "Cannot merge %A and %A" a b
                    ty
            | Single(aArgs, aRem), Single(bArgs, bRem) -> 
                let args = List.map2 (Value mode) aArgs bArgs
                
                let rem = 
                    match aRem, bRem with
                    | Some aRem, Some bRem -> Some(Value mode aRem bRem)
                    | None, None -> None
                    | Some rem, None | None, Some rem -> 
                        match mode with
                        | BoundMode.Equal -> raise (Exception(sprintf "Cannot intersect %A and %A" a b))
                        | BoundMode.Maximum -> Some rem
                        | BoundMode.Minimum -> None
                Single(args, rem)
            | TReference(_), _ | _, TReference(_) -> 
                raise (Exception(sprintf "Unexpected state intersecting %A and %A" a b))

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
                | Some min -> 
                    printfn "Maximum of %A and %A" sub min
                    bound (BoundMode.Maximum) sub min
            minimum <- Some min
            merge ty min
            match maximum with
            | Some max -> merge max min
            | None -> ()
        minimum.Value
    
    member this.AddSupertype sup = 
        if supertypes.Add sup then 
            let max = 
                match minimum with
                | None -> sup
                | Some max -> 
                    printfn "Minimum of %A and %A" sup min
                    bound (BoundMode.Minimum) sup max
            maximum <- Some max
            merge max ty
            match minimum  with
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

and TypeMerger() = 
    let values = new RefLookup<ValueType>()
    let tuples = new RefLookup<TupleType>()
    let provider = new TypeProvider()
    
    let rec mergeValues (current : ValueType) (target : ValueType) = 
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
                raise (Exception(sprintf "Unexpected state merging %A :> %A" current target))
            | (Nil | Value | Dynamic | Primitive _ | Literal _), (Nil | Value | Dynamic | Primitive _ | Literal _) -> 
                if not (provider.IsSubtype current target) then 
                    raise (Exception(sprintf "Cannot cast %A to %A" current target))
            | Table(currentFields, currentOps), Table(targetFields, targetOps) -> 
                for targetField in targetFields do
                    match List.tryFind (fun x -> x.Key = targetField.Key) currentFields with
                    | Some currentField -> mergeValues currentField.Value targetField.Value
                    | None -> printfn "Missing field %A in %A" targetField.Key current
            | Function(currentA, currentR), Function(targetA, targetR) ->
                mergeTuples targetA currentA
                mergeTuples currentR targetR
            | _, _ -> printfn "Skipping %A :> %A" current target // TODO: Implement other types
    
    and mergeTuples (current : TupleType) (target : TupleType) = 
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
                raise (Exception(sprintf "Unexpected state merging %A :> %A" current target))
            | Single(_, _), Single(_, _) -> printfn "Skipping %A :> %A" current target // TODO: Implement normal merging
    
    member this.ValueGet ref = values.[ref]
    member this.TupleGet ref = tuples.[ref]
    
    member this.ValueNew() = 
        let ref = new IdentRef<_>(Unbound)
        let cons = new TypeConstraint<_>(Reference ref, TypeBounds.Value, mergeValues)
        values.Add(ref, cons)
        cons
    
    member this.TupleNew() = 
        let ref = new IdentRef<_>(Unbound)
        let cons = new TypeConstraint<_>(TReference ref, TypeBounds.Tuple, mergeTuples)
        tuples.Add(ref, cons)
        cons
    
    member this.Value = TypeBounds.Value
    member this.MergeValues = mergeValues
    member this.Tuple = TypeBounds.Tuple
    member this.MergeTuples = mergeTuples
    member this.Bake() = 
        let bindV (cons : TypeConstraint<_>) tRef (ty : ValueType) existing = 
            match ty.Root with
                | Reference(IdentRef(Unbound) as oRef) when oRef = tRef -> existing
                | ty ->
                    printfn "Linking %A <- %A" (Reference tRef) ty
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
                printfn "Linking %A <- %A" (TReference tRef) ty
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
