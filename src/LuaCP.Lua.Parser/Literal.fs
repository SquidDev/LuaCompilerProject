namespace LuaCP.Parser

open System
open FParsec
open LuaCP.Parser.Parsers
open LuaCP.Parser.Primitives
open LuaCP.Parser
open LuaCP
open LuaCP.Tree
open LuaCP.Tree.Expression
open LuaCP.Lua.Tree
open LuaCP.Lua.Tree.Expression
open LuaCP.IR

type Literal(lang : Language) = 
    
    let stringVal = 
        let escapeNumber = 
            manyMinMaxSatisfy 1 3 Char.IsDigit 
            |>> (fun x -> Byte.Parse x |> Convert.ToChar)
        
        let escapeSequence = 
            anyChar |>> function 
            | 'n' -> '\n'
            | 'r' -> '\r'
            | 't' -> '\t'
            | c -> c // every other char is mapped to itself
        
        let escape = 
            pchar '\\' >>. (escapeSequence <|> escapeNumber) 
            <?> "escape sequence"
        let string x = 
            betweenL (pchar x) (pchar x) (manyChars (escape <|> noneOf [ x ])) 
                "string"
        let allStrings = string '"' <|> string '\'' <|> LongString // " -- Reset because MonoDevelop highlighting
        allStrings |> Token
    
    let string = stringVal |>> Nodes.String
    
    let table = 
        let tableAssign = Symbol "=" >>. lang.Expression
        let makePair x y = new TableNode.TableItem(x, y)
        // If we start with [ then we must be a key value pair
        let pair = 
            pipe2 
                (betweenL (Symbol "[") (Symbol "]") lang.Expression "table key") 
                tableAssign makePair
        
        // If we are an identifier then we are either <String = Expr> or <Expr>
        let identifier = 
            pipe2 IdentifierBase (opt tableAssign) (fun k v -> 
                match v with
                | Some(v) -> makePair (Nodes.String k) v
                | None -> new TableNode.TableItem(new IdentifierNode(k)))
        
        let tableItem = 
            pair <|> identifier 
            <|> (lang.Expression |>> fun x -> new TableNode.TableItem(x))
        let factory x : IValueNode = upcast new TableNode(x)
        betweenL (Symbol "{") (Symbol "}") 
            (sepEndBy tableItem (Symbol "," <|> Symbol ";") |>> factory) "table"
    
    let floatVal = pfloat |> Token
    let float = floatVal |>> Nodes.ChooseNumber
    let booleanVal = (stringReturn "true" true) <|> (stringReturn "false" false)
                     |> Token
                     <?> "boolean"
    let boolean = booleanVal |>> Nodes.Boolean
    member val String : Parser<IValueNode> = string |> withPosition
    member val StringVal : Parser<string> = stringVal
    member val Float : Parser<IValueNode> = float |> withPosition
    member val FloatVal : Parser<double> = floatVal
    member val Boolean : Parser<IValueNode> = boolean |> withPosition
    member val BooleanVal : Parser<bool> = booleanVal
    member val Table : Parser<IValueNode> = table |> withPosition
    member val Constant = longestChoiceL [ string; float; boolean ] "constant"
    
    member val Nil : Parser<IValueNode> = stringReturn "nil" Nodes.Nil
                                          |> withPosition
                                          |> Token
    
    member val Dots : Parser<IValueNode> = stringReturn "..." Nodes.Dots
                                           |> withPosition
                                           |> Token