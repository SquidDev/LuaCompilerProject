using LuaCP.IR.Components;
using LuaCP.IR.Instructions;
using LuaCP.IR;
using LuaCP.Passes.Tools;
using LuaCP.IR.User;
using System.Linq;

namespace LuaCP.Passes.Optimisation
{
	/// <summary>
	/// Takes a block with multiple predecessors and successors and
	/// finds edges where the branch is constant. It then duplicates the block,
	/// and redirects that edge to the new block.
	/// Constant folding does not occur here.
	/// </summary>
	public static class JumpThreading
	{
		public static Pass<Block> Runner { get { return Run; } }

		public static bool Run(PassManager manager, Block block)
		{
			if (block.PhiNodes.Count == 0) return false;
			if (block.Last == null || block.Last.Opcode != Opcode.BranchCondition) return false;

			BranchCondition condition = (BranchCondition)block.Last;
			IValue test = condition.Test;

			if (test is Phi)
			{
				Phi phi = (Phi)test;
				if (phi.Block != block) return false;

				bool changed = false;
				foreach (var pair in phi.Source.ToList())
				{
					if (pair.Value is Constant)
					{
						Clone(block, pair.Key);
						changed = true;
					}
				}
				return changed;
			}
			else if (test is ValueInstruction)
			{
				ValueInstruction insn = (ValueInstruction)test;
				if (insn.Block != block) return false;

				if (insn.Opcode.IsBinaryOperator())
				{
					bool changed = false;
					BinaryOp op = (BinaryOp)insn;
					foreach (Block previous in block.Previous.ToList())
					{
						if (IsConstant(op.Left, block, previous) && IsConstant(op.Right, block, previous))
						{
							Clone(block, previous);
							changed = true;
						}
					}

					return changed;
				}
				else if (insn.Opcode.IsUnaryOperator())
				{
					UnaryOp op = (UnaryOp)insn;
					bool changed = false;
					foreach (Block previous in block.Previous.ToList())
					{
						if (IsConstant(op.Operand, block, previous))
						{
							Clone(block, previous);
							changed = true;
						}
					}
						
					return changed;
				}
			}

			return false;
		}

		private static void Clone(Block block, Block source)
		{
			block.Function.Dominators.Invalidate();

			var cloner = new SingleBlockCloner();
			Block replacement = new Block(block.Function);
			foreach (var phi in block.PhiNodes)
			{
				cloner.SetValue(phi, phi.Source[source]);
				// phi.Source.Remove(source);
			}

			foreach (Instruction insn in block)
			{
				cloner.CloneInstruction(replacement, insn);
			}
				
			((IUser<Block>)source.Last).Replace(block, replacement);
		}

		private static bool IsConstant(IValue value, Block block, Block source)
		{
			var constant = value as Constant;
			if (constant != null) return true;

			Phi phi = value as Phi;
			if (phi != null && phi.Block == block)
			{
				value = phi.Source[source];
				constant = value as Constant;
				if (constant != null) return true;
			}

			return false;
		}

		private static bool IsNicePhi(this IValue value, Block block)
		{
			Phi phi = value as Phi;
			return phi != null && phi.Block == block;
		}
	}
}

