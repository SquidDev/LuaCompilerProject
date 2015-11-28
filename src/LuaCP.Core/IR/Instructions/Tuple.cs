using System.Collections.Generic;
using LuaCP.IR.User;
using System.Linq;

namespace LuaCP.IR.Instructions
{
	public sealed class TupleNew : ValueInstruction, IUser<IValue>
	{
		private readonly UsingList<IValue> values;
		private IValue remaining;

		public IList<IValue> Values { get { return values; } }

		public IValue Remaining { get { return remaining; } set { remaining = UserExtensions.Replace(this, remaining, value); } }

		public TupleNew(IEnumerable<IValue> values, IValue remaining)
			: base(Opcode.TupleNew, ValueKind.Value)
		{
			this.values = new UsingList<IValue>(this, values);
			Remaining = remaining;
		}

		public IEnumerable<IValue> GetUses()
		{
			return new IValue[] { remaining }.Concat(values);
		}

		public void Replace(IValue original, IValue replace)
		{
			if (remaining == original) Remaining = replace;
			values.Replace(original, replace);
		}

		public override void ForceDestroy()
		{
			remaining.Users.Decrement(this);
			remaining = null;
			values.Clear();
		}
	}

	public sealed class TupleGet : ValueInstruction, IUser<IValue>
	{
		private IValue tuple;

		public IValue Tuple
		{
			get { return tuple; } 
			set { tuple = UserExtensions.Replace(this, tuple, value); }
		}

		public readonly int Offset;

		public TupleGet(IValue tuple, int offset)
			: base(Opcode.TupleGet, ValueKind.Value)
		{
			Tuple = tuple;
			Offset = offset;
		}

		public IEnumerable<IValue> GetUses()
		{
			yield return tuple;
		}

		public void Replace(IValue original, IValue replace)
		{
			if (tuple == original) Tuple = replace;
		}

		public override void ForceDestroy()
		{
			tuple.Users.Decrement(this);
			tuple = null;
		}
	}

	public sealed class TupleRemainder : ValueInstruction, IUser<IValue>
	{
		private IValue tuple;

		public IValue Tuple
		{
			get { return tuple; } 
			set { tuple = UserExtensions.Replace(this, tuple, value); }
		}

		public readonly int Offset;

		public TupleRemainder(IValue tuple, int offset)
			: base(Opcode.TupleGet, ValueKind.Tuple)
		{
			Tuple = tuple;
			Offset = offset;
		}

		public IEnumerable<IValue> GetUses()
		{
			yield return tuple;
		}

		public void Replace(IValue original, IValue replace)
		{
			if (tuple == original) Tuple = replace;
		}

		public override void ForceDestroy()
		{
			tuple.Users.Decrement(this);
			tuple = null;
		}
	}
}
