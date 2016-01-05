using System.Collections.Generic;
using System.Linq;
using LuaCP.Graph;
using LuaCP.IR.Components;
using LuaCP.IR.Instructions;

namespace LuaCP.Passes
{
	/// <summary>
	/// Removes blocks that are unreachable
	/// </summary>
	public static class UnreachableCode
	{
		public static Pass<Function> ForFunction { get { return RunFunction; } }

		public static Pass<Module> ForModule { get { return RunModule; } }

		public static bool RunFunction(PassManager data, Function function)
		{
			HashSet<Block> reachable = function.EntryPoint.ReachableEager();
			List<Block> unreachable = new List<Block>();
			foreach (Block block in function.Blocks)
			{
				if (!reachable.Contains(block)) unreachable.Add(block);
			}

			if (unreachable.Count > 0)
			{
				function.Dominators.Evaluate();
				foreach (Block block in unreachable)
				{
					DestroyBlock(block);
					block.Function.Blocks.Remove(block);
				}

				return true;
			}

			return false;
		}

		public static bool RunModule(PassManager data, Module module)
		{
			List<Function> functions = module.Functions.Where(x => x.Users.UniqueCount == 0 && x != module.EntryPoint).ToList();
			foreach (Function function in functions)
			{
				foreach (Block block in function.Blocks)
				{
					DestroyBlock(block);
				}

				function.Blocks.Clear();
				module.Functions.Remove(function);
			}

			return functions.Count > 0;
		}

		private static void DestroyBlock(Block block)
		{
			foreach (Phi phi in block.PhiNodes) phi.Source.Clear();
			foreach (Block next in block.Next)
			{
				foreach (Phi phi in next.PhiNodes)
				{
					phi.Source.Remove(block);
				}
			}
			foreach (Instruction insn in block) insn.ForceDestroy();

			block.Function.Dominators.Invalidate();
		}
	}
}

