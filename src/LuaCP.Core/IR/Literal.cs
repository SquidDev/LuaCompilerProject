using System;

namespace LuaCP.IR
{
	public enum LiteralKind : byte
	{
		String = 0,
		Integer = 1,
		Number = 2,
		Boolean = 3,
		Nil = 4,
	}

	public class Literal : IEquatable<Literal>
	{
		#region Implementations

		[Serializable]
		public sealed class String : Literal
		{
			internal readonly string item;

			public string Item { get { return item; } }

			public String(string item)
				: base(LiteralKind.String)
			{
				this.item = item;
			}

			public override string ToString()
			{
				return '"' + item.Replace("\n", "\\n") + '"';
			}
		}

		[Serializable]
		public sealed class Integer : Literal
		{
			internal readonly int item;

			public int Item { get { return item; } }

			public Integer(int item)
				: base(LiteralKind.Integer)
			{
				this.item = item;
			}

			public override string ToString()
			{
				return item.ToString();
			}
		}

		[Serializable]
		public sealed class Number : Literal
		{
			internal readonly double item;

			public double Item { get { return item; } }

			public Number(double item)
				: base(LiteralKind.Number)
			{
				this.item = item;
			}

			public override string ToString()
			{
				return item.ToString();
			}
		}

		[Serializable]
		public sealed class Boolean : Literal
		{
			internal readonly bool item;

			public bool Item { get { return this.item; } }

			public Boolean(bool item)
				: base(LiteralKind.Boolean)
			{
				this.item = item;
			}

			public override string ToString()
			{
				return item.ToString();
			}
		}

		private static readonly Literal nil = new Literal(LiteralKind.Nil);
		private static readonly Literal f = new Boolean(false);
		private static readonly Literal t = new Boolean(false);

		public static Literal Nil { get { return nil; } }

		public static Literal False { get { return f; } }

		public static Literal True { get { return t; } }

		#endregion

		private readonly LiteralKind kind;

		public LiteralKind Kind { get { return kind; } }

		internal Literal(LiteralKind kind)
		{
			this.kind = kind;
		}

		public sealed override int GetHashCode()
		{
			unchecked
			{
				switch (Kind)
				{
					case LiteralKind.String:
						{
							string item = ((Literal.String)this).item;
							int num = 0;
							return -1640531527 + (item == null ? 0 : item.GetHashCode()) + (num << 6) + (num >> 2);
						}
					case LiteralKind.Integer:
						{
							int integer = ((Literal.Integer)this).item;
							int num = 1;
							return -1640531527 + integer + (num << 6) + (num >> 2);
						}
					case LiteralKind.Number:
						{
							double number = ((Literal.Number)this).item;
							int num = 2;
							return -1640531527 + number.GetHashCode() + (num << 6) + (num >> 2);
						}
					case LiteralKind.Boolean:
						{
							bool boolean = ((Literal.Boolean)this).item;
							int num = 3;
							return -1640531527 + (boolean ? 1 : 0) + (num << 6) + (num >> 2);
						}
					case LiteralKind.Nil:
						{
							return -1640531527 + (4 << 6) + (4 >> 2);
						}
				}
			}
			
			return 0;
		}

		public bool Equals(Literal obj)
		{
			if (this == null) return obj == null;
			if (obj == null) return false;
			LiteralKind tag = kind;
			if (tag == obj.kind)
			{
				switch (tag)
				{
					case LiteralKind.String:
						{
							Literal.String a = (Literal.String)this;
							Literal.String b = (Literal.String)obj;
							return string.Equals(a.item, b.item);
						}
					case LiteralKind.Integer:
						{
							Literal.Integer a = (Literal.Integer)this;
							Literal.Integer b = (Literal.Integer)obj;
							return a.item == b.item;
						}
					case LiteralKind.Number:
						{
							Literal.Number a = (Literal.Number)this;
							Literal.Number b = (Literal.Number)obj;
							return a.item == b.item;
						}
					case LiteralKind.Boolean:
						{
							Literal.Boolean boolean = (Literal.Boolean)this;
							Literal.Boolean boolean2 = (Literal.Boolean)obj;
							return boolean.item == boolean2.item;
						}
				}
			}
			return false;
		}

		public sealed override bool Equals(object obj)
		{
			Literal Literal = obj as Literal;
			return Literal != null && this.Equals(Literal);
		}

		public override string ToString()
		{
			return Kind.ToString();
		}

		public bool IsTruthy()
		{
			switch (Kind)
			{
				case LiteralKind.Integer:
				case LiteralKind.Number:
				case LiteralKind.String:
					return true;
				case LiteralKind.Boolean:
					return ((Boolean)this).Item;
				case LiteralKind.Nil:
					return false;
			}
        	
			return false;
		}

		public bool IsNumeric()
		{
			return Kind == LiteralKind.Integer || Kind == LiteralKind.Number;
		}

		public static explicit operator int(Literal literal)
		{
			return ((Integer)literal).Item;
		}

		public static explicit operator string(Literal literal)
		{
			return ((String)literal).Item;
		}

		public static explicit operator double(Literal literal)
		{
			return literal is Integer ? ((Integer)literal).Item : ((Number)literal).Item;
		}

		public static explicit operator bool(Literal literal)
		{
			return ((Boolean)literal).Item;
		}
        
		public static implicit operator Literal(int literal)
		{
			return new Literal.Integer(literal);
		}

		public static implicit operator Literal(string literal)
		{
			return new Literal.String(literal);
		}

		public static implicit operator Literal(double literal)
		{
			return new Literal.Number(literal);
		}

		public static implicit operator Literal(bool literal)
		{
			return literal ? True : False;
		}
	}
}
