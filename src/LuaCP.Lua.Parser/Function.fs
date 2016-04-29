namespace LuaCP.Parser

open System
open FParsec
open LuaCP
open LuaCP.Tree
open LuaCP.Parser.Parsers
open LuaCP.Parser.Primitives
open LuaCP.Parser.Extensions

type FunctionDef(lang : Language) = 
    let expr = lang.Get(fun x -> new Literal(x))
    
    let arguments = 
        let args = 
            (expr.Dots |>> (fun x -> List.empty, true)) 
            <|> (lang.DeclarationList1 .>>. (Symbol "," >>. expr.Dots |> bOpt))
        refL "function args" args
    
    let header = refL "function header" (betweenL (Symbol "(") (Symbol ")") arguments.AsParser "function header")
    
    let body = 
        pipe2 header.AsParser lang.ChunkTillEnd (fun x body -> 
            let (args : list<IDeclarable>, dots) = x
            let extract (x : IDeclarable) = x.Name
            Nodes.Function (Seq.map extract args) dots body)
        |> refL "function body"
    
    let name = 
        let rec chain (x : list<string>) : IValueNode = 
            match x with
            | [] -> raise (ArgumentException("Empty list"))
            | [ item ] -> upcast Nodes.Identifier item
            | head :: rest -> upcast Nodes.Index (chain rest) (Nodes.String head)
        
        let ident = sepBy1 IdentifierBase (Symbol ".") |>> (fun items -> List.rev items |> chain)
        
        let name : Parser<IValueNode * bool> = 
            pipe2 ident (Symbol ":" >>. IdentifierBase |> opt) (fun idents indexer -> 
                match indexer with
                | None -> idents, false
                | Some(value) -> upcast Nodes.Index idents (Nodes.String value), true)
        refL "function name" name
    
    member this.Arguments 
        with get () = arguments.AsParser
        and set (value) = arguments.Parser <- value
    
    member this.Header 
        with get () = header.AsParser
        and set (value) = header.Parser <- value
    
    member this.Body 
        with get () = body.AsParser
        and set (value) = body.Parser <- value
    
    member this.Name 
        with get () = name.AsParser
        and set (value) = name.Parser <- value