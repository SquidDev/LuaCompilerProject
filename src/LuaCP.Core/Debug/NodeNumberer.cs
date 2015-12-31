using System.Collections.Generic;
using System.Linq;
using LuaCP.Graph;
using LuaCP.IR.Components;
using LuaCP.IR.Instructions;

namespace LuaCP.Debug
{
	/// <summary>
	/// Numbers every node to enable easier identification
	/// </summary>
	public class NodeNumberer
	{
		private readonly Dictionary<Instruction, int> insnLookup = new Dictionary<Instruction, int>();
		private readonly Dictionary<Block, int> blockLookup = new Dictionary<Block, int>();
		private readonly Dictionary<Phi, int> phiLookup = new Dictionary<Phi, int>();
		private readonly Function function;

		public Function Function { get { return function; } }

		public NodeNumberer(Function function)
		{
			this.function = function;

			int blockIndex = 0;
			int insnIndex = 0;
			int phiIndex = 0;
			// Order from entrypoint but ensure we include all blocks
			foreach (Block block in function.EntryPoint.ReachableLazy().Concat(function.Blocks).Distinct())
			{
				blockLookup.Add(block, blockIndex);
				blockIndex++;

				foreach (Instruction insn in block)
				{
					insnLookup.Add(insn, insnIndex);
					insnIndex++;
				}
                
				foreach (Phi phi in block.PhiNodes)
				{
					phiLookup.Add(phi, phiIndex);
					phiIndex++;
				}
			}
		}

		public bool TryGetBlock(Block block, out int value)
		{
			return blockLookup.TryGetValue(block, out value);
		}

		public bool TryGetPhi(Phi phi, out int value)
		{
			return phiLookup.TryGetValue(phi, out value);
		}

		public bool TryGetInstruction(Instruction insn, out int value)
		{
			return insnLookup.TryGetValue(insn, out value);
		}

		public string PrettyGetBlock(Block block)
		{
			int value;
			return TryGetBlock(block, out value) ? "%" + value : "<unknown block>";
		}

		public string PrettyGetPhi(Phi phi)
		{
			int value;
			return TryGetPhi(phi, out value) ? "@" + value : "<unknown phi>";
		}

		public string PrettyGetInstruction(Instruction insn)
		{
			int value;
			return TryGetInstruction(insn, out value) ? "#" + value : "<unknown instruction " + insn + ">";
		}

		public int GetBlock(Block block)
		{
			return blockLookup[block];
		}

		public int GetPhi(Phi phi)
		{
			return phiLookup[phi];
		}

		public int GetInstruction(Instruction insn)
		{
			return insnLookup[insn];
		}
	}
}
