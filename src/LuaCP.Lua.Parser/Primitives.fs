module LuaCP.Parser.Primitives

open System
open FParsec
open LuaCP.Parser.Parsers
open LuaCP.Utils
open LuaCP.Parser.Extensions

let Keywords = 
    [ "true"; "false"; "nil"; "if"; "while"; "for"; "do"; "then"; "elseif"; "else"; "end"; "goto"; "return"; "and"; "or"; 
      "local"; "in"; "not"; "until" ]
let LongStringStart : Parser<int> = between (pstring "[") (pstring "[") (pstring "=" |> many) |>> fun x -> x.Length
let LongString = 
    LongStringStart >>= (fun x -> manyCharsTill anyChar (pstring ("]" + (String.replicate x "=") + "]"))) 
    <??> "long string"
let Comment = 
    pstring "--" >>. ((lookAhead LongStringStart >>. LongString) <|> (manyCharsTill anyChar newline)) <??> "comment"
let Whitespace = skipMany (spaces1 <|> (Comment >>% ())) <?> "whitespace"
let KWhitespace = nextCharSatisfiesNot isLetter
                  |> attempt
                  .>> Whitespace
let Token x = x .>> Whitespace

let IdentifierBase : Parser<string> = 
    let basic = 
        identifier (IdentifierOptions(isAsciiIdStart = IdentifierStart, isAsciiIdContinue = IdentifierRemaining))
    let lookup = new Set<_>(Keywords)
    blacklist lookup basic |> Token

let Keyword x = (skipString x .>> nextCharSatisfiesNot isLetter |> attempt) |> Token
let Symbol x = skipString x |> Token
let JustSymbol x = 
    (skipString x .>> nextCharSatisfies (fun x -> Char.IsWhiteSpace x || Char.IsLetterOrDigit x) |> attempt) |> Token