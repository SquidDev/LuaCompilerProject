namespace LuaCP.Types

open System
open LuaCP.Types
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
    | BXor =13
    | LShift = 14
    | RShift = 15
    | Equals = 16
    | LessThan = 17
    | Index = 18
    | NewIndex = 19

module OperatorExtensions = 
    let LastIndex = 19
    
    let ToOpcode(x : Operator) = 
        match x with
        | Operator.UnaryMinus -> Opcode.UnaryMinus
        | x when x >= Operator.BNot && x <= Operator.Equals -> enum<Opcode> ((int x) - 1)
        | Operator.LessThan -> Opcode.LessThan
        | Operator.Index -> Opcode.TableGet
        | Operator.NewIndex -> Opcode.TableSet
        | _ -> raise (ArgumentException("Unknown operator " + x.ToString(), "x"))
    
    let ToOperator(x : Opcode) = 
        match x with
        | Opcode.UnaryMinus -> Operator.UnaryMinus
        | x when x >= Opcode.BNot && x <= Opcode.Equals -> enum<Operator> ((int x) + 1)
        | Opcode.LessThan -> Operator.LessThan
        | Opcode.TableGet -> Operator.Index
        | Opcode.TableSet -> Operator.NewIndex
        | _ -> raise (ArgumentException("Unknown opcode " + x.ToString(), "x"))
    
    type Operator with
        member x.ToOpcode() = ToOpcode x
    
    type Opcode with
        member x.ToOperator() = ToOperator x

module OperatorHandlers = 
    let LastIndex = 19
    let UnOp(x : ValueType) = Function(([ x ], None), ([ x ], None))
    let BinOp(x : ValueType) = Function(([ x; x ], None), ([ x; x ], None))
    let private concat = BinOp(Union [ Primitives.Number; Primitives.String ])
    
    let Number = 
        let un, bin = UnOp Primitives.Number, BinOp Primitives.Number
        let ops : ValueType [] = Array.create LastIndex Nil
        // Can do -x
        ops.[int Operator.UnaryMinus] <- un
        // All the stuff in the middle
        for i = int Operator.Add to int Operator.Modulus do
            ops.[i] <- bin
        ops.[int Operator.Concat] <- concat
        ops
    
    let Integer = 
        let un, bin = UnOp Primitives.Integer, BinOp Primitives.Integer
        
        let unJoint, binJoint = 
            FunctionIntersection [ un
                                   UnOp Primitives.Number ], 
            FunctionIntersection [ bin
                                   BinOp Primitives.Number ]
        
        let ops : ValueType [] = Array.create LastIndex Nil
        // Add everything
        ops.[int Operator.UnaryMinus] <- unJoint
        ops.[int Operator.BNot] <- un
        for i = int Operator.Add to int Operator.Modulus do
            ops.[i] <- binJoint
        ops.[int Operator.Concat] <- concat
        for i = int Operator.BAnd to int Operator.RShift do
            ops.[i] <- bin
        ops