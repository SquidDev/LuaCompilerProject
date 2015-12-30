using LuaCP.IR.Instructions;

namespace LuaCP.Optimisation
{
	/// <summary>
	/// Inlines access and creation of tuples
	/// </summary>
	public class TupleInliner
	{
		private static readonly TupleInliner instance = new TupleInliner();

		public static Pass<Instruction> Runner { get { return instance.Run; } }

		public bool Run(Instruction instruction)
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
								System.Console.WriteLine(tuple + " inlining with " + target + " at " + (index - target.Values.Count));

								TupleGet inlined = new TupleGet(target.Remaining, index - target.Values.Count);
								tuple.Block.AddAfter(tuple, inlined);
								tuple.ReplaceWithAndRemove(inlined);
							}
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

						return false;
					}
                    
			}
			return false;
		}
	}
}