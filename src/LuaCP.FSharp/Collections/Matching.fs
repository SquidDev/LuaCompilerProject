module LuaCP.Collections.Matching
open System

let inline (|IdentRef|) (ref : IdentRef<'t>) = ref.Value
let inline (|EmptySet|_|) (set : Set<_>) = if Set.isEmpty set then Some () else None
let inline (|SingleSet|_|) (set : Set<_>) = if Set.count set = 1 then Some set.MinimumElement else None