module LuaCP.Types.TypeCheckerTest

open NUnit.Framework
open LuaCP.IR
open LuaCP.Types
open LuaCP.Types.TypeChecker

let AssertSubtype func current target = 
    if not (func current target) then Assert.Fail(sprintf "Should be able to convert %A to %A" current target)

let AssertNotSubtype func current target = 
    if func current target then Assert.Fail(sprintf "Should not be able to convert %A to %A" current target)

type TC = TestCaseData

let tStr, tNum, tInt = Primitive LiteralKind.String, Primitive LiteralKind.Number, Primitive LiteralKind.Integer
let tVoid = [], None

let ValueSubtypes = 
    [| // Primitive conversions
       TC(tInt, tNum, true)
       TC(tNum, tInt, false)
       TC(tStr, tNum, false)
       // Literal conversion
       TC(ValueType.Literal(Literal.String "x"), ValueType.Literal(Literal.String "x"), true)
       TC(ValueType.Literal(Literal.String "x"), ValueType.Literal(Literal.String "y"), false)
       TC(ValueType.Literal(Literal.String "x"), tStr, true)
       TC(ValueType.Literal(Literal.String "x"), tNum, false)
       TC(tStr, ValueType.Literal(Literal.String "x"), false)
       // Unions
       TC(Nil, Union [ Nil; tStr ], true)
       TC(tStr, Union [ Nil; tStr ], true)
       TC(Union [ Nil; tStr ], tStr, false)
       TC(Union [ Nil; tStr ], Nil, false)
       TC(Union [ Nil; tStr; tNum ], Union [ tStr; tNum ], false)
       TC(Union [ tStr; tNum ], Union [ Nil; tStr; tNum ], true)
       TC(Union [ tStr; tInt ], Union [ tStr; tNum ], true)
       // Value
       TC(Union [ Nil; tStr ], Value, false)
       TC(tStr, Value, true)
       TC(Union [ tNum; tStr ], Value, true)
       TC(Union [ Nil; tNum; tStr ], Union [ Value; Nil ], true)
       // Functions!
       // Basic args
       TC(Function(([ tStr ], None), tVoid), Function(([ tStr ], None), tVoid), true)
       TC(Function(([], None), tVoid), Function(([ tStr ], None), tVoid), true)
       TC(Function(([ tStr ], None), tVoid), Function(([], None), tVoid), false)
       TC(Function(([ tNum ], None), tVoid), Function(([ tStr ], None), tVoid), false)
       TC(Function(([ tNum ], None), tVoid), Function(([ tInt ], None), tVoid), true) |]

let empty = List.empty<ValueType>

let TupleSubtypes = 
    [| TC([ tStr ], None, empty, None, true)
       TC(empty, None, [ tStr ], None, false)
       TC([ tInt ], None, [ tNum ], None, true)
       TC([ tNum ], None, [ tInt ], None, false)
       TC([ tInt; tInt ], None, [ tInt ], None, true)
       TC([ tInt; tInt; tInt ], None, [ tInt ], Some tInt, true)
       TC([ tInt ], Some tInt, [ tInt; tInt; tInt ], None, false)
       TC([ tInt ], Some tInt, 
          [ tInt
            Union [ tInt; Nil ]
            Union [ tInt; Nil ] ], None, true) |]

[<Test>]
[<TestCaseSource("ValueSubtypes")>]
let ``Value subtypes`` (current : ValueType) (target : ValueType) (pass : bool) = 
    if pass then AssertSubtype IsSubtype current target
    else AssertNotSubtype IsSubtype current target

[<Test>]
[<TestCaseSource("TupleSubtypes")>]
let ``Tuple subtypes`` (current : TupleType) (target : TupleType) (pass : bool) = 
    if pass then AssertSubtype IsTupleSubtype current target
    else AssertNotSubtype IsTupleSubtype current target
