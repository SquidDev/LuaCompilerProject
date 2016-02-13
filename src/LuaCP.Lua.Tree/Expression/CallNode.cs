using LuaCP.IR;
using System.Collections.Generic;
using System.Linq;
using LuaCP.IR.Instructions;
using LuaCP.Tree;

namespace LuaCP.Lua.Tree.Expression
{
	public class CallNode : ValueNode
	{
		public readonly IValueNode Function;
		public readonly IReadOnlyList<IValueNode> Arguments;

		public CallNode(IValueNode function, IEnumerable<IValueNode> args)
		{
			Function = function;
			Arguments = args.ToList();
		}

		public override BlockBuilder Build(BlockBuilder builder, out IValue result)
		{
			IValue function;
			builder = Function.BuildAsValue(builder, out function);

			List<IValue> args = new List<IValue>();
			IValue remainder = builder.Constants.Nil;

			int index = 0, length = Arguments.Count - 1;
			foreach (IValueNode node in Arguments)
			{
				if (index < length)
				{
					IValue arg;
					builder = node.BuildAsValue(builder, out arg);
					args.Add(arg);
				}
				else
				{
					IValue arg;
					builder = node.BuildAsTuple(builder, out arg);
					if (arg.Kind == ValueKind.Tuple)
					{
						remainder = arg;
					}
					else
					{
						args.Add(arg);
					}
				}

				index++;
			}

			using (BlockWriter writer = new BlockWriter(builder, this))
			{    
				TupleNew tuple = writer.Add(new TupleNew(args, remainder));
				result = writer.Add(new Call(function, tuple));
			}

			return builder;
		}
	}
}

