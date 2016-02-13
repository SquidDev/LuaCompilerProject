using LuaCP.IR;
using LuaCP.IR.Instructions;
using LuaCP.Tree;

namespace LuaCP.Lua.Tree.Expression
{
	public class IndexNode : ValueNode, IAssignable
	{
		public readonly IValueNode Table;
		public readonly IValueNode Key;

		public IndexNode(IValueNode table, IValueNode key)
		{
			Table = table;
			Key = key;
		}

		public override BlockBuilder Build(BlockBuilder builder, out IValue result)
		{
			IValue table, key;
			builder = Table.BuildAsValue(builder, out table);
			builder = Key.BuildAsValue(builder, out key);

			using (BlockWriter writer = new BlockWriter(builder, this))
			{
				result = writer.Add(new TableGet(table, key));
			}
			return builder;
		}

		public BlockBuilder Assign(BlockBuilder setupBuilder, out Assigner assigner)
		{
			IValue table, key;
			setupBuilder = Table.BuildAsValue(setupBuilder, out table);
			setupBuilder = Key.BuildAsValue(setupBuilder, out key);
            
			assigner = (builder, value) =>
			{
				using (BlockWriter writer = new BlockWriter(builder, this))
				{
					writer.Add(new TableSet(table, key, value));
				}
				return builder;
			};

			return setupBuilder;
		}
	}
}

