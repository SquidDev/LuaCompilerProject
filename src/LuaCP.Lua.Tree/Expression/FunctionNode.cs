using System.Collections.Generic;
using System.Linq;

using LuaCP.IR;
using LuaCP.IR.Instructions;
using LuaCP.Lua.Tree;
using LuaCP.Tree;

namespace LuaCP.Lua.Tree.Expression
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
        	
			IVariableScope scope = function.EntryPoint.Scopes.Get<IVariableScope>();
			FunctionVariableScope fScope = null;
			while (scope != null && fScope == null)
			{
				fScope = scope as FunctionVariableScope;
				scope = scope.Parent;
			}

			List<IValue> values = fScope == null ? new List<IValue>() : fScope.Upvalues;
			using (BlockWriter writer = new BlockWriter(builder, this))
			{
				result = writer.Add(new ClosureNew(function.Function, values, Enumerable.Empty<IValue>()));
			}
			return builder;
		}
	}
}

