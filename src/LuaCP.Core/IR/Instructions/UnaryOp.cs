using System;
using System.Collections.Generic;
using LuaCP.IR.User;

namespace LuaCP.IR.Instructions
{
	public sealed class UnaryOp : ValueInstruction, IUser<IValue>
	{
		private IValue operand;
        
		public IValue Operand
		{
			get { return operand; } 
			set { operand = UserExtensions.Replace(this, operand, value); }
		}

		public UnaryOp(Opcode opcode, IValue operand)
			: base(opcode, ValueKind.Value)
		{
			if (!opcode.IsUnaryOperator()) throw new ArgumentException("Opcode is not operand", "opcode");
			Operand = operand;
		}
    	
		public void Replace(IValue original, IValue replace)
		{
			if (operand == original) Operand = replace;
		}
    	
		public IEnumerable<IValue> GetUses()
		{
			yield return operand;
		}

		public override void ForceDestroy()
		{
			operand.Users.Decrement(this);
			operand = null;
		}
	}
}
