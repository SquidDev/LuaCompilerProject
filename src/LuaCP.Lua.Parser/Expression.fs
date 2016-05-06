namespace LuaCP.Parser

open System
open FParsec
open LuaCP
open LuaCP.Tree
open LuaCP.Parser.Parsers
open LuaCP.Parser.Primitives
open LuaCP.Parser.Extensions
open LuaCP.Parser.Nodes
open LuaCP.IR.Instructions

type Expression(lang : Language) = 
    let func = lang.Get(fun x -> new FunctionDef(x))
    let literal = lang.Get(fun x -> new Literal(x))
    let funcExpr = Keyword "function" >>. func.Body
    
    let primary, primaryRef = 
        longestChoiceL [ literal.Nil
                         literal.Float
                         literal.Boolean
                         literal.String
                         literal.Table
                         literal.Dots
                         funcExpr
                         betweenL (Symbol "(") (Symbol ")") lang.Expression "parens" |>> Parens
                         lang.Identifier |>> fun x -> upcast x ] "primary expression"
    
    let singleton x = [ x ]
    
    let arguments, argumentsRef = 
        longestChoiceL [ betweenL (Symbol "(") (Symbol ")") lang.ExprList "arguments"
                         literal.String |>> singleton
                         literal.Table |>> singleton
                         literal.Boolean |>> singleton ] "arguments"
    
    let suffixes, suffix, suffixRef = 
        let index = 
            (betweenL (Symbol "[") (Symbol "]") lang.Expression "indexer") 
            <|> (JustSymbol "." >>. IdentifierBase |>> Nodes.String) <?> "indexer"
        let invoke = Symbol ":" >>. IdentifierBase .>>. arguments <?> "invoke"
        
        let makeInvoke (x : string * list<IValueNode>) func = 
            let (name, args) = x
            Nodes.Invoke func name args
        
        let makeIndex indexer expr : IValueNode = upcast Nodes.Index expr indexer
        let makeCall args expr = Nodes.Call expr args
        
        // We have to use a monad style syntax here otherwise we can't preserve the value
        let suffixBuilder, suffixBuilderRef = 
            longestChoiceL [ index |>> makeIndex
                             invoke |>> makeInvoke
                             arguments |>> makeCall ] "call"
        
        let suffixOpt = suffixBuilder |> opt
        
        let suffixExtract expr sub = 
            match sub with
            | Some(x) -> x (expr)
            | None -> expr
        
        let suffix expr = suffixOpt |>> suffixExtract expr
        (chain primary suffix) |> withPosition, suffix, suffixBuilderRef
    
    do 
        let expr = lang.ExpressionRef
        expr.TermParser <- suffixes
        let inverseBin op right left = Nodes.BinOp op left right
        // Add operators
        for (symbol, builder, precedence, right) in Operators.Binary do
            let whitespace = 
                if symbol.Chars 0 |> Char.IsLetter then KWhitespace
                else Whitespace
            
            let associtivity = 
                if right then Associativity.Right
                else Associativity.Left
            
            expr.AddOperator(InfixOperator(symbol, whitespace, precedence, associtivity, builder))
        for (symbol, builder, precedence) in Operators.Unary do
            let whitespace = 
                if symbol.Chars 0 |> Char.IsLetter then KWhitespace
                else Whitespace
            expr.AddOperator(PrefixOperator(symbol, whitespace, precedence, true, builder))
    
    member val Arguments = arguments
    member val ArgumentsRef = argumentsRef
    member val Primary = primary
    member val PrimaryRef = primaryRef
    member val Suffix = suffix
    member val SuffixRef = suffixRef
