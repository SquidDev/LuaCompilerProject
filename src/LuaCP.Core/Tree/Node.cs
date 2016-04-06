using System;
using LuaCP.IR;
using LuaCP.IR.Instructions;
using LuaCP.Reporting;
using System.Collections.Generic;

namespace LuaCP.Tree
{
	public abstract class Node : INode
	{
		public abstract BlockBuilder Build(BlockBuilder builder);

		public Range Position { get; set; }
	}

	public abstract class ValueNode : IValueNode
	{
		BlockBuilder INode.Build(BlockBuilder builder)
		{
			IValue result;
			return Build(builder, out result);
		}

		public Range Position { get; set; }

		public abstract BlockBuilder Build(BlockBuilder builder, out IValue result);
	}

	public static class NodeUtilities
	{
		public static BlockBuilder BuildAsValue(this IValueNode node, BlockBuilder builder, out IValue result)
		{
			IValue value;
			builder = node.Build(builder, out value);
			switch (value.Kind)
			{
				case ValueKind.Value:
					result = value;
					break;
				case ValueKind.Tuple:
					result = builder.Block.AddLast(new TupleGet(value, 0));
					break;
				case ValueKind.Reference:
					result = builder.Block.AddLast(new ReferenceGet(value));
					break;
				default:
					throw new ArgumentException("Unknown type " + value.Kind);
			}
			return builder;
		}

		public static BlockBuilder BuildAsTuple(this IValueNode node, BlockBuilder builder, out IValue result)
		{
			IValue value;
			builder = node.Build(builder, out value);
			switch (value.Kind)
			{
				case ValueKind.Value:
				case ValueKind.Tuple:
					result = value;
					break;
				case ValueKind.Reference:
					result = builder.Block.AddLast(new ReferenceGet(value));
					break;
				default:
					throw new ArgumentException("Unknown type " + value.Kind);
			}
			return builder;
		}

		public static BlockBuilder BuildAsTuple(this IReadOnlyList<IValueNode> nodes, BlockBuilder builder, out IValue result)
		{
			var values = new List<IValue>(nodes.Count);
			IValue remainder = builder.Constants.Nil;

			int index = 0, length = nodes.Count - 1;
			foreach (IValueNode node in nodes)
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
					if (remainder.Kind != ValueKind.Tuple)
					{
						values.Add(remainder);
						remainder = builder.Constants.Nil;
					}
				}

				index++;
			}
				
			result = builder.Block.AddLast(new TupleNew(values, remainder));
			return builder;
		}
	}
}

