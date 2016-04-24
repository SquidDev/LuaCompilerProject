using System.IO;
using LuaCP.Collections;
using LuaCP.IR.Components;
using LuaCP.IR.Instructions;
using System;
using System.Linq;
using LuaCP.IR;
using LuaCP.IR.User;

namespace LuaCP.Debug
{
	public class Exporter : Formatter
	{
		protected readonly IndentedTextWriter Writer;

		public Exporter(TextWriter writer)
		{
			Writer = new IndentedTextWriter(writer);
		}

		public virtual void ModuleLong(Module module)
		{
			foreach (Function function in module.Functions)
			{
				FunctionLong(function);
			}
		}

		public virtual void FunctionLong(Function function)
		{
			Writer.WriteLine("Function: " + function.Module.Functions.FindIndex(function));
			function.Dominators.Evaluate();

			Writer.Indent++;
			NodeNumberer numberer = new NodeNumberer(function);
			Writer.WriteLine("Closed: {0}", function.ClosedUpvalues.Count);
			Writer.WriteLine("Open: {0}", function.OpenUpvalues.Count);

			foreach (Block block in function.Blocks)
			{
				BlockLong(block, numberer);
			}
			Writer.Indent--;
		}

		public virtual void BlockLong(Block block, NodeNumberer numberer)
		{
			Writer.Write("Block: ");
			Writer.WriteLine(numberer.PrettyGetBlock(block));
			Writer.Indent++;

			Writer.Write("Dominated by: ");
			Writer.WriteLine(block.ImmediateDominator == null ? "Nothing" : numberer.PrettyGetBlock(block.ImmediateDominator));

			Writer.Write("Frontier: ");
			Writer.WriteLine(block.DominanceFrontier.Count == 0 ? "<empty>" : String.Join(", ", block.DominanceFrontier.Select(numberer.PrettyGetBlock)));
            
			foreach (Phi phi in block.PhiNodes)
			{
				Phi(phi, Writer, numberer);

				Writer.Write(" Total: " + phi.Users.TotalCount + ", Unique: " + phi.Users.UniqueCount);
				Writer.Write(" => " + String.Join(", ", phi.Users.Select<IUser<IValue>, string>(x => Choose(x, numberer))));
				Writer.WriteLine();
			}

			foreach (Instruction insn in block)
			{
				InstructionLong(insn, Writer, numberer);
				if (insn is IValue)
				{
					IValue value = (IValue)insn;
					Writer.Write(" Total: " + value.Users.TotalCount + ", Unique: " + value.Users.UniqueCount + " ");
					Writer.Write(" => " + String.Join(", ", value.Users.Select<IUser<IValue>, string>(x => Choose(x, numberer))));
				}
				Writer.WriteLine();
			}
			Writer.Indent--;
		}
	}
}
