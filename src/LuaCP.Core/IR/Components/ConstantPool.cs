using System.Collections.Generic;

namespace LuaCP.IR.Components
{
	/// <summary>
	/// A pool of constants.
	/// This allows us to count usage of constants
	/// </summary>
	public sealed class ConstantPool
	{
		private readonly Dictionary<Literal, Constant> constants = new Dictionary<Literal, Constant>();

		public Constant this[Literal value]
		{
			get
			{
				Constant result;
				if (!constants.TryGetValue(value, out result))
				{
					result = new Constant(value);
					constants.Add(value, result);
				}
				return result;
			}
		}

		public Constant this[string value]
		{
			get { return this[new Literal.String(value)]; } 
		}

		public Constant this[bool value]
		{
			get { return this[new Literal.Boolean(value)]; } 
		}

		public Constant this[double value]
		{
			get { return this[new Literal.Number(value)]; } 
		}

		public Constant this[int value]
		{
			get { return this[new Literal.Integer(value)]; } 
		}

		public Constant Nil { get { return this[Literal.Nil]; } }
	}
}
