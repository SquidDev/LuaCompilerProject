module LuaCP.Parser.Pattern

open LuaCP.Tree
open LuaCP.Tree.Expression
open LuaCP.Tree.Statement
open LuaCP.IR
open LuaCP.IR.Instructions

let (|Identifier|_|) (input : IValueNode) = 
    if input :? IdentifierNode then 
        let temp = input :?> IdentifierNode
        Some(temp.Name)
    else None

let (|Index|_|) (input : IValueNode) = 
    if input :? IndexNode then 
        let temp = input :?> IndexNode
        Some(temp.Table, temp.Key)
    else None