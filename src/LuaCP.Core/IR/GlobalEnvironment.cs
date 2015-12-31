using LuaCP.IR.User;

namespace LuaCP.IR
{
	public class GlobalEnvironment : IValue
	{
		public ValueKind Kind
		{
			get { return ValueKind.Value; }
		}

		private readonly CountingSet<IUser<IValue>> users = new CountingSet<IUser<IValue>>();

		public CountingSet<IUser<IValue>> Users { get { return users; } }
	}
}

