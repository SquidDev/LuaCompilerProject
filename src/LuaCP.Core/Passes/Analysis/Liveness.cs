using System;
using System.Collections.Generic;
using System.Linq;
using LuaCP.IR;
using LuaCP.IR.Components;
using LuaCP.IR.Instructions;
using LuaCP.IR.User;

namespace LuaCP.Passes.Analysis
{
	public static class Liveness
	{
		/// <summary>
		/// Compute the blocks a value is live in
		/// </summary>
		/// <param name="value">The value to check</param>
		/// <param name="definition">The block the variable is defined in</param>
		/// <returns>All blocks a value is live in</returns>
		public static HashSet<Block> LiveBlocks(IValue value, Block definition)
		{
			HashSet<Block> blocks = new HashSet<Block>() { definition };
			Queue<Block> worklist = new Queue<Block>();

			foreach (Block block in value.Users.OfType<Instruction>().Select(x => x.Block))
			{
				if (blocks.Add(block)) worklist.Enqueue(block);
			}

			foreach (Phi phi in value.Users.OfType<Phi>())
			{
				blocks.Add(phi.Block);
				foreach (var source in phi.Source)
				{
					if (source.Value == value && blocks.Add(source.Key))
					{
						worklist.Enqueue(source.Key);
					}
				}
			}

			while (worklist.Count > 0)
			{
				Block block = worklist.Dequeue();
				foreach (Block previous in block.Previous)
				{
					// Since the reference is live in the current block, the previous items
					// must either be live or a setting block
					if (blocks.Add(previous)) worklist.Enqueue(previous);
				}
			}

			return blocks;
		}

		/// <summary>
		/// Compute the boundary for a variable's liveness
		/// </summary>
		/// <returns>The boundary or <code>null</code> if no such boundary exists.</returns>
		/// <param name="value">The value to check liveness for</param>
		/// <param name="block">The active block</param>
		/// <param name="live">Set of all live blocks for this value</param>
		public static IUser<IValue> Boundary(IValue value, Block block, ICollection<Block> live)
		{
			if (!live.Contains(block)) throw new ArgumentException("Block is not live");

			var belongs = value as IBelongs<Block>;
			Block inital = belongs == null ? null : belongs.Owner;

			// If any succesor is live
			// FIXME: Handle loops with one block
			foreach (Block successor in block.Next)
			{
				if (successor != inital && live.Contains(successor)) return null;
			}

			Instruction insn = block.Last;
			while (insn != null)
			{
				IUser<IValue> user = insn as IUser<IValue>;
				if (user != null && value.Users.Contains(user)) return user;

				insn = insn.Previous;
			}

			foreach (Phi phi in block.PhiNodes.Reverse())
			{
				if (value.Users.Contains(phi)) return phi;
			}

			throw new InvalidOperationException("Block is marked as live yet successors are not live and there are no users");
		}
	}
}

