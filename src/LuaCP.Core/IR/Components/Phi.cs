using System.Collections.Generic;
using LuaCP.IR.User;

namespace LuaCP.IR.Components
{
	/// <summary>
	/// A merge point of two variables depending on where this block was branched from
	/// </summary>
	public sealed class Phi : IValue, IUser<IValue>, IUser<Block>
	{
		private readonly UsingDictionary<Block, IValue, Phi> source;
		private readonly CountingSet<IUser<IValue>> users = new CountingSet<IUser<IValue>>();
		private readonly Block block;

		public Phi(IDictionary<Block, IValue> source, Block block)
		{
			this.source = new UsingDictionary<Block, IValue, Phi>(this, source);
			this.block = block;
			block.PhiNodes.Add(this);
		}

		public Phi(Block block)
		{
			this.source = new UsingDictionary<Block, IValue, Phi>(this);
			this.block = block;
			block.PhiNodes.Add(this);
		}

		public IEnumerable<IValue> GetUses()
		{
			return source.Values;
		}

		public void Replace(IValue original, IValue replace)
		{
			source.ReplaceValue(original, replace);
		}

		IEnumerable<Block> IUser<Block>.GetUses()
		{
			return source.Keys;
		}

		public void Replace(Block original, Block replace)
		{
			source.ReplaceKey(original, replace);
		}

		public ValueKind Kind { get { return ValueKind.Value; } }

		public CountingSet<IUser<IValue>> Users { get { return users; } }

		public IDictionary<Block, IValue> Source { get { return source; } }

		public Block Block { get { return block; } }
	}
}
