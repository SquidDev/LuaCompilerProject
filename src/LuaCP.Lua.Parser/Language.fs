namespace LuaCP.Parser

open System
open LuaCP
open LuaCP.Collections
open LuaCP.Parser
open LuaCP.Parser.Parsers
open LuaCP.Parser.Primitives
open LuaCP.Tree
open LuaCP.Parser.Extensions
open FParsec

type Language() as this = 
    let dict = new TypeDictionary<Language>(this)
    let declaration = 
        new NamedReference<IDeclarable, unit>("declaration", IdentifierBase |>> (fun x -> upcast Nodes.Identifier x))
    let expression = new OperatorPrecedenceParser<IValueNode, unit, unit>()
    let expressionNamed = expression.ExpressionParser <?> "expression"
    let statement = new LongestMatch<INode, unit>("statement")
    let statementLine = statement.AsParser
                        |> withPosition
                        .>> (Symbol ";" |> optional)
    // Declaration
    member val DeclarationList = sepBy declaration.AsParser (Symbol ",") <?> "declaration list"
    member val DeclarationList1 = sepBy declaration.AsParser (Symbol ",") <?> "declaration list"
    
    member this.Declaration 
        with get () = declaration.AsParser |> withPosition
        and set (value) = declaration.Parser <- value
    
    // Expression 
    member val ExpressionRef = expression
    member val Expression = expressionNamed |> withPosition
    member val ExprList = sepBy expressionNamed (Symbol ",") <?> "expression list"
    member val ExprList1 = sepBy expressionNamed (Symbol ",") <?> "expression list"
    member val Identifier = IdentifierBase |>> Nodes.Identifier
                            |> withPosition
                            <?> "identifier"
    // Statement
    member val StatementRef = statement
    member val Statement = statement.AsParser |> withPosition
    member val StatementLine = statementLine
    member val Chunk = many statementLine |>> Nodes.Block
    member this.ChunkTill x = manyTill statementLine x |>> Nodes.Block
    member val ChunkTillEnd = manyTill statementLine (Keyword "end") |>> Nodes.Block |> withPosition
    member this.Source(x : Parser<_, _>) = Primitives.Whitespace >>. x .>> eof
    member this.Script = this.Source this.Chunk
    member this.Get x = dict.Get x
