using System.Collections.Generic;
using System.Linq;

namespace LuaCP.Tree.Statement
{
	public class BlockNode : Node
	{
		public readonly IReadOnlyList<INode> Nodes;

		public BlockNode(IEnumerable<INode> nodes)
		{
			Nodes = nodes.ToList();
		}

		public override BlockBuilder Build(BlockBuilder builder)
		{
			foreach (INode node in Nodes)
			{
				builder = node.Build(builder);
			}
			return builder;
		}
	}
}

