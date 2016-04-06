using LuaCP.IR.Instructions;
using LuaCP.IR;
using LuaCP.IR.Components;

namespace LuaCP.Passes.Optimisation
{
	/// <summary>
	/// Inlines access and creation of tuples
	/// </summary>
	public static class TupleInliner
	{
		public static Pass<Instruction> Runner { get { return Run; } }

		public static bool Run(PassManager data, Instruction instruction)
		{
			switch (instruction.Opcode)
			{
				case Opcode.TupleGet:
					{
						TupleGet tuple = (TupleGet)instruction;
						if (tuple.Tuple is TupleNew)
						{
							TupleNew target = (TupleNew)tuple.Tuple;
							int index = tuple.Offset;
							if (index < target.Values.Count)
							{
								tuple.ReplaceWithAndRemove(target.Values[index]);
							}
							else
							{
								TupleGet inlined = new TupleGet(target.Remaining, index - target.Values.Count);
								tuple.Block.AddAfter(tuple, inlined);
								tuple.ReplaceWithAndRemove(inlined);
							}
							return true;
						}
						else if (tuple.Tuple is Phi)
						{
							Phi phi = (Phi)tuple.Tuple;
							Phi newPhi = new Phi(phi.Block);

							foreach (var pair in phi.Source)
							{
								var getter = new TupleGet(pair.Value, tuple.Offset);
								pair.Key.AddBefore(pair.Key.Last, getter);
								newPhi.Source.Add(pair.Key, getter);
							}

							tuple.ReplaceWithAndRemove(newPhi);

							return true;
						}
						return false;
					}
				case Opcode.TupleNew:
					{
						TupleNew tuple = (TupleNew)instruction;
						if (tuple.Values.Count == 0)
						{
							tuple.ReplaceWithAndRemove(tuple.Remaining);
							return true;
						}
						else if (tuple.Remaining is TupleNew)
						{
							TupleNew remainder = (TupleNew)tuple.Remaining;
							foreach (IValue value in remainder.Values)
							{
								tuple.Values.Add(value);
							}
							tuple.Remaining = remainder.Remaining;
							return true;
						}
						// TODO: TupleRemainder

						return false;
					}
                    
			}
			return false;
		}
	}
}
