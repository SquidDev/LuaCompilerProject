namespace LuaCP.Collections

open System

module Seq = 
    let foldAbort<'T, 'State, 'Choice> f (x : 'State) (source : seq<'T>) : 'Choice option * 'State = 
        use e = source.GetEnumerator()
        let mutable res : 'Choice option = None 
        let mutable state = x
        let f = OptimizedClosures.FSharpFunc<_,_,_>.Adapt(f)
        while (Option.isNone res && e.MoveNext()) do
            let result, other = f.Invoke(state, e.Current)
            res <- result
            state <- other
        res, state
        
module Set = 
    let of2 a b = Set.empty |> Set.add a |> Set.add b
    let of3 a b c = Set.empty |> Set.add a |> Set.add b |> Set.add c