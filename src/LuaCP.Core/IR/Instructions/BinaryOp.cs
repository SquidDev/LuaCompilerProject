using System;
using System.Collections.Generic;
using LuaCP.IR.User;

namespace LuaCP.IR.Instructions
{
	public sealed class BinaryOp : ValueInstruction, IUser<IValue>
	{
		private IValue left;
		private IValue right;

		public IValue Left
		{
			get { return left; } 
			set { left = UserExtensions.Replace(this, left, value); }
		}

		public IValue Right
		{
			get { return right; } 
			set { right = UserExtensions.Replace(this, right, value); }
		}

		public BinaryOp(Opcode opcode, IValue left, IValue right)
			: base(opcode, ValueKind.Value)
		{
			if (!opcode.IsBinaryOperator()) throw new ArgumentException("Opcode is not binary", "opcode");
			Left = left;
			Right = right;
		}

		public void Replace(IValue original, IValue replace)
		{
			if (left == original) Left = replace;
			if (right == original) Right = replace;
		}

		public IEnumerable<IValue> GetUses()
		{
			yield return left;
			yield return right;
		}

		public override void ForceDestroy()
		{
			left.Users.Decrement(this);
			left = null;
			right.Users.Decrement(this);
			right = null;
		}
	}
}
