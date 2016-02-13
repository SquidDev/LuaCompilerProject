using LuaCP.IR;
using LuaCP.IR.Instructions;
using LuaCP.Tree;

namespace LuaCP.Lua.Tree.Statement
{
	public class IfNode : Node
	{
		public readonly IValueNode Test;
		public readonly INode Success;
		public readonly INode Failure;

		public IfNode(IValueNode test, INode success, INode failure)
		{
			Test = test;
			Success = success;
			Failure = failure;
		}

		public override BlockBuilder Build(BlockBuilder builder)
		{
			IValue test;
			builder = Test.BuildAsValue(builder, out test);

			BlockBuilder end = builder.Continue();

			BlockBuilder success = Success.Build(builder.MakeChild());
			success.Block.AddLast(new Branch(end.Block));

			BlockBuilder failure = Failure.Build(builder.MakeChild());
			failure.Block.AddLast(new Branch(end.Block));

			builder.Block.AddLast(new BranchCondition(test, success.Block, failure.Block));

			return end;
		}
	}
}

