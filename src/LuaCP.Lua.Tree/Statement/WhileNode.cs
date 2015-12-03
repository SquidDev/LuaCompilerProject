using LuaCP.IR;
using LuaCP.IR.Instructions;

namespace LuaCP.Tree.Statement
{
    public class WhileNode : Node
    {
        public readonly IValueNode Test;
        public readonly INode Body;

        public WhileNode(IValueNode test, INode body)
        {
            Test = test;
            Body = body;
        }

        public override BlockBuilder Build(BlockBuilder builder)
        {
            BlockBuilder testBranch = builder.MakeChild();
            builder.Block.AddLast(new Branch(testBranch.Block));

            BlockBuilder continueBranch = builder.Continue();

            BlockBuilder bodyBranch = builder.MakeLoop(new LoopState(testBranch, continueBranch));

            IValue test;
            Test.BuildAsValue(testBranch, out test).Block.AddLast(new BranchCondition(test, bodyBranch.Block, continueBranch.Block));

            BlockBuilder bodyEnd = Body.Build(bodyBranch);
            if (bodyEnd.Block.Last == null || !bodyEnd.Block.Last.Opcode.IsTerminator())
            {
                bodyEnd.Block.AddLast(new Branch(testBranch.Block));
            }

            return continueBranch;
        }
    }
}

