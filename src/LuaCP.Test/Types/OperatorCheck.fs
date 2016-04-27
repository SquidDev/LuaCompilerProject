module LuaCP.Types.OperatorCheck

open System
open NUnit.Framework
open LuaCP
open LuaCP.Types
open LuaCP.Types.OperatorExtensions
open LuaCP.IR.Instructions

let Names = Seq.map (fun (x : string) -> Data.Make x) (Enum.GetNames(typedefof<Opcode>))

[<Test>]
[<TestCaseSource("Names")>]
let ``Opcode to Operator`` (name : string) = 
    try 
        let opcode : Opcode = Enum.Parse(typedefof<Opcode>, name) :?> Opcode
        let operator = Enum.Parse(typedefof<Operator>, name)
        Assert.AreEqual(operator, opcode.AsOperator)
    with :? ArgumentException -> ()

[<Test>]
[<TestCaseSource("Names")>]
let ``Operator to Opcode`` (name : string) = 
    try 
        let opcode = Enum.Parse(typedefof<Opcode>, name)
        let operator : Operator = Enum.Parse(typedefof<Operator>, name) :?> Operator
        Assert.AreEqual(opcode, operator.AsOpcode)
    with :? ArgumentException -> ()