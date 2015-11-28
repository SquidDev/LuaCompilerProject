using LuaCP.IR.User;

namespace LuaCP.IR
{
	public enum ValueKind
	{
		Reference,
		Tuple,
		Value,
	}
	public interface IValue : IUsable<IValue>
	{
		ValueKind Kind { get; }
	}
}
