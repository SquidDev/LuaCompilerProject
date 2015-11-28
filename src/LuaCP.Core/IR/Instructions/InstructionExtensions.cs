using System.Collections.Generic;
using LuaCP.IR.Components;
using LuaCP.IR.User;

namespace LuaCP.IR.Instructions
{
	public static class InstructionExtensions
	{
		public static void ReplaceWithAndRemove(this ValueInstruction instruction, IValue value)
		{
			instruction.ReplaceWith(value);
			instruction.Remove();
		}

		public static void Remove(this Instruction instruction)
		{
			instruction.Destroy();
			instruction.Block.Remove(instruction);
		}

		public static IEnumerable<Block> NextBlocks(this Instruction instruction)
		{
			switch (instruction.Opcode)
			{
				case Opcode.Branch:
					{
						Branch branch = (Branch)instruction;
						yield return branch.Target;
						break;
					}
				case Opcode.BranchCondition:
					{
						BranchCondition branch = (BranchCondition)instruction;
						yield return branch.Success;
						yield return branch.Failure;
						break;
					}
			}
		}
	}
}

