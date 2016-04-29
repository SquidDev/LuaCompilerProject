using LuaCP.IR;

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
}

