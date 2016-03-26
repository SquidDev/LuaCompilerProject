module LuaCP.Types.OperatorHandling

open System
open LuaCP.Types
open LuaCP.IR
open LuaCP.IR.Instructions

let private lastIndex = OperatorExtensions.LastIndex
let UnOp(x : ValueType) = Function(([ x ], None), ([ x ], None))
let BinOp(x : ValueType) = Function(([ x; x ], None), ([ x ], None))
let Compare(x : ValueType) = Function(([ x; x ], None), ([ Primitives.Boolean ], None))
let Empty : Operators = Array.create lastIndex Nil

let Singleton (x : ValueType) (op : Operator) = 
    let ops : Operators = Array.create lastIndex Nil
    ops.[int op] <- x
    ops

let private concat = 
    let union = Union [ Primitives.Number; Primitives.String ]
    Function(([ union; union ], None), ([ Primitives.String ], None))

let Number = 
    let un, bin, cmp = UnOp Primitives.Number, BinOp Primitives.Number, Compare Primitives.Number
    let ops : ValueType [] = Array.create lastIndex Nil
    // Can do -x
    ops.[int Operator.UnaryMinus] <- un
    // All the stuff in the middle
    for i = int Operator.Add to int Operator.Modulus do
        ops.[i] <- bin
    ops.[int Operator.Concat] <- concat
    ops.[int Operator.Equals] <- cmp
    ops.[int Operator.LessThan] <- cmp
    ops

let Integer = 
    let un, bin, cmp = UnOp Primitives.Integer, BinOp Primitives.Integer, Compare Primitives.Number
    
    let binJoint = 
        FunctionIntersection [ bin
                               BinOp Primitives.Number ]
    
    let ops : ValueType [] = Array.create lastIndex Nil
    // Add everything
    ops.[int Operator.UnaryMinus] <- un
    ops.[int Operator.BNot] <- un
    for i = int Operator.Add to int Operator.Modulus do
        ops.[i] <- binJoint
    ops.[int Operator.Concat] <- concat
    for i = int Operator.BAnd to int Operator.RShift do
        ops.[i] <- bin
    ops.[int Operator.Equals] <- cmp
    ops.[int Operator.LessThan] <- cmp
    ops

let String = 
    let cmp = Compare Primitives.String
    let ops : ValueType [] = Array.create lastIndex Nil
    ops.[int Operator.Length] <- Function(([ Primitives.String ], None), ([ Primitives.Integer ], None))
    ops.[int Operator.Concat] <- concat
    ops.[int Operator.Equals] <- cmp
    ops.[int Operator.LessThan] <- cmp
    ops

let Boolean : ValueType [] = Array.create lastIndex Nil

let Dynamic = 
    let un, bin, cmp = UnOp Dynamic, BinOp Dynamic, Compare Dynamic
    let ops : ValueType [] = Array.create lastIndex Nil
    for i = int Operator.UnaryMinus to int Operator.Length do
        ops.[i] <- un
    for i = int Operator.Add to int Operator.RShift do
        ops.[i] <- bin
    ops.[int Operator.Equals] <- cmp
    ops.[int Operator.LessThan] <- cmp
    ops.[int Operator.Index] <- bin
    ops.[int Operator.NewIndex] <- Function(([ Dynamic; Dynamic; Dynamic ], None), ([], None))
    ops.[int Operator.Call] <- Function(([], Some(Dynamic)), ([], Some(Dynamic)))
    ops

let GetPrimitiveLookup(ty : LiteralKind) = 
    match ty with
    | LiteralKind.Number -> Number
    | LiteralKind.Integer -> Integer
    | LiteralKind.String -> String
    | LiteralKind.Boolean -> Boolean
    | LiteralKind.Nil -> raise (ArgumentException("Nil cannot be used as a primitive type", "ty"))
    | _ -> raise (ArgumentException("Unknown primitive " + ty.ToString(), "ty"))

let GetOperatorPrimitive (ty : LiteralKind) (op : Operator) = (GetPrimitiveLookup ty).[int op]

let rec GetOperator (ty : ValueType) (op : Operator) = 
    match ty with
    | Primitive prim -> GetOperatorPrimitive prim op
    | Literal lit -> GetOperatorPrimitive lit.Kind op
    | Dynamic -> Dynamic.[int op]
    | Value | Nil -> Nil
    | Function(_, _) | FunctionIntersection _ -> 
        if op = Operator.Call then ty
        else Nil
    | Table(_, ops) -> ops.[int op]
    | Union items -> Union(List.map (fun x -> GetOperator x op) items)
    | Reference item -> 
        match item.Value with
        // TODO: handle infinite loops correctly
        | Link child when ty <> child -> GetOperator child op
        | _ -> Nil

let GetBinaryOperatory (left : ValueType) (right : ValueType) (op : Operator) = 
    let leftOp = GetOperator left op
    if leftOp <> Nil then leftOp
    else GetOperator right op