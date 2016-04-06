using LuaCP.IR.Instructions;
using LuaCP.Tree;

namespace LuaCP.Lua.Tree.Statement
{
	public class DoNode : Node
	{
		public readonly INode Body;

		public DoNode(INode body)
		{
			Body = body;
		}

		public override BlockBuilder Build(BlockBuilder builder)
		{
			BlockBuilder next = builder.Continue();

			builder = Body.Build(builder.MakeScope());

			if (!builder.Block.IsTerminated()) builder.Block.AddLast(new Branch(next.Block));

			return next;
		}
	}
}

