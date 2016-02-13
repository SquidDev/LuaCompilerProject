module LuaCP.Types.TypeCheckerTest

open NUnit.Framework
open System
open System.Text
open LuaCP.IR
open LuaCP.Types
open LuaCP.Types

let AssertSubtype func current target = 
    if (func current target) <> Success then Assert.Fail(sprintf "Should be able to convert %A to %A" current target)

let AssertNotSubtype func current target = 
    if func current target <> Failure then Assert.Fail(sprintf "Should not be able to convert %A to %A" current target)

type Data() = 
    // This works as a member function, but not a let binding.
    static member Make([<ParamArray>] args : Object []) = TestCaseData(args).SetName(sprintf "%A" args)

let tStr, tNum, tInt = Primitives.String, Primitives.Number, Primitives.Integer
let tVoid = [], None
let lStr x = Literal(Literal.String x)
let func x = Function((x, None), tVoid)
let funcL x y = Function((x, None), (y, None))
let tabl x = Table(x, OperatorHandling.Empty)

let checker = new TypeChecker()

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
       Data.Make(tNum, Table([], OperatorHandling.Singleton (funcL [ tNum; tNum ] [ tNum ]) Operator.Add), true) |]

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
    [| Data.Make(Function(([ tStr ], None), tVoid), [ tStr ], None, Some(Function(([ tStr ], None), tVoid)), empty)
       Data.Make(Function(([ tStr ], None), tVoid), [ tNum ], None, None, empty)
       Data.Make(FunctionIntersection [ func [ tStr ]
                                        func [ lStr "foo" ] ], [ tStr ], None, Some(func [ tStr ]), empty)
       Data.Make(FunctionIntersection [ func [ tStr ]
                                        func [ lStr "foo" ] ], [ lStr "foo" ], None, Some(func [ lStr "foo" ]), empty)
       Data.Make(FunctionIntersection [ func [ tStr ]
                                        func [ lStr "foo" ] ], [ lStr "bar" ], None, Some(func [ tStr ]), empty)
       Data.Make(FunctionIntersection [ func [ tStr ]
                                        func [ Value ] ], [ lStr "bar" ], None, None, 
                 [ func [ tStr ]
                   func [ Value ] ])
       Data.Make(FunctionIntersection [ func [ tStr ]
                                        func [ tNum ] ], [ lStr "bar" ], None, Some(func [ tStr ]), empty) |]

[<Test>]
[<TestCaseSource("ValueSubtypes")>]
let ``Value subtypes`` (current : ValueType) (target : ValueType) (pass : bool) = 
    if pass then AssertSubtype checker.IsSubtype current target
    else AssertNotSubtype checker.IsSubtype current target

[<Test>]
[<TestCaseSource("TupleSubtypes")>]
let ``Tuple subtypes`` (current : TupleType) (target : TupleType) (pass : bool) = 
    if pass then AssertSubtype checker.IsTupleSubtype current target
    else AssertNotSubtype checker.IsTupleSubtype current target

[<Test>]
[<TestCaseSource("Functions")>]
let ``Best functions`` (func : ValueType) (args : TupleType) (eFunc : option<ValueType>) (eAlt : ValueType list) = 
    let aFunc, aAlt = checker.FindBestFunction func args
    Assert.AreEqual
        (eFunc, aFunc, 
         sprintf "Function %A with %O: expected %A, got %A + %A" func (ValueType.FormatTuple args) eFunc aFunc aAlt)
    CollectionAssert.AreEquivalent
        (eAlt, aAlt, 
         sprintf "Function %A with %O: expected alternatives of %A, got %A + %A" func (ValueType.FormatTuple args) eAlt 
             aFunc aAlt)
