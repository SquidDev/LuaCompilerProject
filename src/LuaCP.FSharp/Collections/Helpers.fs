namespace LuaCP.Collections

open System

module ListX = 
    let rec private collectGen f xs acc = 
        match xs with
        | [] -> Some(List.rev acc)
        | h :: t -> 
            match f h with
            | None -> None
            | Some x -> collectGen f xs (x :: acc)
    
    /// <summary>Map each item in the list. If any returns None then return None</summary>
    [<CompiledName("ChooseAll")>]
    let chooseAll f l = collectGen f l []
