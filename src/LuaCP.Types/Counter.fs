namespace LuaCP.Types

open System

type Counter() = 
    let mutable id = 0
    member this.Next() = 
        let current = id
        id <- id + 1
        current