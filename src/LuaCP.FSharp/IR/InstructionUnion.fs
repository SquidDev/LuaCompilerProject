namespace LuaCP.IR.Instructions.Union

open System
open LuaCP.IR.Instructions

/// A union for instructions, useful for pattern matching
type InstructionUnion = 
    | BinaryOp of BinaryOp
    | UnaryOp of UnaryOp
    | Branch of Branch
    | BranchCondition of BranchCondition
    | ValueCondition of ValueCondition
    | Return of Return
    | TableGet of TableGet
    | TableSet of TableSet
    | TableNew of TableNew
    | Call of Call
    | TupleNew of TupleNew
    | TupleGet of TupleGet
    | TupleRemainder of TupleRemainder
    | ReferenceGet of ReferenceGet
    | ReferenceSet of ReferenceSet
    | ReferenceNew of ReferenceNew
    | ClosureNew of ClosureNew
    /// Create a instruction union for types
    static member Create(x : Instruction) = 
        match x.Opcode with
        | op when op.IsBinaryOperator() -> InstructionUnion.BinaryOp(x :?> BinaryOp)
        | op when op.IsUnaryOperator() -> InstructionUnion.UnaryOp(x :?> UnaryOp)
        | Opcode.Branch -> InstructionUnion.Branch(x :?> Branch)
        | Opcode.BranchCondition -> InstructionUnion.BranchCondition(x :?> BranchCondition)
        | Opcode.ValueCondition -> InstructionUnion.ValueCondition(x :?> ValueCondition)
        | Opcode.Return -> InstructionUnion.Return(x :?> Return)
        | Opcode.TableGet -> InstructionUnion.TableGet(x :?> TableGet)
        | Opcode.TableSet -> InstructionUnion.TableSet(x :?> TableSet)
        | Opcode.TableNew -> InstructionUnion.TableNew(x :?> TableNew)
        | Opcode.Call -> InstructionUnion.Call(x :?> Call)
        | Opcode.TupleNew -> InstructionUnion.TupleNew(x :?> TupleNew)
        | Opcode.TupleGet -> InstructionUnion.TupleGet(x :?> TupleGet)
        | Opcode.TupleRemainder -> InstructionUnion.TupleRemainder(x :?> TupleRemainder)
        | Opcode.ReferenceGet -> InstructionUnion.ReferenceGet(x :?> ReferenceGet)
        | Opcode.ReferenceSet -> InstructionUnion.ReferenceSet(x :?> ReferenceSet)
        | Opcode.ReferenceNew -> InstructionUnion.ReferenceNew(x :?> ReferenceNew)
        | Opcode.ClosureNew -> InstructionUnion.ClosureNew(x :?> ClosureNew)
        | _ -> raise (Exception("Unknown opcode " + x.Opcode.ToString()))
