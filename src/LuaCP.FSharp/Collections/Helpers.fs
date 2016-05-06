namespace LuaCP.Collections

open System
open System.Collections.Generic

module Seq = 
    let foldAbort<'T, 'State, 'Choice> f (x : 'State) (source : seq<'T>) : 'Choice option * 'State = 
        use e = source.GetEnumerator()
        let mutable res : 'Choice option = None
        let mutable state = x
        let f = OptimizedClosures.FSharpFunc<_, _, _>.Adapt(f)
        while (Option.isNone res && e.MoveNext()) do
            let result, other = f.Invoke(state, e.Current)
            res <- result
            state <- other
        res, state
    
    let groupBy2<'T, 'Key when 'Key : equality> (getKey : 'T -> 'Key) (a : seq<'T>) (b : seq<'T>) : seq<'Key * 'T List * 'T List> = 
        let dict = new Dictionary<_, List<'T> * List<'T>>()
        for v in a do
            let mutable prev = Unchecked.defaultof<_>
            let key = getKey v
            match dict.TryGetValue(key, &prev) with
            | true -> 
                let a, _ = prev
                a.Add v
            | false -> 
                let a, b = new List<_>(), new List<_>()
                dict.[key] <- (a, b)
                a.Add v
        for v in b do
            let mutable prev = Unchecked.defaultof<_>
            let key = getKey v
            match dict.TryGetValue(key, &prev) with
            | true -> 
                let _, b = prev
                b.Add v
            | false -> 
                let a, b = new List<_>(), new List<_>()
                dict.[key] <- (a, b)
                b.Add v
        dict |> Seq.map (fun pair -> 
                    let a, b = pair.Value
                    pair.Key, a, b)
    
    let foldHead<'T> f (source : seq<'T>) = 
        if source = null then invalidArg "source" "Source cannot be null"
        use e = source.GetEnumerator()
        let f = OptimizedClosures.FSharpFunc<_, _, _>.Adapt(f)
        if not (e.MoveNext()) then invalidArg "source" "Source cannot be empty"
        let mutable state = e.Current
        while e.MoveNext() do
            state <- f.Invoke(state, e.Current)
        state

module Set = 
    let of2 a b = 
        Set.empty
        |> Set.add a
        |> Set.add b
    
    let of3 a b c = 
        Set.empty
        |> Set.add a
        |> Set.add b
        |> Set.add c
