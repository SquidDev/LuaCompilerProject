using LuaCP.IR.Instructions;
using System.Linq;
using LuaCP.IR.User;
using LuaCP.IR;

namespace LuaCP.CodeGen.Lua
{
	public static class FunctionCodeGenHelpers
	{
		public static bool IsSimpleComparison(ValueInstruction insn)
		{
			// Can only be used once
			if (insn.Users.TotalCount != 1) return false;

			// Must be a trivial operator
			if (!insn.Opcode.IsComparisonOperator() && insn.Opcode != Opcode.Not) return false;

			// Must be the next instruction
			var user = insn.Users.First<IUser<IValue>>() as Instruction;
			if (user == null || insn.Next != user) return false;

			switch (user.Opcode)
			{
				case Opcode.BranchCondition:
					{
						var cond = (BranchCondition)user;
						return cond.Test == insn;
					}
				case Opcode.ValueCondition:
					{
						var cond = (ValueCondition)user;
						return cond.Test == insn;
					}
				case Opcode.Not: 
					{
						var op = (UnaryOp)user;
						return op.Operand == insn;
					}
				default:
					return false;
			}
		}
	}
}

