module LuaCP.Types.TypeMergerCheck

open System
open NUnit.Framework
open LuaCP
open LuaCP.Collections
open LuaCP.Types
open LuaCP.Types.Extensions
open LuaCP.Types.Primitives

[<Test>]
let ``Simple equate`` () =
    let root = Reference(new IdentRef<_>(Unbound))
    let number = Number

    let merged = TypeBounds.Value (BoundMode.Equal) root number
    Assert.AreEqual(number, merged, "Incorrect result of merging")
    Assert.AreEqual(root.Root, number, "Unbound was bound incorrectly")

let tStr, tNum, tInt, tBoo = Primitives.String, Primitives.Number, Primitives.Integer, Primitives.Boolean
let checker = new TypeProvider()

let Bounds =
    [| // Primitive conversions
       Data.Make(BoundMode.Minimum, tNum, tInt, tInt)
       Data.Make(BoundMode.Maximum, tNum, tInt, tNum) |]

[<Test>]
[<TestCaseSource("Bounds")>]
let ``ValueType bounds`` (mode : BoundMode) (a : ValueType) (b : ValueType) (expected : ValueType) =
    let current = TypeBounds.Value mode a b
    Assert.AreEqual(expected,  current, sprintf "%A(%A, %A)" mode a b)


(* Basic lua test cases
    return (...)=>print(...)

    local x : string = 2

    local x = { hello = 2 }
    return x.hello
*)