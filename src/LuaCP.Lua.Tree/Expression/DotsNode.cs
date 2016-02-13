using LuaCP.IR;
using LuaCP.IR.Components;
using LuaCP.Reporting;
using LuaCP.IR.Instructions;
using System.Linq;
using LuaCP.Tree;

namespace LuaCP.Lua.Tree.Expression
{
	public class DotsNode : ValueNode
	{
		public override BlockBuilder Build(BlockBuilder builder, out IValue result)
		{
			Function function = builder.Block.Function;
			result = function.Dots;
			if (function.Dots == null)
			{
				builder.Block.Function.Module.Reporter.Report(ReportLevel.Error, "This function is not varargs", Position);
				result = builder.Block.AddLast(new TupleNew(Enumerable.Empty<IValue>(), builder.Constants.Nil));
			}
			return builder;
		}
	}
}

