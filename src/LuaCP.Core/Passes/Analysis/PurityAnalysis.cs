using LuaCP.IR.Instructions;

namespace LuaCP.Passes.Analysis
{
	public static class PurityAnalysis
	{
		/// <summary>
		/// Evaluates if an instruction is constant for a given set of inputs.
		/// This will also be side effect free.
		/// </summary>
		public static bool IsPure(this ValueInstruction insn)
		{
			// We presume that operations don't have a side effect.
			// If they do then something is breaking
			if (insn.Opcode.IsBinaryOperator() || insn.Opcode.IsUnaryOperator()) return true;
			switch (insn.Opcode)
			{
				case Opcode.TupleGet:
				case Opcode.TupleRemainder:
				case Opcode.TupleNew:
				case Opcode.ValueCondition:
					return true;
				default:
					return false;
			}
		}

		/// <summary>
		/// Ensures an instruction is side effect free
		/// </summary>
		/// <returns><c>true</c> if is side free the specified insn; otherwise, <c>false</c>.</returns>
		/// <param name="insn">Insn.</param>
		public static bool IsSideFree(this ValueInstruction insn)
		{
			// We presume that operations don't have a side effect.
			// If they do then something is breaking
			if (insn.Opcode.IsBinaryOperator() || insn.Opcode.IsUnaryOperator()) return true;
			switch (insn.Opcode)
			{
				case Opcode.TupleGet:
				case Opcode.TupleRemainder:
				case Opcode.TupleNew:
				case Opcode.ValueCondition:
				case Opcode.ClosureNew:
				case Opcode.ReferenceGet:
				case Opcode.ReferenceNew:
					return true;
				default:
					return false;
			}
		}
	}
}

