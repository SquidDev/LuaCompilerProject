using System.Collections.Generic;
using System.Linq;
using LuaCP.IR;
using LuaCP.IR.Instructions;
using LuaCP.Tree;

namespace LuaCP.Lua.Tree.Statement
{
	public class ReturnNode : Node
	{
		public readonly IReadOnlyList<IValueNode> Values;

		public ReturnNode(IEnumerable<IValueNode> values)
		{
			Values = values.ToList();
		}

		public override BlockBuilder Build(BlockBuilder builder)
		{
			IValue result;
			builder = Values.BuildAsTuple(builder, out result);
			using (var writer = new BlockWriter(builder, this))
			{
				writer.Add(new Return(result));
			}
			return builder;
		}
	}
}

