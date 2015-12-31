using LuaCP.IR;
using LuaCP.IR.Instructions;
using System.Collections.Generic;
using LuaCP.IR.Components;

namespace LuaCP.Tree.Expression
{
	public class ConditionNode : ValueNode
	{
		public enum ConditionKind
		{
			/// <summary>
			/// If Left is truthy then return right, else return left
			/// </summary>
			And,

			/// <summary>
			/// If Left is truthy then return left, else return right
			/// </summary>
			Or,
		}

		public readonly IValueNode Left;
		public readonly IValueNode Right;
		public readonly ConditionKind Kind;

		public ConditionNode(ConditionKind kind, IValueNode left, IValueNode right)
		{
			Kind = kind;
			Left = left;
			Right = right;
		}

		public override BlockBuilder Build(BlockBuilder leftBlock, out IValue result)
		{
			IValue left;
			leftBlock = Left.BuildAsValue(leftBlock, out left);

			BlockBuilder resultBlock = leftBlock.Continue();

			IValue right;
			BlockBuilder rightBlockStart = leftBlock.MakeChild();
			BlockBuilder rightBlock = Right.BuildAsValue(rightBlockStart, out right);
			rightBlock.Block.AddLast(new Branch(resultBlock.Block));

			Phi phi = new Phi(new Dictionary<Block, IValue>()
				{
					{ leftBlock.Block, left },
					{ rightBlock.Block, right },
				}, resultBlock.Block);
			result = phi;

			BranchCondition conditional = Kind == ConditionKind.And ?
                new BranchCondition(left, rightBlockStart.Block, resultBlock.Block) : 
                new BranchCondition(left, resultBlock.Block, rightBlockStart.Block);

			using (BlockWriter writer = new BlockWriter(leftBlock, this))
			{
				writer.Add(conditional);
			}

			return resultBlock;
		}
	}

}

