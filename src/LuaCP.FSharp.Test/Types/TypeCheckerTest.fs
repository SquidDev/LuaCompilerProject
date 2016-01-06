module LuaCP.Types.TypeCheckerTest

open NUnit.Framework
open LuaCP.IR
open LuaCP.Types
open LuaCP.Types.TypeChecker

let AssertSubtype current target = 
    if not (IsSubtype current target) then Assert.Fail(sprintf "Cannot convert %A to %A" current target)

let AssertNotSubtype current target = 
    if IsSubtype current target then Assert.Fail(sprintf "Should not be able to convert %A to %A" current target)

let PrimitiveSubtypes = 
    [| TestCaseData(LiteralKind.Integer, LiteralKind.Number, true)
       TestCaseData(LiteralKind.Number, LiteralKind.Integer, false)
       TestCaseData(LiteralKind.String, LiteralKind.Number, false) |]

[<TestCaseSource("PrimitiveSubtypes")>]
let ``Primitive subtyping`` (current : LiteralKind) (target : LiteralKind) (pass : bool) = 
    if pass then AssertSubtype (Primitive current) (Primitive target)
    else AssertNotSubtype (Primitive current) (Primitive target)
