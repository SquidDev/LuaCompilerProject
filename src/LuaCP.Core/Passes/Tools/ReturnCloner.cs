using System.Collections.Generic;
using LuaCP.IR;
using LuaCP.IR.Components;
using LuaCP.IR.Instructions;

namespace LuaCP.Passes.Tools
{
	public class ReturnCloner : InstructionCloner
	{
		private readonly Block exit;
		private readonly Phi phi;

		public  IValue Value { get { return phi; } }

		public ReturnCloner(
			Function function, 
			Function target, 
			Block exit,
			IList<IValue> args = null, 
			IList<IValue> openUpvalues = null,
			IList<IValue> closedUpvalues = null 
		)
			: base(function, target, args, openUpvalues, closedUpvalues)
		{
			this.exit = exit;
			this.phi = new Phi(exit);
		}

		protected override void CloneInstruction(Block toWrite, Instruction insn)
		{
			if (insn.Opcode == Opcode.Return)
			{
				Return returner = (Return)insn;
				phi.Source.Add(toWrite, GetValue(returner.Values));
				toWrite.AddLast(new Branch(exit));
			}
			else
			{
				base.CloneInstruction(toWrite, insn);
			}
			
		}
	}
}
