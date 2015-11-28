using System.Collections.Generic;
using LuaCP.IR.User;

namespace LuaCP.IR.Instructions
{
	public sealed class Call : ValueInstruction, IUser<IValue>
	{
		private IValue method;

		public IValue Method
		{
			get { return method; } 
			set { method = UserExtensions.Replace(this, method, value); }
		}

		private IValue arguments;

		public IValue Arguments
		{ 
			get { return arguments; } 
			set { arguments = UserExtensions.Replace(this, arguments, value); }
		}

		public Call(IValue method, IValue arguments)
			: base(Opcode.Call, ValueKind.Tuple)
		{
			Method = method;
			Arguments = arguments;
		}

		public void Replace(IValue original, IValue replace)
		{
			if (method == original) Method = replace;
			if (arguments == original) Arguments = replace;
		}

		public IEnumerable<IValue> GetUses()
		{
			yield return method;			
			yield return arguments;
		}

		public override void ForceDestroy()
		{
			method.Users.Decrement(this);
			method = null;
			arguments.Users.Decrement(this);
			arguments = null;
		}
	}

}
