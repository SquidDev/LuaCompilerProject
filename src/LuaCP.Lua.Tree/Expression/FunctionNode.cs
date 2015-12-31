using System.Collections.Generic;
using System.Linq;

using LuaCP.IR;
using LuaCP.IR.Instructions;

namespace LuaCP.Tree.Expression
{
	public class FunctionNode : ValueNode
	{
		public readonly IReadOnlyList<string> Arguments;
		public readonly bool Dots;
		public readonly INode Body;

		public FunctionNode(IEnumerable<string> arguments, bool dots, INode body)
		{
			Arguments = arguments.ToList();
			Dots = dots;
			Body = body;
		}

		public override BlockBuilder Build(BlockBuilder builder, out IValue result)
		{
			FunctionBuilder function = new FunctionBuilder(builder, Arguments, Dots);
			function.Accept(Body);
        	
			List<IValue> values = new List<IValue>(function.Upvalues.Count);
			foreach (IValue upvalue in function.Upvalues)
			{
				values.Add(upvalue);
			}
        	
			using (BlockWriter writer = new BlockWriter(builder, this))
			{
				result = writer.Add(new ClosureNew(function.Function, values, Enumerable.Empty<IValue>()));
			}
			return builder;
		}
	}
}

