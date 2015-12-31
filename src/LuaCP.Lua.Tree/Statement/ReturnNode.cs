using System.Collections.Generic;
using System.Linq;
using LuaCP.IR;
using LuaCP.IR.Instructions;

namespace LuaCP.Tree.Statement
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
			List<IValue> values = new List<IValue>(Values.Count);
			IValue remainder = builder.Constants.Nil;

			int index = 0, length = Values.Count - 1;
			foreach (IValueNode node in Values)
			{
				if (index < length)
				{
					IValue arg;
					builder = node.BuildAsValue(builder, out arg);
					values.Add(arg);
				}
				else
				{
					builder = node.BuildAsTuple(builder, out remainder);
				}

				index++;
			}

			using (BlockWriter writer = new BlockWriter(builder, this))
			{
				TupleNew tuple = writer.Add(new TupleNew(values, remainder));
				writer.Add(new Return(tuple));
			}
			return builder;
		}
	}
}

