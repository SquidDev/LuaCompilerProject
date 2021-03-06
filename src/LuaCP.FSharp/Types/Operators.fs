namespace LuaCP.Types

open System
open LuaCP.IR
open LuaCP.IR.Instructions

type Operator = 
    | UnaryMinus = 0
    | BNot = 1
    | Length = 2
    | Add = 3
    | Subtract = 4
    | Multiply = 5
    | Divide = 6
    | IntegerDivide = 7
    | Power = 8
    | Modulus = 9
    | Concat = 10
    | BAnd = 11
    | BOr = 12
    | BXor = 13
    | LShift = 14
    | RShift = 15
    | Equals = 16
    | LessThan = 17
    | Index = 18
    | NewIndex = 19
    | Call = 20

module OperatorExtensions = 
    let LastIndex = 21
    
    let ToOpcode(x : Operator) = 
        match x with
        | x when x >= Operator.UnaryMinus && x <= Operator.Equals -> enum<Opcode> ((int x) + 1)
        | Operator.LessThan -> Opcode.LessThan
        | Operator.Index -> Opcode.TableGet
        | Operator.NewIndex -> Opcode.TableSet
        | _ -> raise (ArgumentException("Unknown operator " + x.ToString(), "x"))
    
    let ToOperator(x : Opcode) = 
        match x with
        | x when x >= Opcode.UnaryMinus && x <= Opcode.Equals -> enum<Operator> ((int x) - 1)
        | Opcode.LessThan -> Operator.LessThan
        | Opcode.TableGet -> Operator.Index
        | Opcode.TableSet -> Operator.NewIndex
        | _ -> raise (ArgumentException("Unknown opcode " + x.ToString(), "x"))
    
    type Operator with
        member x.AsOpcode = ToOpcode x
    
    type Opcode with
        member x.AsOperator = ToOperator x
