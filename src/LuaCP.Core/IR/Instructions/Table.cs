using System;
using System.Collections.Generic;
using System.Linq;

using LuaCP.IR.User;

namespace LuaCP.IR.Instructions
{
	public sealed class TableGet : ValueInstruction, IUser<IValue>
	{
		private IValue table;

		public IValue Table
		{
			get { return table; } 
			set { table = UserExtensions.Replace(this, table, value); }
		}

		private IValue key;

		public IValue Key
		{
			get { return key; } 
			set { key = UserExtensions.Replace(this, key, value); }
		}

		public TableGet(IValue table, IValue key)
			: base(Opcode.TableGet, ValueKind.Value)
		{
			Table = table;
			Key = key;
		}

		public IEnumerable<IValue> GetUses()
		{
			yield return Table;
			yield return Key;
		}

		public void Replace(IValue original, IValue replace)
		{
			if (table == original) Table = replace;
			if (key == original) Key = replace;
		}

		public override void ForceDestroy()
		{
			table.Users.Decrement(this);
			table = null;
			key.Users.Decrement(this);
			key = null;
		}
	}

	public sealed class TableSet : Instruction, IUser<IValue>
	{
		private IValue table;
		private IValue key;
		private IValue val;

		public IValue Table
		{
			get { return table; } 
			set { table = UserExtensions.Replace(this, table, value); }
		}

		public IValue Key
		{
			get { return key; } 
			set { key = UserExtensions.Replace(this, key, value); }
		}

		public IValue Value
		{
			get { return val; } 
			set { val = UserExtensions.Replace(this, val, value); }
		}

		public TableSet(IValue table, IValue key, IValue value)
			: base(Opcode.TableSet)
		{
			Table = table;
			Key = key;
			Value = value;
		}

		public IEnumerable<IValue> GetUses()
		{
			yield return table;
			yield return key;
			yield return val;
		}

		public void Replace(IValue original, IValue replace)
		{
			if (table == original) Table = replace;
			if (key == original) Key = replace;
			if (val == original) Value = replace;
		}

		public override void ForceDestroy()
		{
			table.Users.Decrement(this);
			table = null;
			key.Users.Decrement(this);
			key = null;
			val.Users.Decrement(this);
			val = null;
		}
	}

	public sealed class TableNew : ValueInstruction, IUser<IValue>
	{
		public readonly int AdditionalArray;
		public readonly int AdditionalHash;
		
		private readonly UsingList<IValue> arrayPart;
		public readonly UsingDictionary<IValue, IValue, TableNew> hashPart;

		public IList<IValue> ArrayPart { get { return arrayPart; } }

		public IDictionary<IValue, IValue> HashPart { get { return hashPart; } }

		public TableNew(int arraySize, int hashSize, IEnumerable<IValue> array, IDictionary<IValue, IValue> hash)
			: base(Opcode.TableNew, ValueKind.Value)
		{
			AdditionalArray = arraySize;
			AdditionalHash = hashSize;
			arrayPart = new UsingList<IValue>(this, array);
			hashPart = new UsingDictionary<IValue, IValue, TableNew>(this, hash);
		}

		public TableNew(IEnumerable<IValue> array, IDictionary<IValue, IValue> hash)
			: this(0, 0, array, hash)
		{
		}

		public IEnumerable<IValue> GetUses()
		{
			return arrayPart.Concat(hashPart.Keys).Concat(hashPart.Values).Distinct();
		}

		public void Replace(IValue original, IValue replace)
		{
			arrayPart.Replace(original, replace);
			hashPart.ReplaceKey(original, replace);
			hashPart.ReplaceValue(original, replace);
		}

		public override void ForceDestroy()
		{
			arrayPart.Clear();
			hashPart.Clear();
		}
	}
}
