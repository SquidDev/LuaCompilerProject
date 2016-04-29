module LuaCP.Parser.Operators

open LuaCP.IR.Instructions
open LuaCP.Parser
open LuaCP.Tree
open LuaCP.Lua.Tree

let private revBin opcode x y = Nodes.BinOp opcode y x
let private conditional t x y : IValueNode = upcast new Expression.ConditionNode(t, x, y)

type Conditions = Expression.ConditionNode.ConditionKind

let Binary : list<string * (IValueNode -> IValueNode -> IValueNode) * int * bool> = 
    [ ("^", Nodes.BinOp Opcode.Power, 12, true)
      ("+", Nodes.BinOp Opcode.Add, 9, false)
      ("-", Nodes.BinOp Opcode.Subtract, 9, false)
      ("*", Nodes.BinOp Opcode.Multiply, 10, false)
      ("/", Nodes.BinOp Opcode.Divide, 10, false)
      ("%", Nodes.BinOp Opcode.Modulus, 10, false)
      ("//", Nodes.BinOp Opcode.IntegerDivide, 10, false)
      ("..", Nodes.BinOp Opcode.Concat, 8, true)
      (">>", Nodes.BinOp Opcode.RShift, 7, false)
      ("<<", Nodes.BinOp Opcode.LShift, 7, false)
      ("&", Nodes.BinOp Opcode.BAnd, 6, false)
      ("~", Nodes.BinOp Opcode.BXor, 5, false)
      ("|", Nodes.BinOp Opcode.BOr, 4, false)
      ("==", Nodes.BinOp Opcode.Equals, 3, false)
      ("<=", Nodes.BinOp Opcode.LessThanEquals, 3, false)
      ("<", Nodes.BinOp Opcode.LessThan, 3, false)
      (">=", revBin Opcode.LessThanEquals, 3, false)
      (">", revBin Opcode.LessThan, 3, false)
      ("or", conditional Conditions.Or, 2, false)
      ("and", conditional Conditions.And, 1, false) ]

let Unary : list<string * (IValueNode -> IValueNode) * int> = 
    [ ("not", Nodes.UnOp Opcode.Not, 11)
      ("-", Nodes.UnOp Opcode.UnaryMinus, 11)
      ("~", Nodes.UnOp Opcode.BNot, 11)
      ("#", Nodes.UnOp Opcode.Length, 11) ]