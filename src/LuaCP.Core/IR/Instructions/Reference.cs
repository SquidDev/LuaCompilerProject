using System.Collections.Generic;
using System.Linq;
using LuaCP.IR.User;
using LuaCP.IR.Components;

namespace LuaCP.IR.Instructions
{
	public sealed class ReferenceGet : ValueInstruction, IUser<IValue>
	{
		private IValue reference;

		public IValue Reference
		{
			get { return reference; } 
			set { reference = UserExtensions.Replace(this, reference, value); }
		}

		public ReferenceGet(IValue reference)
			: base(Opcode.ReferenceGet, ValueKind.Value)
		{
			Reference = reference;
		}

		public IEnumerable<IValue> GetUses()
		{
			yield return reference;
		}

		public void Replace(IValue original, IValue replace)
		{
			if (reference == original) Reference = replace;
		}

		public override void ForceDestroy()
		{
			reference.Users.Decrement(this);
			reference = null;
		}
	}

	public sealed class ReferenceSet : Instruction, IUser<IValue>
	{
		private IValue reference;

		public IValue Reference
		{
			get { return reference; } 
			set { reference = UserExtensions.Replace(this, reference, value); }
		}

		private IValue val;

		public IValue Value
		{
			get { return val; } 
			set { val = UserExtensions.Replace(this, val, value); }
		}

		public ReferenceSet(IValue reference, IValue value)
			: base(Opcode.ReferenceSet)
		{
			Reference = reference;
			Value = value;
		}

		public IEnumerable<IValue> GetUses()
		{
			yield return Reference;
			yield return Value;
		}

		public void Replace(IValue original, IValue replace)
		{
			if (reference == original) Reference = replace;
			if (val == original) Value = replace;
		}

		public override void ForceDestroy()
		{
			reference.Users.Decrement(this);
			val.Users.Decrement(this);
			reference = null;
		}
	}

	public sealed class ReferenceNew : ValueInstruction, IUser<IValue>
	{
		private IValue val;

		public IValue Value
		{
			get { return val; } 
			set { val = UserExtensions.Replace(this, val, value); }
		}

		public ReferenceNew(IValue value)
			: base(Opcode.ReferenceNew, ValueKind.Reference)
		{
			Value = value;
		}

		public IEnumerable<IValue> GetUses()
		{
			yield return val;
		}

		public void Replace(IValue original, IValue replace)
		{
			if (val == original) Value = replace;
		}

		public override void ForceDestroy()
		{
			val.Users.Decrement(this);
			val = null;
		}
	}

	public sealed class ClosureNew : ValueInstruction, IUser<IValue>, IUser<Function>
	{
		private Function function;

		public Function Function
		{ 
			get { return function; }
			set { function = UserExtensions.Replace(this, function, value); }
		}

		private UsingList<IValue> openUpvalues;

		public IList<IValue> OpenUpvalues { get { return openUpvalues; } }

		private UsingList<IValue> closedUpvalues;

		public IList<IValue> ClosedUpvalues { get { return closedUpvalues; } }

		public ClosureNew(Function function, IEnumerable<IValue> open, IEnumerable<IValue> closed)
			: base(Opcode.ClosureNew, ValueKind.Value)
		{
			Function = function;
			openUpvalues = new UsingList<IValue>(this, open);
			closedUpvalues = new UsingList<IValue>(this, closed);
		}

		public IEnumerable<IValue> GetUses()
		{
			return OpenUpvalues.Concat(ClosedUpvalues);
		}

		public void Replace(IValue original, IValue replace)
		{
			openUpvalues.Replace(original, replace);
			closedUpvalues.Replace(original, replace);
		}

		IEnumerable<Function> IUser<Function>.GetUses()
		{
			yield return function;
		}

		public void Replace(Function original, Function replace)
		{
			if (function == original) Function = replace;
		}

		public override void ForceDestroy()
		{
			function.Users.Decrement(this);
			function = null;
			openUpvalues.Clear();
			closedUpvalues.Clear();
		}
	}
}
