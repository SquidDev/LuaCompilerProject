using System.Collections.Generic;
using System.Linq;
using LuaCP.Collections;
using LuaCP.Graph;
using LuaCP.IR.Components;
using LuaCP.IR.Instructions;
using LuaCP.Passes.Analysis;

namespace LuaCP.Passes.Optimisation
{
	public static class CommonSubexpressionElimination
	{
		public static Pass<Function> Runner { get { return Run; } }

		public static bool Run(PassManager data, Function function)
		{
			var definitions = new Dictionary<Block, Dictionary<ValueInstruction, ValueInstruction>>();
			bool changed = false;
			foreach (Block block in function.EntryPoint.VisitPreorder())
			{
				foreach (ValueInstruction instruction in block.OfType<ValueInstruction>())
				{
					if (instruction.IsPure())
					{
						Block dom = block;
						bool replaced = false;
						while (dom != null)
						{
							Dictionary<ValueInstruction, ValueInstruction> parent;
							ValueInstruction original;
							if (definitions.TryGetValue(dom, out parent) && parent.TryGetValue(instruction, out original))
							{
								instruction.ReplaceWithAndRemove(original);
								replaced = true;
								changed = true;
								break;
							}

							dom = dom.ImmediateDominator;
						}

						if (!replaced)
						{
							definitions
								.GetOrAddDefault(block, () => new Dictionary<ValueInstruction, ValueInstruction>(InstructionComparer.Instance))
								.Add(instruction, instruction);
						}
					}
				}
			}

			return changed;
		}
	}
}

