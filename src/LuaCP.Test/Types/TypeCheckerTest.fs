module LuaCP.Types.TypeCheckerTest

open NUnit.Framework
open System
open LuaCP.IR
open LuaCP.Types
open LuaCP.Types.TypeChecker

let AssertSubtype func current target = 
    if not (func current target) then Assert.Fail(sprintf "Should be able to convert %A to %A" current target)

let AssertNotSubtype func current target = 
    if func current target then Assert.Fail(sprintf "Should not be able to convert %A to %A" current target)

type Data() = 
    // This works as a member function, but not a let binding.
    static member Make([<ParamArray>] args : Object []) = TestCaseData(args).SetName(sprintf "%A" args)

let tStr, tNum, tInt = Primitive LiteralKind.String, Primitive LiteralKind.Number, Primitive LiteralKind.Integer
let tVoid = [], None

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
       Data.Make(Function(([ tStr ], None), tVoid), Function(([ tStr ], None), tVoid), true)
       Data.Make(Function(([], None), tVoid), Function(([ tStr ], None), tVoid), true)
       Data.Make(Function(([ tStr ], None), tVoid), Function(([], None), tVoid), false)
       Data.Make(Function(([ tNum ], None), tVoid), Function(([ tStr ], None), tVoid), false)
       Data.Make(Function(([ tNum ], None), tVoid), Function(([ tInt ], None), tVoid), true) |]

let empty = List.empty<ValueType>

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