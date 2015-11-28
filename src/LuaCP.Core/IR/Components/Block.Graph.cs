using System.Collections.Generic;
using System.Linq;
using LuaCP.Graph;
using LuaCP.IR.Instructions;
using LuaCP.IR.User;

namespace LuaCP.IR.Components
{
	public partial class Block : ICollection<Instruction>, IEnumerable<Instruction>, IUsable<Block>, IGraphNode<Block>
	{
		public Block ImmediateDominator { get; set; }

		private readonly HashSet<Block> dominatorTreeChildren = new HashSet<Block>();
		private readonly HashSet<Block> dominanceFrontier = new HashSet<Block>();

		public HashSet<Block> DominatorTreeChildren { get { return dominatorTreeChildren; } }

		public HashSet<Block> DominanceFrontier { get { return dominanceFrontier; } }

		public IEnumerable<Block> Next { get { return Last == null ? Enumerable.Empty<Block>() : Last.NextBlocks(); } }

		public IEnumerable<Block> Previous
		{
			get
			{
				return Users
                    .OfType<Instruction>()
                    .Where(x => x.Opcode == Opcode.Branch || x.Opcode == Opcode.BranchCondition)
                    .Select(x => x.Block);
			}
		}
	}
}

