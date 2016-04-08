namespace LuaCP.Types

open System
open LuaCP.Collections.Matching
open LuaCP.Types
open LuaCP.Types.Extensions

type TypeEquator(checker : TypeProvider) = 
    
    member this.Value (a : ValueType) (b : ValueType) = 
        let a = a.Root
        let b = b.Root
        if a <> b then 
            printfn "Intersecting %A and %A" a b
            match a, b with
            | Reference(IdentRef(Unbound)), Reference(IdentRef(Unbound)) -> ()
            | Reference(IdentRef(Unbound) as tRef), ty | ty, Reference(IdentRef(Unbound) as tRef) -> 
                tRef.Value <- Link ty
            | Function(aArgs, aRet), Function(bArgs, bRet) -> 
                this.Tuple aArgs bArgs
                this.Tuple aArgs bArgs
            | Table(aFields, aOps), Table(bFields, bOps) -> 
                let find (a : ValueType) (b : TableField) = checker.IsTypeEqual a b.Key
                let visited = new Collections.Generic.HashSet<ValueType>(LuaCP.Collections.IdentityComparer.Instance)
                
                let merge aFields bFields = 
                    for field in aFields do
                        if visited.Add field.Key && not field.HasUnbound then 
                            match List.tryFind (fun x -> checker.IsTypeEqual field.Key x.Key) bFields with
                            | Some pair -> this.Value field.Value pair.Value
                            | None -> () // TODO: Return merged tables
                merge aFields bFields
                merge bFields aFields
                Array.iter2 this.Value aOps bOps
            | Reference(_), _ | _, Reference(_) -> 
                raise (Exception(sprintf "Unexpected state intersecting %A and %A" a b))
            | _, _ -> printfn "TODO: %A and %A" a b
    
    member this.Tuple (a : TupleType) (b : TupleType) = 
        let a = a.Root
        let b = b.Root
        if a = b then 
            printfn "Intersecting %A and %A" a b
            match a, b with
            | TReference(IdentRef(Unbound)), TReference(IdentRef(Unbound)) -> ()
            | TReference(IdentRef(Unbound) as tRef), ty | ty, TReference(IdentRef(Unbound) as tRef) -> 
                tRef.Value <- Link ty
            | Single(aArgs, aRem), Single(bArgs, bRem) -> 
                List.iter2 this.Value aArgs bArgs
                match aRem, bRem with
                | Some aRem, Some bRem -> this.Value aRem bRem
                | _, _ -> raise (Exception(sprintf "Cannot intersect %A and %A" a b))
            | TReference(_), _ | _, TReference(_) -> 
                raise (Exception(sprintf "Unexpected state intersecting %A and %A" a b))