module LuaCP.Collections.Matching
open System

let inline (|IdentRef|) (ref : IdentRef<'t>) = ref.Value
