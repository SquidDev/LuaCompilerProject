using System.Collections.Generic;
using System.Linq;
using LuaCP.IR;
using LuaCP.IR.Instructions;

namespace LuaCP.Tree.Statement
{
	public class AssignNode : Node
	{
		public readonly IReadOnlyList<IAssignable> Assignables;
		public readonly IReadOnlyList<IValueNode> Values;

		public AssignNode(IEnumerable<IAssignable> assignables, IEnumerable<IValueNode> values)
		{
			Assignables = assignables.ToList();
			Values = values.ToList();
		}

		public override BlockBuilder Build(BlockBuilder builder)
		{
			Assigner[] assignables = new Assigner[Assignables.Count];
			for (int i = 0; i < Assignables.Count; i++)
			{
				builder = Assignables[i].Assign(builder, out assignables[i]);
			}
            
			IValue[] values = new IValue[Assignables.Count];
			IValue last = null;
			for (int i = 0; i < Assignables.Count; i++)
			{
				if (i == Values.Count - 1)
				{
					IValue current;
					builder = Values[i].Build(builder, out current);
					if (current.Kind == ValueKind.Tuple)
					{
						last = current;
						values[i] = builder.Block.AddLast(new TupleGet(current, 0));
					}
					else
					{
						values[i] = current;
					}
				}
				else if (i < Values.Count)
				{
					builder = Values[i].BuildAsValue(builder, out values[i]);
				}
				else if (last == null)
				{
					values[i] = builder.Constants.Nil;
				}
				else
				{
					values[i] = builder.Block.AddLast(new TupleGet(last, i - Values.Count + 1));
				}
			}            
            
			for (int i = Assignables.Count; i < Values.Count; i++)
			{
				builder = Values[i].Build(builder);
			}

			for (int i = 0; i < Assignables.Count; i++)
			{
				builder = assignables[i](builder, values[i]);
			}

			return builder;
		}
	}
}

