using System;
using System.Collections.Generic;
using LuaCP.IR.Components;
using LuaCP.IR.User;
using LuaCP.Reporting;
using LuaCP.Collections;

namespace LuaCP.IR.Instructions
{
	public abstract class Instruction : IBelongs<Block>
	{
		private readonly Opcode opcode;

		public Opcode Opcode { get { return opcode; } }

		internal LinkedListNode<Instruction> node;

		public Instruction Previous { get { return node != null && node.Previous != null ? node.Previous.Value : null; } }

		public Instruction Next { get { return node != null && node.Next != null ? node.Next.Value : null; } }

		public Block Block { get; internal set; }

		public Range Position { get; set; }

		public Block Owner { get { return Block; } }

		public Instruction(Opcode opcode)
		{
			this.opcode = opcode;
		}

		public virtual void Destroy()
		{
			ForceDestroy();
		}

		public abstract void ForceDestroy();
	}

	public abstract class ValueInstruction : Instruction, IValue
	{
		private readonly CountingSet<IUser<IValue>> users = new CountingSet<IUser<IValue>>();

		public CountingSet<IUser<IValue>> Users { get { return users; } }

		private readonly ValueKind kind;

		public ValueInstruction(Opcode opcode, ValueKind kind)
			: base(opcode)
		{
			this.kind = kind;
		}

		public ValueKind Kind { get { return kind; } }

		public override void Destroy()
		{
			if (users.UniqueCount > 0) throw new InvalidOperationException("This instruction is still being used");
			base.Destroy();
		}
	}
}
