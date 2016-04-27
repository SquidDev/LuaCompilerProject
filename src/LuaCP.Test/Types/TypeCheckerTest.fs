module LuaCP.Types.TypeCheckerTest

open NUnit.Framework
open System
open System.Text
open LuaCP
open LuaCP.IR
open LuaCP.Collections
open LuaCP.Types
open LuaCP.Types.TypeFactory

let AssertSubtype func current target = 
    if not (func current target) then Assert.Fail(sprintf "Should be able to convert %A to %A" current target)

let AssertNotSubtype func current target = 
    if func current target then Assert.Fail(sprintf "Should not be able to convert %A to %A" current target)

let tStr, tNum, tInt, tBoo = Primitives.String, Primitives.Number, Primitives.Integer, Primitives.Boolean
let tVoid = TupleType.Empty
let lStr x = Literal(Literal.String x)
let func x = Function(Single(x, None), tVoid)
let funcL x y = Function(Single(x, None), Single(y, None))
let tabl x = Table(x, OperatorHelpers.Empty)
let checker = new TypeProvider()

let a = 
    let tyRef = new IdentRef<_>(Unbound)
    let ty = Reference tyRef
    
    let table = 
        Table
            ([ { Key = tInt
                 Value = tInt
                 ReadOnly = true } ], 
             OperatorHelpers.Singleton (Function(Single([ ty; ty ], None), Single([ tBoo ], None))) Operator.Equals)
    tyRef.Value <- Link table
    table

let b = 
    let tyRef = new IdentRef<_>(Unbound)
    let ty = Reference tyRef
    let table = 
        Table
            ([], OperatorHelpers.Singleton (Function(Single([ ty; ty ], None), Single([ tBoo ], None))) Operator.Equals)
    tyRef.Value <- Link table
    table

let addable = 
    let tyRef = new IdentRef<_>(Unbound)
    let ty = Reference tyRef
    let table = 
        Table([], OperatorHelpers.Singleton (Function(Single([ ty; ty ], None), Single([ ty ], None))) Operator.Add)
    tyRef.Value <- Link table
    table

let ValueSubtypes = 
    [| // Primitive conversions
       Data.Make(tInt, tNum, true)
       Data.Make(tNum, tInt, false)
       Data.Make(tStr, tNum, false)
       // Literal conversion
       Data.Make(ValueType.Literal(Literal.String "x"), ValueType.Literal(Literal.String "x"), true)
       Data.Make(ValueType.Literal(Literal.String "x"), ValueType.Literal(Literal.String "y"), false)
       Data.Make(ValueType.Literal(Literal.String "x"), tStr, true)
       Data.Make(ValueType.Literal(Literal.String "x"), tNum, false)
       Data.Make(tStr, ValueType.Literal(Literal.String "x"), false)
       // Unions
       Data.Make(Nil, Union [ Nil; tStr ], true)
       Data.Make(tStr, Union [ Nil; tStr ], true)
       Data.Make(Union [ Nil; tStr ], tStr, false)
       Data.Make(Union [ Nil; tStr ], Nil, false)
       Data.Make(Union [ Nil; tStr; tNum ], Union [ tStr; tNum ], false)
       Data.Make(Union [ tStr; tNum ], Union [ Nil; tStr; tNum ], true)
       Data.Make(Union [ tStr; tInt ], Union [ tStr; tNum ], true)
       // Value
       Data.Make(Union [ Nil; tStr ], Value, false)
       Data.Make(tStr, Value, true)
       Data.Make(Union [ tNum; tStr ], Value, true)
       Data.Make(Union [ Nil; tNum; tStr ], Union [ Value; Nil ], true)
       // Functions!
       // Basic args
       Data.Make(func [ tStr ], func [ tStr ], true)
       Data.Make(func [], func [ tStr ], true)
       Data.Make(func [ tStr ], func [], false)
       Data.Make(func [ tNum ], func [ tStr ], false)
       Data.Make(func [ tNum ], func [ tInt ], true)
       // Intersections
       Data.Make(FunctionIntersection [ func [ tNum ]
                                        func [ tStr ] ], func [ tInt ], true)
       Data.Make(func [ tInt ], 
                 FunctionIntersection [ func [ tNum ]
                                        func [ tStr ] ], false)
       // Tables
       Data.Make(tabl [ { Key = tNum
                          Value = tNum
                          ReadOnly = false } ], 
                 tabl [ { Key = tNum
                          Value = tNum
                          ReadOnly = false } ], true)
       Data.Make(tabl [ { Key = tNum
                          Value = tNum
                          ReadOnly = false } ], 
                 tabl [ { Key = tNum
                          Value = Value
                          ReadOnly = false } ], false)
       Data.Make(tabl [ { Key = tNum
                          Value = tNum
                          ReadOnly = false } ], 
                 tabl [ { Key = tNum
                          Value = Value
                          ReadOnly = true } ], true)
       // Opcodes
       Data.Make(tNum, Table([], OperatorHelpers.Singleton (funcL [ tNum; tNum ] [ tNum ]) Operator.Add), true)
       // Recursive types
       Data.Make(a, b, true)
       Data.Make(b, a, false)
       // With operators
       Data.Make(tNum, a, false)
       Data.Make(tNum, b, true)
       Data.Make(tNum, addable, true)
       Data.Make(addable, tNum, false) |]

