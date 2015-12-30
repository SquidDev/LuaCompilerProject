namespace LuaCP.Parser.Extensions

open System
open FParsec
open LuaCP.Utils
open LuaCP.Parser.Parsers
open LuaCP.Parser.Primitives
open LuaCP.Parser.Pattern
open LuaCP.Parser
open LuaCP.IR.Instructions
open LuaCP.Tree
open LuaCP.Tree.Expression
open System.Collections.Generic

type Adt(lang : Language) = 
    let literal = lang.Get(fun x -> new Literal(x))
    let expr = lang.Get(fun x -> new Expression(x))
    let choices, choicesRef = longestChoiceL [ literal.Boolean; literal.Float; literal.String; literal.Table ] "value"
    
    let adt = 
        let start = Symbol "`" >>. IdentifierBase
        
        let builder (x : string) (y : option<IValueNode>) = 
            let key = Nodes.TablePair (Nodes.String "tag") (Nodes.String x)
            match y with
            | Some(:? TableNode as x) -> Nodes.Table(Seq.append x.Items [ key ])
            | Some(y) -> 
                Nodes.Table [ key
                              Nodes.TableItem y ]
            | None -> Nodes.Table [ key ]
        pipe2 start (choices |> opt) builder
    
    do expr.PrimaryRef.Parsers.Add(adt)
    member val Choices = choices
    member val ChoicesRef = choicesRef
    member val Adt = adt

type Lambda(lang : Language) = 
    let func = lang.Get(fun x -> new FunctionDef(x))
    let extract (x : IDeclarable) = x.Name
    let wholeArgs = betweenL (Symbol "(") (Symbol ")") func.Arguments "lambda arguments"
    let args = wholeArgs <|> (lang.Declaration |>> (fun x -> [ x ], false))
    
    let builder x expr = 
        let (args, dots) = x
        Nodes.Function (Seq.map extract args) dots (Nodes.Return [ expr ])
    
    let lambda = pipe2 (args .>> Symbol "=>") lang.Expression builder
    do lang.Get(fun x -> new Expression(x)).PrimaryRef.Parsers.Add(lambda)
    member val Lambda = lambda

type OpEquals(lang : Language) = 
    let opAssign (op : IValueNode -> IValueNode -> IValueNode) (left : list<IAssignable>) (right : list<IValueNode>) = 
        // TODO: Replace x.y.z += 1 with tmp = x.y; tmp.z = tmp.z + 1
        if left.Length <> right.Length then raise (ArgumentException("Assymetric operator"))
        else Nodes.Assign left (List.map2 op left right)
    
    do 
        let assign = lang.Get(fun x -> new Statement(x)).Assignments.Parsers
        let useful = Set.ofList [ ".."; "+"; "-"; "*"; "/"; "%"; "^"; "&"; "|"; "~"; "//"; "<<"; ">>" ]
        for (symbol, builder, _, _) in Operators.Binary do
            if useful.Contains symbol then assign.Add(Symbol("`" + symbol) >>% (opAssign builder))