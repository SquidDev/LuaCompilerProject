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

			IValue args;
			builder = Arguments.BuildAsTuple(builder, out args);

			using (BlockWriter writer = new BlockWriter(builder, this))
			{    
				result = writer.Add(new Call(function, args));
			}

			return builder;
		}
	}
}

