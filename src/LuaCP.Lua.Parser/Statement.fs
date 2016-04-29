namespace LuaCP.Parser

open System
open FParsec
open LuaCP
open LuaCP.Tree
open LuaCP.Tree.Expression
open LuaCP.Lua.Tree.Expression
open LuaCP.Parser.Parsers
open LuaCP.Parser.Primitives

type Statement(lang : Language) = 
    let chunkTillEnd = lang.ChunkTillEnd
    let chunkTill = lang.ChunkTill
    let doTilEnd = Keyword "do" >>. chunkTillEnd
    let doStmt = doTilEnd |>> Nodes.Do |> refL "do statement"
    let func = lang.Get(fun x -> new FunctionDef(x))
    
    let whileStmt = 
        let condition = betweenL (Keyword "while") (Keyword "do") lang.Expression "while condition"
        pipe2 condition chunkTillEnd Nodes.While |> refL "while statement"
    
    let repeatStmt = 
        let body = Keyword "repeat" >>. (chunkTill (Keyword "until"))
        pipe2 body lang.Expression Nodes.Repeat |> refL "repeat statement"
    
    let ifStmt = 
        let elseChunk = chunkTill (Keyword "elseif" <|> Keyword "else" <|> Keyword "end" |> lookAhead)
        let header = betweenL (Keyword "if") (Keyword "then") lang.Expression "if condition"
        let elseIf = betweenL (Keyword "elseif") (Keyword "then") lang.Expression "elseif condition" .>>. elseChunk
        let elses = Keyword "else" >>. chunkTill (Keyword "end" |> lookAhead)
        let whole = pipe4 header elseChunk (many elseIf) (opt elses) Nodes.IfElseIf .>> Keyword "end"
        refL "if statement" whole
    
    let breakStmt = Keyword "break" >>% Nodes.Break <?> "break statement"
    let goto = Keyword "goto" >>. IdentifierBase |>> Nodes.Goto <?> "goto statement"
    let label = betweenL (Symbol "::") (Symbol "::") IdentifierBase "label" |>> Nodes.Label
    let returnStmt = Keyword "return" >>. lang.ExprList |>> Nodes.Return
    
    let forNum = 
        let makeForNum ident start stop (step : option<IValueNode>) body = 
            let stepContents = 
                if step.IsSome then step.Value
                else Nodes.Number 1.0
            Nodes.ForNum ident start stop stepContents body
        
        let forNum = 
            pipe5 lang.Declaration (Symbol "=" >>. lang.Expression) (Symbol "," >>. lang.Expression) 
                (Symbol "," >>. lang.Expression |> opt) doTilEnd makeForNum
        Keyword "for" >>. forNum |> refL "for numeric statement"
    
    let forIn = 
        let forIn = pipe3 lang.DeclarationList1 (Symbol "in" >>. lang.ExprList1) doTilEnd Nodes.ForIn
        Keyword "for" >>. forIn |> refL "for in statement"
    
    let localStmt, localChoice = 
        let nameList = 
            pipe2 lang.DeclarationList1 (Symbol "=" >>. lang.ExprList1 |> opt) (fun ident vars -> 
                Nodes.Local ident (match vars with
                                   | None -> List.empty
                                   | Some(x) -> x))
        
        let func = 
            pipe2 (Keyword "function" >>. lang.Declaration) func.Body 
                (fun dec body -> Nodes.LocalRec [dec] [body])
        let localChoice, localChoiceRef = longestChoiceL [ func; nameList ] "local statement"
        Keyword "local" >>. localChoice <?> "local statement", localChoiceRef
    
    let funcStmt = 
        let parser = 
            pipe2 (Keyword "function" >>. func.Name) func.Body (fun name func -> 
                match name with
                | (expr, false) -> Nodes.Assign [expr :?> IAssignable] [func]
                | (expr, true) -> 
                    match func with
                    | :? Lua.Tree.Expression.FunctionNode as func -> 
                        Nodes.Assign [expr :?> IAssignable]
                            [Nodes.Function ("self" :: Seq.toList func.Arguments) func.Dots func.Body]
                    | _ -> raise (ArgumentException "Expected function"))
        refL "function statement" parser
    
    let assignments, assignmentsRef = 
        longestChoiceL [ Symbol "=" >>% (fun (l : list<IAssignable>) (r : list<IValueNode>) -> Nodes.Assign l r) ] 
            "assignment operator"
    let builder left b right = b (Seq.cast<IAssignable> left |> Seq.toList) right
    let assignStmt = pipe3 lang.ExprList1 assignments lang.ExprList1 builder |> refL "assign statement"
    
    let callStmt = 
        let validate (x : IValueNode) : Parser<INode> = 
            match x with
            | :? Lua.Tree.Expression.CallNode as func -> preturn (upcast func)
            | _ -> fail "Expected call statement"
        lang.Expression >>= validate |> refL "call statement"
    
    do 
        let statements = 
            [ doStmt.AsParser; repeatStmt.AsParser; whileStmt.AsParser; ifStmt.AsParser; forIn.AsParser; forNum.AsParser; 
              localStmt; goto; label; breakStmt; returnStmt; funcStmt.AsParser; assignStmt.AsParser; callStmt.AsParser ]
        for stmt in statements do
            lang.StatementRef.Parsers.Add(stmt)
    
    member val Break = breakStmt
    member val Goto = goto
    member val Label = label
    member val Return = returnStmt
    
    member this.Do 
        with get () = doStmt.AsParser
        and set (value) = doStmt.Parser <- value
    
    member this.Repeat 
        with get () = repeatStmt.AsParser
        and set (value) = repeatStmt.Parser <- value
    
    member this.While 
        with get () = whileStmt.AsParser
        and set (value) = whileStmt.Parser <- value
    
    member this.If 
        with get () = ifStmt.AsParser
        and set (value) = ifStmt.Parser <- value
    
    member this.ForIn 
        with get () = forIn.AsParser
        and set (value) = forIn.Parser <- value
    
    member this.ForNum 
        with get () = forNum.AsParser
        and set (value) = forNum.Parser <- value
    
    member val LocalChoice = localChoice
    
    member this.Function 
        with get () = funcStmt.AsParser
        and set (value) = funcStmt.Parser <- value
    
    member this.Assign 
        with get () = assignStmt.AsParser
        and set (value) = assignStmt.Parser <- value
    
    member val Assignments = assignmentsRef
    
    member this.Call 
        with get () = callStmt.AsParser
        and set (value) = callStmt.Parser <- value
    
    static member Constructor x = new Statement(x)