using LuaCP.IR;
using LuaCP.IR.Instructions;

namespace LuaCP.Tree.Expression
{
	public class BinOpNode : ValueNode
	{
		public readonly Opcode Opcode;
		public readonly IValueNode Left;
		public readonly IValueNode Right;

		public BinOpNode(Opcode opcode, IValueNode left, IValueNode right)
		{
			Opcode = opcode;
			Left = left;
			Right = right;
		}

		public override BlockBuilder Build(BlockBuilder builder, out IValue result)
		{
			IValue left, right;
			builder = Left.BuildAsValue(builder, out left);
			builder = Right.BuildAsValue(builder, out right);

			using (BlockWriter writer = new BlockWriter(builder, this))
			{
				result = writer.Add(new BinaryOp(Opcode, left, right));
			}
			return builder;
		}
	}

	public class UnaryOpNode : ValueNode
	{
		public readonly Opcode Opcode;
		public readonly IValueNode Operand;

		public UnaryOpNode(Opcode opcode, IValueNode operand)
		{
			Opcode = opcode;
			Operand = operand;
		}

		public override BlockBuilder Build(BlockBuilder builder, out IValue result)
		{
			IValue operand;
			builder = Operand.BuildAsValue(builder, out operand);

			using (BlockWriter writer = new BlockWriter(builder, this))
			{
				result = writer.Add(new UnaryOp(Opcode, operand));
			}
			return builder;
		}
	}
}

