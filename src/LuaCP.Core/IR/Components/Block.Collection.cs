using System;
using System.Collections.Generic;
using LuaCP.IR.Instructions;
using LuaCP.Graph;
using LuaCP.IR.User;

namespace LuaCP.IR.Components
{
	public partial class Block : ICollection<Instruction>, IEnumerable<Instruction>, IUsable<Block>, IGraphNode<Block>
	{
		private readonly LinkedList<Instruction> instructions = new LinkedList<Instruction>();

		public Instruction First { get { return instructions.First == null ? null : instructions.First.Value; } }

		public Instruction Last { get { return instructions.Last == null ? null : instructions.Last.Value; } }

		public void AddAfter(Instruction node, Instruction value)
		{
			if (value == null) throw new ArgumentNullException("value");
			if (value.Block != null) throw new ArgumentException("Already in block", "value");
			if (node.Opcode.IsTerminator()) throw new InvalidOperationException("Cannot add after node as it is a terminator");

			value.node = instructions.AddAfter(node.node, value);
			value.Block = this;
		}

		public void AddBefore(Instruction node, Instruction value)
		{
			if (value == null) throw new ArgumentNullException("value");
			if (value.Block != null) throw new ArgumentException("Already in block", "value");
            
			value.node = instructions.AddBefore(node.node, value);
			value.Block = this;
		}

		public void AddFirst(Instruction value)
		{
			if (value == null) throw new ArgumentNullException("value");
			if (value.Block != null) throw new ArgumentException("Already in block", "value");
            
			value.node = instructions.AddFirst(value);
			value.Block = this;
		}

		public T AddLast<T>(T value)
            where T : Instruction
		{
			if (value == null) throw new ArgumentNullException("value");
			if (value.Block != null) throw new ArgumentException("Already in block", "value");
			Instruction last = Last;
			if (last != null && last.Opcode.IsTerminator()) throw new InvalidOperationException("Cannot add to block, already has terminator");

			value.node = instructions.AddLast(value);
			value.Block = this;
			return value;
		}

		public void Remove(Instruction value)
		{
			instructions.Remove(value.node);
			value.node = null;
			value.Block = null;
		}

		public int Count { get { return instructions.Count; } }

		public bool IsReadOnly { get { return false; } }

		void ICollection<Instruction>.Add(Instruction item)
		{
			AddLast(item);
		}

		public void Clear()
		{
			Instruction next = this.First;
			while (next != null)
			{
				Instruction node = next;
				next = node.Next;
				Remove(next);
			}
		}

		public bool Contains(Instruction item)
		{
			return item.Block == this;
		}

		public void CopyTo(Instruction[] array, int arrayIndex)
		{
			instructions.CopyTo(array, arrayIndex);
		}

		bool ICollection<Instruction>.Remove(Instruction item)
		{
			if (item.Block == this)
			{
				Remove(item);
				return true;
			}

			return false;
		}

		public IEnumerator<Instruction> GetEnumerator()
		{
			// Allows removing the *current* instruction
			Instruction next = this.First;
			while (next != null)
			{
				Instruction current = next;
				next = current.Next;
				yield return current;
			}
		}

		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}
	}
}

