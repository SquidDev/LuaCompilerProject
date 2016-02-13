using LuaCP.IR.Instructions;
using LuaCP.Tree;

namespace LuaCP.Lua.Tree.Statement
{
	public class GoToNode : Node
	{
		public readonly string Name;

		public GoToNode(string name)
		{
			Name = name;
		}

		public override BlockBuilder Build(BlockBuilder builder)
		{
			builder.Block.AddLast(new Branch(builder.Get<LabelScope>().Get(Name, this)));
			return builder.Continue();
		}
	}

	public class LabelNode : Node
	{
		public readonly string Name;

		public LabelNode(string name)
		{
			Name = name;
		}

		public override BlockBuilder Build(BlockBuilder builder)
		{
			BlockBuilder next = builder.Continue();
			builder.Block.AddLast(new Branch(next.Block));
			next.Get<LabelScope>().Declare(Name, next);
			return next;
		}
	}
}
