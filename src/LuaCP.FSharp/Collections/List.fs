module LuaCP.Collections.ListHelpers

open System

type List<'T> with
    member this.Last = this.Item(this.Length - 1)
