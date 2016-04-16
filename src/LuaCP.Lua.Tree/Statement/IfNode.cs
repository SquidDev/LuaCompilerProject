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

			BlockBuilder successStart = builder.MakeChild();
			BlockBuilder success = Success.Build(successStart);
			if (!success.Block.IsTerminated()) success.Block.AddLast(new Branch(end.Block));

			BlockBuilder failureStart = builder.MakeChild();
			BlockBuilder failure = Failure.Build(failureStart);
			if (!failure.Block.IsTerminated()) failure.Block.AddLast(new Branch(end.Block));

			builder.Block.AddLast(new BranchCondition(test, successStart.Block, failureStart.Block));

			return end;
		}
	}
}

