using LuaCP.IR.Instructions;
using LuaCP.Reporting;

namespace LuaCP.Tree.Statement
{
    public class BreakNode : Node
    {
        public override BlockBuilder Build(BlockBuilder builder)
        {
            if (builder.LoopState == null)
            {
                builder.Block.Function.Module.Reporter.Report(ReportLevel.Error, "Cannot break outside loop", Position);
            }
            else
            {
                using (BlockWriter writer = new BlockWriter(builder, this))
                {
                    writer.Add(new Branch(builder.LoopState.End.Block));
                }
            }
            return builder;
        }
    }

    public class ContinueNode : Node
    {
        public override BlockBuilder Build(BlockBuilder builder)
        {
            if (builder.LoopState == null)
            {
                builder.Block.Function.Module.Reporter.Report(ReportLevel.Error, "Cannot break outside loop", Position);
            }
            else
            {
                using (BlockWriter writer = new BlockWriter(builder, this))
                {
                    writer.Add(new Branch(builder.LoopState.Test.Block));
                }
            }
            return builder;
        }
    }
}

