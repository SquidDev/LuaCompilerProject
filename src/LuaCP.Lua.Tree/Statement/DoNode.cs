using LuaCP.IR.Instructions;

namespace LuaCP.Tree.Statement
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
            builder.Block.AddLast(new Branch(next.Block));

            return next;
        }
    }
}

