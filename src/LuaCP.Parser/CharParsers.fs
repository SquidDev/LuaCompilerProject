module LuaCP.Parser.CharParsers

open System
open LuaCP.Parser

type private SingleChar = 
    
    interface TerminalPredicate<char> with
        member this.Matches other = other = this.Char
    
    val Char : char
    override this.ToString() = sprintf "%A" this.Char
    override this.GetHashCode() = this.Char.GetHashCode()
    
    override this.Equals other = 
        match other with
        | :? SingleChar as other -> other.Char = this.Char
        | _ -> false
    
    new(ch : char) = { Char = ch }

type private Range = 
    
    interface TerminalPredicate<char> with
        member this.Matches other = other >= this.Min && other <= this.Max
    
    val Min : char
    val Max : char
    override this.ToString() = sprintf "%A-%A" this.Min this.Max
    override this.GetHashCode() = this.Max.GetHashCode() * 256 + this.Min.GetHashCode()
    
    override this.Equals other = 
        match other with
        | :? Range as other -> other.Min = this.Min && other.Max = this.Max
        | _ -> false
    
    new(min : char, max : char) = 
        { Min = min
          Max = max }

type private Choice = 
    
    interface TerminalPredicate<char> with
        member this.Matches other = this.Chars.IndexOf other >= 0
    
    val Chars : string
    override this.ToString() = sprintf "[%s]" this.Chars
    override this.GetHashCode() = this.Chars.GetHashCode()
    
    override this.Equals other = 
        match other with
        | :? Choice as other -> other.Chars = this.Chars
        | _ -> false
    
    new(chars : string) = { Chars = chars }

let char x = new SingleChar(x) :> TerminalPredicate<char> |> Terminal
let range min max = new Range(min, max) :> TerminalPredicate<char> |> Terminal
let string string = Seq.map char string |> Seq.toArray
let anyOf string = new Choice(string) :> TerminalPredicate<char> |> Terminal
