namespace LuaCP.Types

open System
open LuaCP.Collections.Matching
open LuaCP.Types
open LuaCP.Types.Extensions

type EquateMode = 
    | Equal = 0
    | Minimum = 1
    | Maximum = 2

type TypeEquator() = 
    
    let opposite (mode : EquateMode) = 
        match mode with
        | EquateMode.Equal -> EquateMode.Equal
        | EquateMode.Minimum -> EquateMode.Maximum
        | EquateMode.Maximum -> EquateMode.Minimum
    
    member this.Value (mode : EquateMode) (a : ValueType) (b : ValueType) : ValueType = 
        let a = a.Root
        let b = b.Root
        if a = b then a
        else 
            printfn "Intersecting %A and %A" a b
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
                tRef.Value <- Link ty
                ty
            | Function(aArgs, aRet), Function(bArgs, bRet) -> 
                Function(this.Tuple mode aArgs bArgs, this.Tuple (opposite mode) aArgs bArgs)
            | Table(aFields, aOps), Table(bFields, bOps) -> 
                let convertPair a b = 
                    { Key = this.Value EquateMode.Equal a.Key b.Key
                      Value = this.Value mode a.Key b.Key
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
            printfn "Intersecting %A and %A" a b
            match a, b with
            | TReference(IdentRef(Unbound)), TReference(IdentRef(Unbound)) -> a
            | TReference(IdentRef(Unbound) as tRef), ty | ty, TReference(IdentRef(Unbound) as tRef) -> 
                tRef.Value <- Link ty
                ty
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