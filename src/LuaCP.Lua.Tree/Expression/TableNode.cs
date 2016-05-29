using System.Collections.Generic;
using System.Linq;
using LuaCP.IR;
using LuaCP.IR.Instructions;
using LuaCP.Tree;

namespace LuaCP.Lua.Tree.Expression
{
	public class TableNode : ValueNode
	{
		public sealed class TableItem
		{
			public readonly IValueNode Key;
			public readonly IValueNode Value;

			public TableItem(IValueNode key, IValueNode value)
			{
				Key = key;
				Value = value;
			}

			public TableItem(IValueNode value)
			{
				Key = null;
				Value = value;
			}
		}

		public readonly IReadOnlyList<TableItem> Items;

		public TableNode(IEnumerable<TableItem> items)
		{
			Items = items.ToList();
		}

		public override BlockBuilder Build(BlockBuilder builder, out IValue result)
		{
			List<IValue> array = new List<IValue>();
			Dictionary<IValue, IValue> hash = new Dictionary<IValue, IValue>();

			foreach (TableItem item in Items)
			{
				if (item.Key == null)
				{
					IValue value;
					builder = item.Value.BuildAsTuple(builder, out value);   

					array.Add(value);
				}
				else
				{
					IValue key, value;
					builder = item.Key.BuildAsValue(builder, out key);
					builder = item.Value.BuildAsValue(builder, out value);

					hash.Add(key, value);
				}
			}

			using (BlockWriter writer = new BlockWriter(builder, this))
			{
				result = writer.Add(new TableNew(array.GetAsTuple(builder), hash));
			}

			return builder;
		}
	}
}

