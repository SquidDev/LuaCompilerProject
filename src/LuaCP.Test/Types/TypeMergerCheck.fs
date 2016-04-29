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
    printfn "Running Simple equate"
    let merger = new TypeMerger()
    let root = merger.ValueNew()
    let number = Number

    let merged = merger.Value (EquateMode.Equal) root.Type number
    Assert.AreEqual(number, merged, "Incorrect result of merging")
    Assert.AreEqual(root.Type.Root, number, "Unbound was bound incorrectly")

[<Test>]
let ``Simple minimum`` () =
    printfn "Running Simple min"
    let merger = new TypeMerger()
    let root = merger.ValueNew()
    let number = Number

    let merged = merger.Value (EquateMode.Minimum) root.Type number
    Assert.AreEqual(number, merged, "Incorrect result of merging")
    Assert.AreEqual(Some number, root.Maximum)


[<Test>]
let ``Simple maximum`` () =
    printfn "Running Simple max"
    let merger = new TypeMerger()
    let root = merger.ValueNew()
    let number = Number

    let merged = merger.Value (EquateMode.Maximum) root.Type number
    Assert.AreEqual(number, merged, "Incorrect result of merging")
    Assert.AreEqual(Some number, root.Minimum)
