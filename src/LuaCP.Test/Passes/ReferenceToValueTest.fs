module LuaCP.Passes.ReferenceToValueTest

open NUnit.Framework
open LuaCP.IR.Components
open LuaCP.IR.Instructions
open System.Linq

[<Test>]
let ``Ternary style: x = nil, if 1 then x = 2 else x = 3 end``() = 
    let modu = Module()
    let pool = modu.Constants
    let func = modu.EntryPoint
    // Blocks
    let start = func.EntryPoint
    let setA, setB, finish = Block(func), Block(func), Block(func)
    // local x; if(1) { } else { }
    let x = start.AddLast(ReferenceNew pool.Nil)
    start.AddLast(BranchCondition(pool.[1], setA, setB)) |> ignore
    // x = 2
    setA.AddLast(ReferenceSet(x, pool.[2])) |> ignore
    setA.AddLast(Branch(finish)) |> ignore
    // x = 3
    setB.AddLast(ReferenceSet(x, pool.[3])) |> ignore
    setB.AddLast(Branch(finish)) |> ignore
    // return x
    let getter = finish.AddLast(ReferenceGet x)
    let tuple = finish.AddLast(TupleNew([| getter |], pool.Nil))
    finish.AddLast(Return(tuple)) |> ignore
    // Run pass
    Assert.IsTrue(PassManager(modu).RunPass(ReferenceToValue.Runner, func), "Expected successful pass")
    // Check phi nodes
    Assert.AreEqual(1, finish.PhiNodes.Count)
    let phi = finish.PhiNodes.First()
    Assert.Contains(setA, phi.Source.Keys.ToList())
    Assert.Contains(setB, phi.Source.Keys.ToList())
    Assert.AreEqual(pool.[2], phi.Source.[setA])
    Assert.AreEqual(pool.[3], phi.Source.[setB])

[<Test>]
let ``Long or: x = 3 if 1 then x = 2 else end``() = 
    let modu = Module()
    let pool = modu.Constants
    let func = modu.EntryPoint
    // Blocks
    let start = func.EntryPoint
    let setA, empty, finish = Block(func), Block(func), Block(func)
    // local x; if(1) { } else { }
    let x = start.AddLast(ReferenceNew pool.[3])
    start.AddLast(BranchCondition(pool.[1], setA, empty)) |> ignore
    // x = 2
    setA.AddLast(ReferenceSet(x, pool.[2])) |> ignore
    setA.AddLast(Branch(finish)) |> ignore
    // Empty block
    empty.AddLast(Branch(finish)) |> ignore
    // return x
    let getter = finish.AddLast(ReferenceGet x)
    let tuple = finish.AddLast(TupleNew([| getter |], pool.Nil))
    finish.AddLast(Return(tuple)) |> ignore
    // Run pass
    Assert.IsTrue(PassManager(modu).RunPass(ReferenceToValue.Runner, func), "Expected successful pass")
    // Check phi nodes
    Assert.AreEqual(1, finish.PhiNodes.Count)
    let phi = finish.PhiNodes.First()
    Assert.Contains(setA, phi.Source.Keys.ToList())
    Assert.Contains(empty, phi.Source.Keys.ToList())
    Assert.AreEqual(pool.[2], phi.Source.[setA])
    Assert.AreEqual(pool.[3], phi.Source.[empty])

[<Test>]
let ``Short or: x = 3 if 1 then x = 2 end``() = 
    let modu = Module()
    let pool = modu.Constants
    let func = modu.EntryPoint
    // Blocks
    let start = func.EntryPoint
    let setA, finish = Block(func), Block(func)
    // local x; if(1) { } 
    let x = start.AddLast(ReferenceNew pool.[3])
    start.AddLast(BranchCondition(pool.[1], setA, finish)) |> ignore
    // x = 2
    setA.AddLast(ReferenceSet(x, pool.[2])) |> ignore
    setA.AddLast(Branch(finish)) |> ignore
    // return x
    let getter = finish.AddLast(ReferenceGet x)
    let tuple = finish.AddLast(TupleNew([| getter |], pool.Nil))
    finish.AddLast(Return(tuple)) |> ignore
    // Run pass
    Assert.IsTrue(PassManager(modu).RunPass(ReferenceToValue.Runner, func), "Expected successful pass")
    // Check phi nodes
    Assert.AreEqual(1, finish.PhiNodes.Count)
    let phi = finish.PhiNodes.First()
    Assert.Contains(setA, phi.Source.Keys.ToList())
    Assert.Contains(start, phi.Source.Keys.ToList())
    Assert.AreEqual(pool.[2], phi.Source.[setA])
    Assert.AreEqual(pool.[3], phi.Source.[start])
