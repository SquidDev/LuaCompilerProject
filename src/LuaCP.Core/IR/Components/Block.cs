using System.Collections.Generic;
using LuaCP.Graph;
using LuaCP.IR.Instructions;
using LuaCP.IR.User;
using LuaCP.Collections;

namespace LuaCP.IR.Components
{
	/// <summary>
	/// Represents a series of instructions, ending in a branch or return
	/// </summary>
	public sealed partial class Block : ICollection<Instruction>, IEnumerable<Instruction>, IUsable<Block>, IGraphNode<Block>
	{
		private readonly CountingSet<IUser<Block>> users = new CountingSet<IUser<Block>>();
		internal readonly HashSet<Phi> phiNodes = new HashSet<Phi>();
		private readonly Function function;

		public Block(Function func)
		{
			function = func;
			func.Blocks.Add(this);
            
			func.Dominators.Invalidate();
		}

		public CountingSet<IUser<Block>> Users { get { return users; } }

		public Function Function { get { return function; } }

		public ISet<Phi> PhiNodes { get { return phiNodes; } }
	}
}
