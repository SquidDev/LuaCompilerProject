using LuaCP.IR;
using LuaCP.Tree;

namespace LuaCP.Lua.Tree.Expression
{
	public class ParenthesisNode : ValueNode
	{
		public IValueNode Value;

		public ParenthesisNode(IValueNode value)
		{
			Value = value;
		}

		public override BlockBuilder Build(BlockBuilder builder, out IValue result)
		{
			return Value.BuildAsValue(builder, out result);
		}
	}
}

