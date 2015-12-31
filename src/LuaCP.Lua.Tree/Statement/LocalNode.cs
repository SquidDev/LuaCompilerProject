using System.Collections.Generic;
using System.Linq;
using LuaCP.IR;
using LuaCP.IR.Instructions;

namespace LuaCP.Tree.Statement
{
	public class LocalNode : Node
	{
		public readonly IReadOnlyList<IDeclarable> Declared;
		public readonly IReadOnlyList<IValueNode> Values;

		public LocalNode(IEnumerable<IDeclarable> declared, IEnumerable<IValueNode> values)
		{
			Declared = declared.ToList();
			Values = values.ToList();
		}

		public override BlockBuilder Build(BlockBuilder builder)
		{
			IValue[] values = new IValue[Declared.Count];
			IValue last = null;
			for (int i = 0; i < Declared.Count; i++)
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
            
			for (int i = Declared.Count; i < Values.Count; i++)
			{
				builder = Values[i].Build(builder);
			}

			for (int i = 0; i < Declared.Count; i++)
			{
				builder = Declared[i].Declare(builder, values[i]);
			}

			return builder.MakeScope();
		}
	}
}

