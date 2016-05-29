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

		public static IValue GetAsValue(this IValue value, BlockBuilder builder)
		{
			switch (value.Kind)
			{
				case ValueKind.Value:
					return value;
				case ValueKind.Tuple:
					return builder.Block.AddLast(new TupleGet(value, 0));
				case ValueKind.Reference:
					return builder.Block.AddLast(new ReferenceGet(value));
				default:
					throw new ArgumentException("Unknown type " + value.Kind);
			}
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

		public static IValue GetAsTuple(this IValue value, BlockBuilder builder)
		{
			switch (value.Kind)
			{
				case ValueKind.Value:
				case ValueKind.Tuple:
					return value;
				case ValueKind.Reference:
					return builder.Block.AddLast(new ReferenceGet(value));
				default:
					throw new ArgumentException("Unknown type " + value.Kind);
			}
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

			if (values.Count == 0)
			{
				result = remainder;
			}
			else if (remainder.IsNil() && values.Count == 1)
			{
				result = values[0];
			}
			else
			{
				result =  builder.Block.AddLast(new TupleNew(values, remainder));
			}

			return builder;
		}

		public static IValue GetAsTuple(this IReadOnlyList<IValue> values, BlockBuilder builder)
		{
			var items = new List<IValue>(values.Count);
			IValue remainder = builder.Constants.Nil;

			int index = 0, length = values.Count - 1;
			foreach (IValue value in values)
			{
				if (index < length)
				{
					items.Add(value.GetAsValue(builder));
				}
				else
				{
					remainder = value.GetAsTuple(builder);
					if (remainder.Kind != ValueKind.Tuple)
					{
						items.Add(remainder);
						remainder = builder.Constants.Nil;
					}
				}

				index++;
			}

			if (items.Count == 0)
			{
				return remainder;
			}
			else if (remainder.IsNil() && items.Count == 1)
			{
				return items[0];
			}
			else
			{
				return  builder.Block.AddLast(new TupleNew(items, remainder));
			}
		}
	}
}

