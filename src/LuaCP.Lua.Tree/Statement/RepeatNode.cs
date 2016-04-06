using LuaCP.IR.Instructions;
using LuaCP.IR;
using LuaCP.Tree;

namespace LuaCP.Lua.Tree.Statement
{
	public class RepeatNode : Node
	{
		public readonly IValueNode Test;
		public readonly INode Body;

		public RepeatNode(INode body, IValueNode test)
		{
			Test = test;
			Body = body;
		}

		public override BlockBuilder Build(BlockBuilder builder)
		{
			BlockBuilder testBranch = builder.Continue();

			BlockBuilder continueBranch = builder.Continue();

			BlockBuilder bodyBranch = builder.MakeLoop(new LoopState(testBranch, continueBranch));
			builder.Block.AddLast(new Branch(bodyBranch.Block));

			BlockBuilder bodyEnd = Body.Build(bodyBranch);
			if (!bodyEnd.Block.IsTerminated()) bodyEnd.Block.AddLast(new Branch(testBranch.Block));
            
			testBranch = bodyEnd.Continue();
			IValue test;
			testBranch = Test.BuildAsValue(testBranch, out test);
			testBranch.Block.AddLast(new BranchCondition(test, bodyBranch.Block, continueBranch.Block));

			return continueBranch;
		}
	}
}

