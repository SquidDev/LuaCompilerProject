namespace TypesGalore

open System.Collections.Generic
open System.Text

type Literal = 
    | LStr of string
    | LNum of double
    | LBool of bool
    override this.ToString() = 
        match this with
        | LStr x -> "\"" + x + "\""
        | LNum x -> x.ToString()
        | LBool x -> x.ToString()

type Expression = 
    | ELiteral of Literal
    | EIf of Reference * Reference * Reference
    | ECall of int * Reference list
    override this.ToString() = 
        match this with
        | ELiteral x -> x.ToString()
        | EIf(c, t, f) -> "If(" + c.ToString() + ", " + t.ToString() + ", " + f.ToString() + ")"
        | ECall(func, args) -> 
            let args' = 
                args
                |> List.map (fun x -> x.ToString())
                |> String.concat " "
            "#" + (func.ToString()) + "(" + args' + ")"

and Function(slot : int, args : int) = 
    let expressions = new List<Expression>()
    
    member this.Add(expr : Expression) = 
        expressions.Add expr
        RVar(expressions.Count - 1)
    
    member this.AddE(expr : Expression) = expressions.Add expr
    member this.Id = slot
    override this.ToString() = 
        let builder = new StringBuilder()
        builder.Append "\\" |> ignore // "
        for i = 0 to args - 1 do
            builder.Append "$" |> ignore
            builder.Append i |> ignore
            builder.Append " " |> ignore
        builder.Append "→ \n" |> ignore
        for i = 0 to expressions.Count - 1 do
            builder.Append "    %" |> ignore
            builder.Append i |> ignore
            builder.Append " ← " |> ignore
            builder.Append expressions.[i] |> ignore
            builder.Append "\n" |> ignore
        builder.ToString()

and Reference = 
    | RArg of int
    | RVar of int
    override this.ToString() = 
        match this with
        | RArg x -> "$" + x.ToString()
        | RVar x -> "%" + x.ToString()

type Scope() = 
    let functions = new List<Function>()
    
    member this.Add(args : int) = 
        let func = new Function(functions.Count, args)
        functions.Add(func)
        func
    
    override this.ToString() = 
        let builder = new StringBuilder()
        for item in functions do
            builder.Append item |> ignore
        builder.ToString()