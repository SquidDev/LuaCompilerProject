using LuaCP.IR.User;
using LuaCP.Collections;

namespace LuaCP.IR.Components
{
	public sealed class Argument : IValue, IBelongs<Function>
	{
		private readonly CountingSet<IUser<IValue>> users = new CountingSet<IUser<IValue>>();
		private readonly Function function;
		private readonly string name;
		private readonly ValueKind kind;

		internal Argument(Function function, string name, ValueKind kind)
		{
			this.function = function;
			this.name = name;
			this.kind = kind;
		}

		public static Argument Dots(Function function)
		{
			return new Argument(function, "...", ValueKind.Tuple);
		}

		public static Argument Arg(Function function, string name)
		{
			return new Argument(function, name, ValueKind.Value);
		}

		public CountingSet<IUser<IValue>> Users { get { return users; } }

		public Function Function { get { return function; } }

		public ValueKind Kind { get { return kind; } }

		public string Name { get { return name; } }

		public Function Owner { get { return function; } }
	}
}

