using System.IO;
using LuaCP.Collections;
using LuaCP.IR.Components;
using LuaCP.IR.Instructions;
using System;
using System.Linq;

namespace LuaCP.Debug
{
	public class Exporter : Formatter
	{
		protected readonly TextWriter writer;

		public Exporter(TextWriter writer)
		{
			this.writer = writer;
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
			writer.WriteLine("Function: " + function.Module.Functions.FindIndex(function));
			function.Dominators.Evaluate();

			NodeNumberer numberer = new NodeNumberer(function);
			writer.WriteLine("Closed: {0}", function.ClosedUpvalues.Count);
			writer.WriteLine("Open: {0}", function.OpenUpvalues.Count);

			foreach (Block block in function.Blocks)
			{
				BlockLong(block, numberer);
			}
		}

		public virtual void BlockLong(Block block, NodeNumberer numberer)
		{
			writer.Write("Block: ");
			writer.WriteLine(numberer.PrettyGetBlock(block));

			writer.Write("Dominated by: ");
			writer.WriteLine(block.ImmediateDominator == null ? "Nothing" : numberer.PrettyGetBlock(block.ImmediateDominator));

			writer.Write("Frontier: ");
			writer.WriteLine(block.DominanceFrontier.Count == 0 ? "<empty>" : String.Join(", ", block.DominanceFrontier.Select(numberer.PrettyGetBlock)));
            
			foreach (Phi phi in block.PhiNodes)
			{
				Phi(phi, writer, numberer);
				writer.WriteLine();
			}

			foreach (Instruction insn in block)
			{
				InstructionLong(insn, writer, numberer);
				writer.WriteLine();
			}
		}
	}
}
