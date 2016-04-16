using System.Linq;
using LuaCP.IR;
using LuaCP.IR.Components;
using LuaCP.IR.Instructions;
using LuaCP.IR.User;
using LuaCP.Passes.Analysis;

namespace LuaCP.Passes.Optimisation
{
	/// <summary>
	/// Removes useless constructs including: 
	///  - Unused instructions
	///  - Unused phi nodes
	///  - Conditions on constants
	///  - Branches just composed of a jump instruction with no following phi node
	/// </summary>
	public static class DeadCode
	{
		public static Pass<Block> Runner { get { return Run; } }

		private static bool Run(PassManager data, Block block)
		{
			bool changed = false;
			Function function = block.Function;

			// Remove unused phi nodes
			foreach (Phi phi in block.PhiNodes.ToList())
			{
				if (phi.Users.UniqueCount == 0)
				{
					phi.Remove();
					changed = true;
				}
			}

			// Merge blocks together
			if (block.Next.Count() == 1)
			{
				Block next = block.Next.First();
				if (next.Previous.Count() == 1)
				{
					block.Last.Remove();
					foreach (Instruction insn in next)
					{
						insn.Block.Remove(insn);
						block.AddLast(insn);
					}
					next.ReplaceWith(block);

					next.Function.Dominators.Invalidate();
					if (function.EntryPoint == next) function.EntryPoint = block;
					changed = true;
				}
			}
			
			foreach (Instruction element in block)
			{
				ValueInstruction insn = element as ValueInstruction;
				// We presume that operations don't have a side effect.
				// If they do then something is breaking
				if (insn != null && insn.Users.UniqueCount == 0 && insn.IsSideFree())
				{
					element.Remove();
					changed = true;
					continue;
				}


				switch (element.Opcode)
				{
					case Opcode.ValueCondition:
						{
							ValueCondition condition = (ValueCondition)element;

							// Constant term
							Constant c = condition.Test as Constant;
							if (c != null)
							{
								condition.ReplaceWithAndRemove(c.Literal.IsTruthy() ? condition.Success : condition.Failure);
								changed = true;
							}
							break;
						}
					case Opcode.BranchCondition:
						{
							BranchCondition branch = (BranchCondition)element;
							Constant c = branch.Test as Constant;
							if (c != null)
							{
								// Fold constants!
								Block success = branch.Success, failure = branch.Failure;
								branch.Remove();
								if (c.Literal.IsTruthy())
								{
									block.AddLast(new Branch(success));
									RemoveBranch(block, failure);
								}
								else
								{
									block.AddLast(new Branch(failure));
									RemoveBranch(block, success);
								}
								changed = true;
							}
							break;
						}
				}
			}
			
			return changed;
		}

		public static void RemoveBranch(Block current, Block next)
		{
			next.Function.Dominators.Invalidate();
			if (!next.Previous.Contains(current))
			{
				foreach (Phi phi in next.PhiNodes) phi.Source.Remove(current);
			}
		}
	}
}
