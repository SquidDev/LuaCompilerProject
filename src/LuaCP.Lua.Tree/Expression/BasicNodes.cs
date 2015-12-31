using LuaCP.IR;
using LuaCP.IR.Components;
using LuaCP.Reporting;
using LuaCP.IR.Instructions;
using System.Linq;

namespace LuaCP.Tree.Expression
{
	public class ConstantNode : ValueNode
	{
		public static readonly ConstantNode Nil = new ConstantNode(Literal.Nil);
		public readonly Literal Value;

		public ConstantNode(Literal value)
		{
			Value = value;
		}

		public override BlockBuilder Build(BlockBuilder builder, out IValue result)
		{
			result = builder.Constants[Value];
			return builder;
		}
	}

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

