namespace LuaCP.Lua.Parser.Extensions

open System
open FParsec
open LuaCP
open LuaCP.Utils
open LuaCP.Parser.Parsers
open LuaCP.Parser.Primitives
open LuaCP.Parser.Pattern
open LuaCP.Parser
open LuaCP.IR.Instructions
open LuaCP.Tree
open LuaCP.Tree.Expression
open LuaCP.Lua.Tree
open LuaCP.Lua.Tree.Expression
open LuaCP.Types
open LuaCP.IR
open System.Collections.Generic

type TypedDeclaration(name : string, ty : option<ValueType>) = 
    inherit IdentifierNode(name)
    override this.Declare(builder : BlockBuilder, value : IValue) : BlockBuilder = 
        let ref = builder.Block.AddLast(ReferenceNew(value))
        builder.Get<IVariableScope>().Declare(this.Name, ref)
        match ty with
        | Some ty -> builder.Get<TypeScope>().EquateValueWith ref ty
        | None -> ()
        builder

type FieldType = 
    | Meta of Operator * ValueType
    | Field of TableField

type Types(lang : Language) = 
    let literal = lang.Get(fun x -> new Parser.Literal(x))
    let typeParser, typeRef = createParserForwardedToRef<ValueType, unit>()
    
    let named = 
        let namedTypes = 
            Set
                ([| "nil"; "value"; "any"; "string"; "number"; "num"; "integer"; "boolean"; "readonly"; "meta"; "int"; 
                    "bool"; "str" |])
        let basic = 
            identifier (IdentifierOptions(isAsciiIdStart = IdentifierStart, isAsciiIdContinue = IdentifierRemaining)) 
            |> Token
        
        let getNamed x = 
            match x with
            | "nil" -> Types.Nil
            | "value" -> Types.Value
            | "any" -> Types.Dynamic
            | "string" | "str" -> Primitives.String
            | "number" | "num" -> Primitives.Number
            | "integer" | "int" -> Primitives.Integer
            | "boolean" | "bool" -> Primitives.Boolean
            | _ -> raise (Exception("Unexpected type " + x))
        whitelist namedTypes basic |>> getNamed
    
    let table = 
        let field = 
            pipe2 (Keyword "readonly" >>. typeParser) (Symbol ":" >>. typeParser) (fun x y -> 
                Field { Key = y
                        Value = y
                        ReadOnly = true }) 
            <|> pipe2 (Keyword "meta" >>. IdentifierBase) (Symbol ":" >>. typeParser) 
                    (fun x y -> Meta(Enum.Parse(typeof<Operator>, x, true) :?> Operator, y)) 
            <|> pipe2 (typeParser) (Symbol ":" >>. typeParser) (fun x y -> 
                    Field { Key = y
                            Value = y
                            ReadOnly = false })
        
        let table = Symbol "{" >>. sepBy field (Symbol ",") .>> Symbol "}"
        
        let convert (x : FieldType list) : ValueType = 
            let rec doConvert (x : FieldType list) (fields : TableField list) (meta : Operators) = 
                match x with
                | [] -> fields
                | item :: remaining -> 
                    let newFields = 
                        match item with
                        | Field x -> x :: fields
                        | Meta(op, value) -> 
                            meta.[int op] <- value
                            fields
                    doConvert remaining newFields meta
            
            let ops : Operators = Array.create OperatorExtensions.LastIndex ValueType.Nil
            let fields = doConvert x [] ops
            ValueType.Table(fields, ops)
        table |>> convert
    
    let func = 
        let simpleTuple : Parser<TupleType> = named |>> (fun x -> Single([ x ], None))
        let longTuple : Parser<TupleType> = 
            betweenL (Symbol "(") (Symbol ")") (sepBy typeParser (Symbol ",")) "tuple" |>> (fun x -> Single(x, None))
        let tuple = simpleTuple <|> longTuple
        tuple .>> Symbol "->" .>>. tuple |>> ValueType.Function
    
    let root, rootRef = 
        longestChoiceL [ literal.BooleanVal |>> (fun x -> Literal.Boolean(x) :> Literal |> Types.Literal)
                         literal.FloatVal |>> (fun x -> Literal.Number(x) :> Literal |> Types.Literal)
                         literal.StringVal |>> (fun x -> Literal.String(x) :> Literal |> Types.Literal)
                         table
                         func
                         Symbol "(" >>. typeParser .>> Symbol ")"
                         named ] "type"
    
    let ofOne (factory : ValueType list -> ValueType) (lst : ValueType list) = 
        match lst with
        | [ item ] -> item
        | _ -> factory lst
    
    let union = sepBy1 root (Symbol "|") |>> ofOne ValueType.Union
    let intersection = sepBy1 union (Symbol "&") |>> ofOne ValueType.FunctionIntersection
    
    do 
        rootRef.Parsers.Add(Symbol "(" >>. intersection .>> Symbol ")")
        typeRef := intersection
        lang.Declaration <- pipe2 IdentifierBase (Symbol ":" >>. intersection |> opt) 
                                (fun x y -> TypedDeclaration(x, y) :> IDeclarable)
    
    member val Type = typeParser