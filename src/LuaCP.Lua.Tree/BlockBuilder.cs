using System;
using LuaCP.IR.Instructions;
using LuaCP.IR.Components;
using LuaCP.Reporting;

namespace LuaCP.Tree
{
	public class BlockBuilder
	{
		public readonly VariableScope Variables;
		public readonly LabelScope Labels;

		public ConstantPool Constants { get { return Block.Function.Module.Constants; } }

		public readonly BlockBuilder Parent;
		public readonly Block Block;
		public readonly LoopState LoopState;

		public BlockBuilder(Function function)
		{
			Variables = new VariableScope();
			Block = function.EntryPoint;
			Labels = new LabelScope(function);
		}

		internal BlockBuilder(Block block, BlockBuilder parent, VariableScope variables, LabelScope labels, LoopState state)
		{
			Parent = parent;
			Block = block;
			LoopState = state;
			Variables = variables;
			Labels = labels;
		}

		/// <summary>
		/// Continue this scope in a new block
		/// </summary>
		/// <returns>A new builder for the scope</returns>
		public BlockBuilder Continue()
		{
			return new BlockBuilder(
				new Block(Block.Function), 
				this,
				Variables,
				Labels,
				LoopState
			);
		}

		public BlockBuilder MakeLoop(LoopState state)
		{
			return new BlockBuilder(
				new Block(Block.Function), 
				this, 
				new VariableScope(Variables),
				new LabelScope(Labels),
				state
			);
		}

		public BlockBuilder MakeScope()
		{
			return new BlockBuilder(
				this.Block, 
				this, 
				new VariableScope(Variables), 
				new LabelScope(Labels),
				LoopState
			);
		}

		public BlockBuilder MakeChild()
		{
			return new BlockBuilder(
				new Block(Block.Function),
				this, 
				new VariableScope(Variables), 
				new LabelScope(Labels),
				LoopState
			);
		}
	}

	public sealed class LoopState
	{
		/// <summary>
		/// Where the test occurs
		/// (to skip to for a "continue" statement)
		/// </summary>
		public readonly BlockBuilder Test;

		/// <summary>
		/// The exit point of the current loop
		/// (to skip to for a "break" statement)
		/// </summary>
		public readonly BlockBuilder End;

		public LoopState(BlockBuilder test, BlockBuilder end)
		{
			Test = test;
			End = end;
		}
	}

	public class BlockWriter : IDisposable
	{
		private readonly Range range;
		private readonly Block block;

		public BlockWriter(Block block, Range range)
		{
			this.block = block;
			this.range = range;
		}

		public BlockWriter(BlockBuilder builder, INode node)
			: this(builder.Block, node.Position)
		{
		}

		public void Dispose()
		{
		}

		public T Add<T>(T value)
            where T : Instruction
		{
			if (value.Position == null) value.Position = range;
			return block.AddLast(value);
		}
	}
}
