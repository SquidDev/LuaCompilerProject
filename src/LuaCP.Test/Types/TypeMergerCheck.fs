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
let func x = Function(Single(x, None), Single([], None))
let funcB x y = Function(Single(x, None), Single(y, None))
let funcR x = Function(Single([], None), Single([], None))

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
       Data.Named("Primitive", BoundMode.Minimum, tNum, tInt, tNum |> Some)
       Data.Named("Primitive", BoundMode.Maximum, tNum, tInt, tInt |> Some)
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
                  |> Some)
       // Functions
       Data.Make(BoundMode.Minimum, func [ tNum ], func [ tInt ], func [ tInt ] |> Some)
       Data.Make(BoundMode.Maximum, func [ tNum ], func [ tInt ], func [ tNum ] |> Some)
       Data.Make(BoundMode.Minimum, funcR [ tNum ], funcR [ tInt ], funcR [ tNum ] |> Some)
       Data.Make(BoundMode.Maximum, funcR [ tNum ], funcR [ tInt ], funcR [ tInt ] |> Some)
       Data.Make(BoundMode.Minimum, funcR [ tNum ], func [ tNum ], func [ tNum ] |> Some)
       Data.Make(BoundMode.Maximum, funcR [ tNum ], func [ tNum ], funcR [ tNum ] |> Some) |]

let AssertSubtype func current target = 
    if not (func current target) then Assert.Fail(sprintf "Should be able to convert %A to %A" current target)

[<Test>]
[<TestCaseSource("Bounds")>]
let ``ValueType bounds`` (mode : BoundMode) (a : ValueType) (b : ValueType) (expected : ValueType option) = 
    match expected with
    | None -> 
        try 
            let result = TypeBounds.Value mode a b
            Assert.Fail(sprintf "Expected error, got %A" result)
        with :? BoundException as e -> ()
    | Some expected -> 
        let current = TypeBounds.Value mode a b
        Assert.AreEqual(expected, current, sprintf "%A(%A, %A)" mode a b)
        match mode with
        | BoundMode.Minimum -> 
            let checker = (new TypeProvider()).IsSubtype
            AssertSubtype checker a expected
            AssertSubtype checker b expected
        | BoundMode.Maximum -> 
            let checker = (new TypeProvider()).IsSubtype
            AssertSubtype checker expected a
            AssertSubtype checker expected b
        | _ -> ()

[<Test>]
[<TestCaseSource("Bounds")>]
let ``ValueType bounds symetrical`` (mode : BoundMode) (a : ValueType) (b : ValueType) (expected : ValueType option) = 
    match expected with
    | None -> 
        try 
            let result = TypeBounds.Value mode a b
            Assert.Fail(sprintf "Expected error, got %A" result)
        with :? BoundException as e -> ()
        try 
            let result = TypeBounds.Value mode b a
            Assert.Fail(sprintf "Expected error, got %A (when flipped)" result)
        with :? BoundException as e -> ()
    | Some expected -> 
        let bound1 = TypeBounds.Value mode a b
        let bound2 = TypeBounds.Value mode b a
        Assert.AreEqual(bound1, bound2, sprintf "%A(%A, %A)" mode a b)
(* Basic lua test cases
    return (...)=>print(...)

    local x : string = 2

    local x = { hello = 2 }
    return x.hello
*)
