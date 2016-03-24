using LuaCP.IR.Components;
using LuaCP.IR.Instructions;
using System.Collections.Generic;
using System.Linq;
using LuaCP.IR;

namespace LuaCP.Passes.Optimisation
{
	/// <summary>
	/// Converts branch conditions to value conditions
	/// </summary>
	public static class BranchToValue
	{
		public static Pass<Block> Runner { get { return Run; } }

		public static bool Run(PassManager data, Block block)
		{
			Instruction insn = block.Last;
			if (insn == null || insn.Opcode != Opcode.BranchCondition) return false;

			BranchCondition branch = (BranchCondition)insn;
			Block success = branch.Success, failure = branch.Failure;
			IValue test = branch.Test;

			if (AttemptMerge(success, failure))
			{
				branch.Remove();
				foreach (Phi phi in failure.PhiNodes)
				{
					IValue successValue = phi.Source[success];
					IValue failureValue = phi.Source[block];

					IValue value = block.AddLast(new ValueCondition(test, successValue, failureValue));
					phi.Source[block] = value;
				}

				block.AddLast(new Branch(failure));
				return true;
			}
			else if (AttemptMerge(failure, success))
			{
				branch.Remove();
				foreach (Phi phi in success.PhiNodes)
				{
					IValue successValue = phi.Source[block];
					IValue failureValue = phi.Source[failure];

					IValue value = block.AddLast(new ValueCondition(test, successValue, failureValue));
					phi.Source[block] = value;
				}

				block.AddLast(new Branch(success));
				return true;
			}
			else
			{
				return false;
			}
		}

		private static bool AttemptMerge(Block a, Block b)
		{
			// We're expecting something with 0 actual instructions
			// TODO: Handle phi nodes correctly
			if (a.Count != 1 || a.PhiNodes.Count != 0) return false;

			// We're expecting a jump into the other block
			List<Block> next = a.Next.ToList();
			if (next.Count != 1 || next[0] != b) return false;

			return true;
		}
	}
}

