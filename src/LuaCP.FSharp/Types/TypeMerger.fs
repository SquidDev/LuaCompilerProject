namespace LuaCP.Types

open System
open System.Text
open System.Collections.Generic
open LuaCP.Collections
open LuaCP.Collections.Matching
open LuaCP.Types
open LuaCP.Types.Extensions

type EquateMode = 
    | Equal = 0
    | Minimum = 1
    | Maximum = 2

[<StructuredFormatDisplay("{AsString}")>]
type TypeConstraint<'t>(ty : 't, merge : EquateMode -> 't -> 't -> 't) = 
    let mutable minimum : 't option = None
    let mutable maximum : 't option = None
    let subtypes = new HashSet<'t>()
    let supertypes = new HashSet<'t>()
    let equal = new HashSet<'t>()
    member this.Type = ty
    member this.Equal = equal
    member this.Minimum = minimum
    member this.Maximum = maximum
    
    member this.AddSubtype ty = 
        if subtypes.Add ty then 
            match minimum with
            | None -> minimum <- Some ty
            | Some min -> 
                printfn "Maximum of %A and %A" ty min
                minimum <- Some(merge (EquateMode.Maximum) ty min)
        minimum.Value
    
    member this.AddSupertype ty = 
        if supertypes.Add ty then 
            match minimum with
            | None -> maximum <- Some ty
            | Some max -> 
                printfn "Minimum of %A and %A" ty min
                maximum <- Some(merge (EquateMode.Minimum) ty max)
        maximum.Value
    
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
    
    let opposite (mode : EquateMode) = 
        match mode with
        | EquateMode.Equal -> EquateMode.Equal
        | EquateMode.Minimum -> EquateMode.Maximum
        | EquateMode.Maximum -> EquateMode.Minimum
    
    member this.ValueGet ref = values.[ref]
    member this.TupleGet ref = tuples.[ref]
    
    member this.ValueNew() = 
        let ref = new IdentRef<_>(Unbound)
        let cons = new TypeConstraint<_>(Reference ref, this.Value)
        values.Add(ref, cons)
        cons
    
    member this.TupleNew() = 
        let ref = new IdentRef<_>(Unbound)
        let cons = new TypeConstraint<_>(TReference ref, this.Tuple)
        tuples.Add(ref, cons)
        cons
    
    member this.Value (mode : EquateMode) (a : ValueType) (b : ValueType) : ValueType = 
        let a = a.Root
        let b = b.Root
        if a = b then a
        else 
            printfn "Finding %A of %A and %A" mode a b
            match a, b with
            | Literal lit, Primitive kind | Primitive kind, Literal lit -> 
                match mode with
                | EquateMode.Equal -> raise (Exception(sprintf "Cannot equate %A and %A" a b))
                | EquateMode.Minimum -> 
                    if TypeProvider.IsPrimitiveSubtype lit.Kind kind then Primitive kind
                    else raise (Exception(sprintf "Cannot assign %A to %A" lit kind))
                | EquateMode.Maximum -> 
                    if TypeProvider.IsPrimitiveSubtype lit.Kind kind then Literal lit
                    else raise (Exception(sprintf "Cannot assign %A to %A" lit kind))
            | Primitive a, Primitive b -> 
                match mode with
                | EquateMode.Equal -> raise (Exception(sprintf "Cannot equate %A and %A" a b))
                | EquateMode.Minimum -> 
                    if TypeProvider.IsPrimitiveSubtype a b then Primitive b
                    else raise (Exception(sprintf "Cannot assign %A to %A" a b))
                | EquateMode.Maximum -> 
                    if TypeProvider.IsPrimitiveSubtype a b then Primitive b
                    else raise (Exception(sprintf "Cannot assign %A to %A" a b))
            | Reference(IdentRef(Unbound)), Reference(IdentRef(Unbound)) -> a
            | Reference(IdentRef(Unbound) as tRef), ty | ty, Reference(IdentRef(Unbound) as tRef) -> 
                // This is technically incorrect
                match mode with
                | EquateMode.Equal -> 
                    tRef.Value <- Link ty
                    ty
                | EquateMode.Minimum -> (this.ValueGet tRef).AddSupertype ty
                | EquateMode.Maximum -> (this.ValueGet tRef).AddSubtype ty
            | Function(aArgs, aRet), Function(bArgs, bRet) -> 
                Function(this.Tuple mode aArgs bArgs, this.Tuple (opposite mode) aArgs bArgs)
            | Table(aFields, aOps), Table(bFields, bOps) -> 
                let convertPair a b = 
                    { Key = this.Value EquateMode.Equal a.Key b.Key
                      Value = this.Value mode a.Value b.Value
                      ReadOnly = a.ReadOnly || b.ReadOnly }
                
                let convert (_, field) = Seq.skip 1 field |> Seq.fold convertPair (Seq.head field)
                
                let fields = 
                    Seq.concat [ aFields; bFields ]
                    |> Seq.groupBy (fun x -> x.Key)
                    |> Seq.map convert
                    |> Seq.toList
                
                let ops = Array.map2 (this.Value mode) aOps bOps
                Table(fields, ops)
            | Reference(_), _ | _, Reference(_) -> 
                raise (Exception(sprintf "Unexpected state intersecting %A and %A" a b))
            | _, _ -> 
                printfn "TODO: %A and %A" a b
                a
    
    member this.Tuple (mode : EquateMode) (a : TupleType) (b : TupleType) = 
        let a = a.Root
        let b = b.Root
        if a = b then a
        else 
            printfn "Finding %A of %A and %A" mode a b
            match a, b with
            | TReference(IdentRef(Unbound)), TReference(IdentRef(Unbound)) -> a
            | TReference(IdentRef(Unbound) as tRef), ty | ty, TReference(IdentRef(Unbound) as tRef) -> 
                // This is technically incorrect
                match mode with
                | EquateMode.Equal -> 
                    tRef.Value <- Link ty
                    ty
                | EquateMode.Minimum -> (this.TupleGet tRef).AddSupertype ty
                | EquateMode.Maximum -> (this.TupleGet tRef).AddSubtype ty
            | Single(aArgs, aRem), Single(bArgs, bRem) -> 
                let args = List.map2 (this.Value mode) aArgs bArgs
                
                let rem = 
                    match aRem, bRem with
                    | Some aRem, Some bRem -> Some(this.Value mode aRem bRem)
                    | None, None -> None
                    | Some rem, None | None, Some rem -> 
                        match mode with
                        | EquateMode.Equal -> raise (Exception(sprintf "Cannot intersect %A and %A" a b))
                        | EquateMode.Maximum -> Some rem
                        | EquateMode.Minimum -> None
                Single(args, rem)
            | TReference(_), _ | _, TReference(_) -> 
                raise (Exception(sprintf "Unexpected state intersecting %A and %A" a b))