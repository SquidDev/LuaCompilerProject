module LuaCP.Types.TypeMergerCheck

open System
open NUnit.Framework
open LuaCP
open LuaCP.Collections
open LuaCP.IR
open LuaCP.Types
open LuaCP.Types.Extensions
open LuaCP.Types.Primitives

[<Test>]
let ``Simple equate``() = 
    let root = Reference(new IdentRef<_>(Unbound))
    let number = Number
    let merged = TypeBounds.Value (BoundMode.Equal) root number
    Assert.AreEqual(number, merged, "Incorrect result of merging")
    Assert.AreEqual(root.Root, number, "Unbound was bound incorrectly")

let tStr, tNum, tInt, tBoo = Primitives.String, Primitives.Number, Primitives.Integer, Primitives.Boolean
let lStr x = Literal(Literal.String x)
let tabl x = Table(Set.ofList x, OperatorHelpers.Empty)

let pair k v = 
    { Key = k
      Value = v
      ReadOnly = true }

let pairW k v = 
    { Key = k
      Value = v
      ReadOnly = false }

let Union x = Set.ofList x |> Union
let Intersection x = Set.ofList x |> Intersection

let Bounds = 
    [| // Primitive conversions
       Data.Named("Primitive", BoundMode.Minimum, tNum, tInt, tInt |> Some)
       Data.Named("Primitive", BoundMode.Maximum, tNum, tInt, tNum |> Some)
       Data.Named("Primitive", BoundMode.Minimum, tNum, tStr, Union [ tNum; tStr ] |> Some)
       Data.Named("Primitive", BoundMode.Maximum, tNum, tStr, None)
       // Constants
       Data.Named("Constant", BoundMode.Minimum, lStr "foo", tStr, tStr |> Some)
       Data.Named("Constant", BoundMode.Maximum, lStr "foo", tStr, lStr "foo" |> Some)
       Data.Named("Constant", BoundMode.Minimum, lStr "foo", tNum, 
                  Union [ tNum
                          lStr "foo" ]
                  |> Some)
       Data.Named("Constant", BoundMode.Maximum, lStr "foo", tNum, None)
       // Dynamic should always resolve to the other type
       Data.Named("Dynamic", BoundMode.Minimum, tStr, Dynamic, tStr |> Some)
       Data.Named("Dynamic", BoundMode.Maximum, tStr, Dynamic, tStr |> Some)
       // Basic tables
       Data.Named("Tables", BoundMode.Minimum, tabl [ pair (lStr "foo") tStr ], 
                  tabl [ pair (lStr "foo") tStr
                         pair (lStr "bar") tStr ], tabl [ pair (lStr "foo") tStr ] |> Some)
       Data.Named("Tables", BoundMode.Maximum, tabl [ pair (lStr "foo") tStr ], 
                  tabl [ pair (lStr "foo") tStr
                         pair (lStr "bar") tStr ], 
                  tabl [ pair (lStr "foo") tStr
                         pair (lStr "bar") tStr ]
                  |> Some) |]

[<Test>]
[<TestCaseSource("Bounds")>]
let ``ValueType bounds`` (mode : BoundMode) (a : ValueType) (b : ValueType) (expected : ValueType option) = 
    let current = 
        try 
            TypeBounds.Value mode a b |> Some
        with _ -> None
    Assert.AreEqual(expected, current, sprintf "%A(%A, %A)" mode a b)
(* Basic lua test cases
    return (...)=>print(...)

    local x : string = 2

    local x = { hello = 2 }
    return x.hello
*)
