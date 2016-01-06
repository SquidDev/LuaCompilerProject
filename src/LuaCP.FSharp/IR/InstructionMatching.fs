module LuaCP.IR.Instructions.Matching

let inline (|BinaryOp|_|) (op : Instruction) = 
    if op.Opcode.IsBinaryOperator() then Some(op :?> BinaryOp)
    else None

let inline (|UnaryOp|_|) (op : Instruction) = 
    if op.Opcode.IsUnaryOperator() then Some(op :?> UnaryOp)
    else None

let inline (|Branch|_|) (op : Instruction) = 
    if op.Opcode = Opcode.Branch then Some(op :?> Branch)
    else None

let inline (|BranchCondition|_|) (op : Instruction) = 
    if op.Opcode = Opcode.BranchCondition then Some(op :?> BranchCondition)
    else None

let inline (|ValueCondition|_|) (op : Instruction) = 
    if op.Opcode = Opcode.ValueCondition then Some(op :?> ValueCondition)
    else None

let inline (|Return|_|) (op : Instruction) = 
    if op.Opcode = Opcode.Return then Some(op :?> Return)
    else None

let inline (|TableGet|_|) (op : Instruction) = 
    if op.Opcode = Opcode.TableGet then Some(op :?> TableGet)
    else None

let inline (|TableSet|_|) (op : Instruction) = 
    if op.Opcode = Opcode.TableSet then Some(op :?> TableSet)
    else None

let inline (|TableNew|_|) (op : Instruction) = 
    if op.Opcode = Opcode.TableNew then Some(op :?> TableNew)
    else None

let inline (|Call|_|) (op : Instruction) = 
    if op.Opcode = Opcode.Call then Some(op :?> Call)
    else None

let inline (|TupleNew|_|) (op : Instruction) = 
    if op.Opcode = Opcode.TupleNew then Some(op :?> TupleNew)
    else None

let inline (|TupleGet|_|) (op : Instruction) = 
    if op.Opcode = Opcode.TupleGet then Some(op :?> TupleGet)
    else None

let inline (|TupleRemainder|_|) (op : Instruction) = 
    if op.Opcode = Opcode.TupleRemainder then Some(op :?> TupleRemainder)
    else None

let inline (|ReferenceGet|_|) (op : Instruction) = 
    if op.Opcode = Opcode.ReferenceGet then Some(op :?> ReferenceGet)
    else None

let inline (|ReferenceSet|_|) (op : Instruction) = 
    if op.Opcode = Opcode.ReferenceSet then Some(op :?> ReferenceSet)
    else None

let inline (|ReferenceNew|_|) (op : Instruction) = 
    if op.Opcode = Opcode.ReferenceNew then Some(op :?> ReferenceNew)
    else None

let inline (|ClosureNew|_|) (op : Instruction) = 
    if op.Opcode = Opcode.ClosureNew then Some(op :?> ClosureNew)
    else None
