module LuaCP.Utils

open System

let IdentifierStart x = x = '_' || Char.IsLetter x
let IdentifierRemaining x = x = '_' || Char.IsLetterOrDigit x
let IsValidIdentifier(x : string) = 
    x.Length > 0 && IdentifierStart(x.Chars 0) && (Seq.skip 1 x |> Seq.forall IdentifierRemaining)

let split (n : int) (list : list<'t>) = 
    let rec splitter n remaining acc = 
        match remaining with
        | [] -> List.rev acc, []
        | _ when n = 0 -> List.rev acc, remaining
        | head :: tail -> splitter (n - 1) tail (head :: acc)
    splitter n list []

type List<'T> with
    
    /// Repeat each element of the sequence n times
    member this.Last = this.Item(this.Length - 1)
    
    member this.Split n = split n this