let empty = List.empty<ValueType>
let emptyTuples = List.empty<TupleType>

let TupleSubtypes = 
    [| Data.Make([ tStr ], None, empty, None, true)
       Data.Make(empty, None, [ tStr ], None, false)
       Data.Make([ tInt ], None, [ tNum ], None, true)
       Data.Make([ tNum ], None, [ tInt ], None, false)
       Data.Make([ tInt; tInt ], None, [ tInt ], None, true)
       Data.Make([ tInt; tInt; tInt ], None, [ tInt ], Some tInt, true)
       Data.Make([ tInt ], Some tInt, [ tInt; tInt; tInt ], None, false)
       Data.Make([ tInt ], Some tInt, 
                 [ tInt
                   Union [ tInt; Nil ]
                   Union [ tInt; Nil ] ], None, true) |]

let Functions = 
    [| Data.Make
           (Function(Single([ tStr ], None), tVoid), [ tStr ], None, Some(Function(Single([ tStr ], None), tVoid)), 
            empty)
       Data.Make(Function(Single([ tStr ], None), tVoid), [ tNum ], None, None, empty)
       Data.Make(FunctionIntersection [ func [ tStr ]
                                        func [ lStr "foo" ] ], [ tStr ], None, Some(func [ tStr ]), empty)
       Data.Make(FunctionIntersection [ func [ tStr ]
                                        func [ lStr "foo" ] ], [ lStr "foo" ], None, Some(func [ lStr "foo" ]), empty)
       Data.Make(FunctionIntersection [ func [ tStr ]
                                        func [ lStr "foo" ] ], [ lStr "bar" ], None, Some(func [ tStr ]), empty)
       Data.Make(FunctionIntersection [ func [ tStr ]
                                        func [ Value ] ], [ lStr "bar" ], None, Some(func [ tStr ]), empty)
       Data.Make(FunctionIntersection [ func [ tStr ]
                                        func [ tNum ] ], [ lStr "bar" ], None, Some(func [ tStr ]), empty) |]

let Unions = 
    [| Data.Make([ tStr; tStr ], tStr)
       Data.Make([ tStr
                   lStr "foo" ], tStr)
       Data.Make([ lStr "foo"
                   tStr ], tStr)
       Data.Make([ lStr "foo"
                   tStr
                   tNum
                   Nil ], Union [ tStr; tNum; Nil ]) |]

let Constraints = 
    [| Data.Make(tStr, tStr, Some tStr)
       Data.Make(tStr, tNum, None)
       Data.Make(Union [ tStr; tNum ], tStr, Some tStr)
       Data.Make(Union [ tStr; tNum ], Nil, None)
       Data.Make(Union [ Nil; tStr; tNum ], addable, Some tNum)
       Data.Make(Union [ tStr; tNum; Nil ], Union [ tStr; Nil ], Some(Union [ tStr; Nil ])) |]

[<Test>]
[<TestCaseSource("ValueSubtypes")>]
let ``Value subtypes`` (current : ValueType) (target : ValueType) (pass : bool) = 
    if pass then AssertSubtype checker.IsSubtype current target
    else AssertNotSubtype checker.IsSubtype current target

[<Test>]
[<TestCaseSource("TupleSubtypes")>]
let ``Tuple subtypes`` (current : ValueType list) (currentRem : ValueType option) (target : ValueType list) 
    (targetRem : ValueType option) (pass : bool) = 
    let current = Single(current, currentRem)
    let target = Single(target, targetRem)
    if pass then AssertSubtype checker.IsTupleSubtype current target
    else AssertNotSubtype checker.IsTupleSubtype current target

[<Test>]
[<TestCaseSource("Functions")>]
let ``Best functions`` (func : ValueType) (args : ValueType list) (argsRem : ValueType option) 
    (eFunc : option<ValueType>) (eAlt : ValueType list) = 
    let args = Single(args, argsRem)
    let aFunc, aAlt = checker.FindBestFunction func args
    Assert.AreEqual(eFunc, aFunc, sprintf "Function %A with %O: expected %A, got %A + %A" func args eFunc aFunc aAlt)
    CollectionAssert.AreEquivalent
        (eAlt, aAlt, sprintf "Function %A with %O: expected alternatives of %A, got %A + %A" func args eAlt aFunc aAlt)

[<Test>]
[<TestCaseSource("Unions")>]
let ``Union simplification`` (current : ValueType list) (expected : ValueType) = 
    let aCurrent = checker.Union current
    Assert.True
        (checker.IsTypeEqual expected aCurrent, 
         sprintf "Union %A: expected %A, got %A" (Union current) expected aCurrent)

[<Test>]
[<TestCaseSource("Constraints")>]
let ``Type constraints`` (ty : ValueType) (constrain : ValueType) (expected : ValueType option) = 
    let aCurrent = checker.Constrain ty constrain
    match (expected, aCurrent) with
    | None, None -> ()
    | Some _, None | None, Some _ -> 
        raise (AssertionException(sprintf "%A <: %A: expected %A, got %A" constrain ty expected aCurrent))
    | Some expected, Some aCurrent -> 
        Assert.True
            (checker.IsTypeEqual expected aCurrent, 
             sprintf "%A <: %A: expected %A, got %A" constrain ty expected aCurrent)
