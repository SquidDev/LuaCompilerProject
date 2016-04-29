using System;
using LuaCP.IR.Instructions;
using LuaCP.IR.Components;
using LuaCP.Reporting;

namespace LuaCP.Tree
{
	public class BlockBuilder
	{
		public ConstantPool Constants { get { return Block.Function.Module.Constants; } }

		public readonly ScopeDictionary Scopes;
		public readonly BlockBuilder Parent;
		public readonly Block Block;
		public readonly LoopState LoopState;

		public BlockBuilder(Function function)
		{
			Scopes = new ScopeDictionary(this);
			Block = function.EntryPoint;
		}

		public BlockBuilder(Block block, BlockBuilder parent, ScopeDictionary scope, LoopState state)
		{
			Parent = parent;
			Block = block;
			LoopState = state;
			Scopes = scope;
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
				Scopes,
				LoopState
			);
		}

		public BlockBuilder MakeLoop(LoopState state)
		{
			return new BlockBuilder(
				new Block(Block.Function), 
				this, 
				Scopes.CreateChild(),
				state
			);
		}

		public BlockBuilder MakeScope()
		{
			return new BlockBuilder(
				this.Block, 
				this, 
				Scopes.CreateChild(),
				LoopState
			);
		}

		public BlockBuilder MakeChild()
		{
			return new BlockBuilder(
				new Block(Block.Function),
				this, 
				Scopes.CreateChild(),
				LoopState
			);
		}

		public T Get<T>()  where T : IScope
		{
			return Scopes.Get<T>();
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
