using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using LuaCP.IR.Components;
using LuaCP.IR.Instructions;
using LuaCP.Collections;

namespace LuaCP.Debug
{
	public sealed class DotExporter : Exporter
	{
		private readonly TextWriter normalWriter;
		private readonly bool withDominators;

		public DotExporter(TextWriter writer, bool withDominators = true)
			: base(new EscapeWriter(writer))
		{
			normalWriter = writer;
			this.withDominators = withDominators;
		}

		public override void ModuleLong(Module module)
		{
			normalWriter.WriteLine("strict digraph {");
			normalWriter.WriteLine("node [shape=box]");
			base.ModuleLong(module);
			normalWriter.WriteLine("}");
		}

		public override void FunctionLong(Function function)
		{
			int index = function.Module.Functions.FindIndex(function);
			writer.Write("subgraph cluster_");
			writer.Write(index);
			writer.WriteLine("{");
			writer.Write("label = \"Function ");
			writer.Write(index);
			writer.WriteLine("\";");

			function.Dominators.Evaluate();
			NodeNumberer numberer = new NodeNumberer(function);

			foreach (Block block in function.Blocks)
			{
				BlockLong(block, numberer);
			}

			writer.WriteLine("}");
		}

		public override void BlockLong(Block block, NodeNumberer numberer)
		{
			int functionIndex = block.Function.Module.Functions.FindIndex(block.Function);

			string name = "block_" + functionIndex + "_" + numberer.GetBlock(block);
			normalWriter.Write(name);
			normalWriter.Write("[label =<");
            
			normalWriter.Write("<b>Block: ");
			Block(block, writer, numberer);
			normalWriter.Write("</b><br />");
            
			writer.Write("Dominator: ");
			writer.WriteLine(block.ImmediateDominator == null ? "Nothing" : numberer.PrettyGetBlock(block.ImmediateDominator));
			normalWriter.Write("<br />");

			foreach (Phi phi in block.PhiNodes)
			{
				Phi(phi, writer, numberer);
				normalWriter.Write("<br align=\"left\" />");
			}

			foreach (Instruction insn in block)
			{
				InstructionLong(insn, writer, numberer);
				normalWriter.Write("<br align=\"left\"/>");
			}
            
			normalWriter.Write(">];\n");

			Instruction last = block.Last;
			if (last != null)
			{
				foreach (Block next in last.NextBlocks())
				{
					normalWriter.Write(name);
					normalWriter.Write(" -> ");
					normalWriter.Write("block_");
					normalWriter.Write(functionIndex);
					normalWriter.Write("_");
					normalWriter.Write(numberer.GetBlock(next));
					normalWriter.WriteLine(";");
				}
			}

			if (withDominators)
			{
				// Dom tree
				normalWriter.Write("dom_");
				normalWriter.Write(name);
				normalWriter.Write(" [label=<<b>Block: ");
				Block(block, writer, numberer);
				normalWriter.Write("</b>");
				if (block.DominanceFrontier.Count > 0)
				{
					normalWriter.Write("<br />Doms: ");
					foreach (Block f in block.DominanceFrontier)
					{
						Block(f, writer, numberer);
						normalWriter.Write(", ");
					}
				}

				if (block.DominatorTreeChildren.Count > 0)
				{
					normalWriter.Write("<br />Children: ");
					foreach (Block f in block.DominatorTreeChildren)
					{
						Block(f, writer, numberer);
						normalWriter.Write(", ");
					}
				}

				normalWriter.WriteLine(">];");
				if (block.ImmediateDominator != null)
				{
					normalWriter.Write("dom_");
					normalWriter.Write(name);
					normalWriter.Write("-> dom_block_");
					normalWriter.Write(functionIndex);
					normalWriter.Write("_");
					normalWriter.Write(numberer.GetBlock(block.ImmediateDominator));
					normalWriter.WriteLine(";");
				}
			}
		}

		private const string GraphVizPath = "dot";

		public static void Write(Module module, bool doms = true)
		{
			string tempWhole = Path.GetTempFileName();
			string tempName = tempWhole + ".png";
			ProcessStartInfo startInfo = new ProcessStartInfo
			{
				FileName = GraphVizPath,
				Arguments = "-Tpng",
				UseShellExecute = false,
				CreateNoWindow = true,
				RedirectStandardInput = true,
				RedirectStandardOutput = true,
				RedirectStandardError = true
			};

			using (StreamWriter output = new StreamWriter(File.OpenRead(tempName)))
			{
				using (Process process = Process.Start(startInfo))
				{
					using (StreamWriter standardInput = process.StandardInput)
					{
						new DotExporter(standardInput, doms).ModuleLong(module);
					}

					using (StreamReader standardOutput = process.StandardOutput)
					{
						standardOutput.BaseStream.CopyTo(output.BaseStream);
					}

					using (StreamReader standardErr = process.StandardError)
					{
						Console.ForegroundColor = ConsoleColor.Yellow;
						Console.Error.WriteLine(standardErr.ReadToEnd());
						Console.ForegroundColor = ConsoleColor.White;
					}
				}
			}

			Process viewer = new Process();
			viewer.StartInfo.FileName = tempName;
			viewer.StartInfo.CreateNoWindow = true;
			viewer.EnableRaisingEvents = true;
			viewer.Exited += (sender, e) =>
			{
				File.Delete(tempName);
				if (File.Exists(tempWhole)) File.Delete(tempWhole);
			};
			viewer.Start();
		}

		private sealed class EscapeWriter : TextWriter
		{
			private readonly TextWriter writer;

			public EscapeWriter(TextWriter writer)
			{
				this.writer = writer;
			}

			public override Encoding Encoding { get { return Encoding.ASCII; } }

			public override void Write(char value)
			{
				switch (value)
				{
					case '>':
						writer.Write("&gt;");
						break;
					case '<':
						writer.Write("&lt;");
						break;
					case '&':
						writer.Write("&amp;");
						break;
					default:
						writer.Write(value);
						break;
				}
			}
		}
	}
}

