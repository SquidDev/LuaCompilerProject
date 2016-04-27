module LuaCP.Types.TypeMergerCheck

open System
open NUnit.Framework
open LuaCP
open LuaCP.Collections
open LuaCP.Types
open LuaCP.Types.Primitives

let merger = new TypeEquator()

[<Test>]
let ``Simple equate`` () = 
    let root = new IdentRef<_>(Unbound)
    let number = Number

    let merged = merger.Value (EquateMode.Equal) (Reference root) number
    Assert.AreEqual(number, merged, "Incorrect result of merging")
    match root.Value with
    | Unbound -> Assert.Fail "Unbound has not been bound"
    | Link ty -> Assert.AreEqual(ty, number, "Unbound was bound incorrectly")

[<Test>]
let ``Simple minimum`` () = 
    let root = new IdentRef<_>(Unbound)
    let number = Number

    let merged = merger.Value (EquateMode.Minimum) (Reference root) number
    Assert.AreEqual(number, merged, "Incorrect result of merging")
    match root.Value with
    | Unbound -> Assert.Fail "Unbound has not been bound"
    | Link ty -> Assert.AreEqual(ty, number, "Unbound was bound incorrectly")


[<Test>]
let ``Simple maximum`` () = 
    let root = new IdentRef<_>(Unbound)
    let number = Number

    let merged = merger.Value (EquateMode.Maximum) (Reference root) number
    Assert.AreEqual(number, merged, "Incorrect result of merging")
    match root.Value with
    | Unbound -> Assert.Fail "Unbound has not been bound"
    | Link ty -> Assert.AreEqual(ty, number, "Unbound was bound incorrectly")
