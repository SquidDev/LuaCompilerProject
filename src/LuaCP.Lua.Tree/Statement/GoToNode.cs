using LuaCP.IR.Instructions;

namespace LuaCP.Tree.Statement
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
            builder.Block.AddLast(new Branch(builder.Labels.Get(Name, this)));
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
            next.Labels.Declare(Name, next);
            return next;
        }
    }
}
