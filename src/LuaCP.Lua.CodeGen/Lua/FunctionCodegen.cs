using LuaCP.IR.Components;
using LuaCP.Graph;
using System.CodeDom.Compiler;

namespace LuaCP.CodeGen.Lua
{
    public sealed class FunctionCodegen
    {
        private readonly FunctionState state;

        public FunctionCodegen(Function state)
            : this(new FunctionState(state))
        {
        }

        public FunctionCodegen(FunctionState state)
        {
            this.state = state;
        }

        private void WriteBlock(Block block, IndentedTextWriter writer)
        {
            new BlockCodegen(block, state, writer).Write();
        }

        public void Write(IndentedTextWriter writer)
        {
            writer.Write("function(");
            bool first = true;
            foreach (Argument argument in state.Function.Arguments)
            {
                if (!first)
                {
                    writer.Write(", ");
                }
                else
                {
                    first = true;
                }

                writer.Write(state.Temps[argument]);
            }
            writer.WriteLine(")");

            writer.Indent++;
            foreach (Block block in state.Function.EntryPoint.ReachableLazy())
            {
                WriteBlock(block, writer);
            }
            writer.Indent--;

            writer.WriteLine("end");
        }
    }
}

