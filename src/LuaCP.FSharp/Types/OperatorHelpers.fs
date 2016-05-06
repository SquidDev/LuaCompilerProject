module LuaCP.Types.OperatorHelpers

open System
open LuaCP.Collections
open LuaCP.IR
open LuaCP.IR.Instructions
open LuaCP.Types

let private lastIndex = OperatorExtensions.LastIndex
let UnOp(x : ValueType) = Function(Single([ x ], None), Single([ x ], None))
let BinOp(x : ValueType) = Function(Single([ x; x ], None), Single([ x ], None))
let Compare(x : ValueType) = Function(Single([ x; x ], None), Single([ Primitives.Boolean ], None))
let Empty : Operators = Array.create lastIndex Nil

let Singleton (x : ValueType) (op : Operator) =
    let ops : Operators = Array.create lastIndex Nil
    ops.[int op] <- x
    ops

let private concat =
    let union = Set.of2 Primitives.Number Primitives.String |> Union
    Function(Single([ union; union ], None), Single([ Primitives.String ], None))

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
        Set.of2 bin (BinOp Primitives.Number) |> Intersection

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
    ops.[int Operator.Length] <- Function(Single([ Primitives.String ], None), Single([ Primitives.Integer ], None))
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
    ops.[int Operator.NewIndex] <- Function(Single([ Dynamic; Dynamic; Dynamic ], None), Single([], None))
    ops.[int Operator.Call] <- Function(Single([], Some(Dynamic)), Single([], Some(Dynamic)))
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
