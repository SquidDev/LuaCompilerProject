using System;
using LuaCP.IR.User;
using LuaCP.Collections;

namespace LuaCP.IR
{
	public sealed class Constant : IValue, IEquatable<Constant>
	{
		private readonly Literal literal;
		private readonly CountingSet<IUser<IValue>> users = new CountingSet<IUser<IValue>>();

		public Literal Literal { get { return literal; } }

		internal Constant(Literal literal)
		{
			this.literal = literal;
		}

		public override string ToString()
		{
			return literal.ToString();
		}

		public CountingSet<IUser<IValue>> Users { get { return users; } }

		public override bool Equals(object obj)
		{
			return Equals(obj as Constant);
		}

		public bool Equals(Constant other)
		{
			return other != null && literal.Equals(other.literal);
		}

		public override int GetHashCode()
		{
			return literal.GetHashCode();
		}

		public ValueKind Kind { get { return ValueKind.Value; } }
	}
}

