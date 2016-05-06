module LuaCP.Collections.Matching

open System
open System.Collections.Generic

let inline (|IdentRef|) (ref : IdentRef<'t>) = ref.Value
let inline (|EmptySet|_|) (set : ICollection<_>) = if set.Count = 0 then Some () else None
let inline (|SingleSet|_|) (set : ICollection<_>) = if set.Count = 1 then Seq.head set |> Some else None