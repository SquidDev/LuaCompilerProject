using System;
using System.Linq;
using LuaCP.IR;
using LuaCP.IR.Components;
using LuaCP.IR.Instructions;
using LuaCP.IR.User;
using LuaCP.Passes.Analysis;

namespace LuaCP.Passes.Optimisation
{
	/// <summary>
	/// Convert a reference ("open") upvalue to a value ("closed") one.
	/// </summary>
	public static class DemoteUpvalue
	{
		public static Pass<Module> Runner { get { return Run; } }

		private static bool Run(Module module)
		{
			UpvalueAnalysis analysis = new UpvalueAnalysis(module);
			bool changed = false;
			foreach (Function function in module.Functions)
			{
				if (function.OpenUpvalues.Count == 0) continue;
				foreach (Upvalue upvalue in function.OpenUpvalues.Where(analysis.NeverWritten).ToList())
				{
					changed = true;
					Upvalue replacement = new Upvalue(function, true);
					foreach (IUser<IValue> user in upvalue.Users.ToList<IUser<IValue>>())
					{
						Instruction insn = user as Instruction;
						if (insn == null) throw new Exception("Unknown user of upvalue " + user);

						switch (insn.Opcode)
						{
							case Opcode.ReferenceGet:
								{
									// We're getting a reference: Replace it with with the value
									ReferenceGet getter = (ReferenceGet)insn;
									getter.ReplaceWithAndRemove(replacement);
									break;
								}
							case Opcode.ClosureNew:
								{
									// Create a spoof reference. This will be removed by other passes
									ClosureNew closure = (ClosureNew)insn;
									ReferenceNew rNew = new ReferenceNew(replacement);
									closure.Block.AddBefore(closure, rNew);
									closure.Replace(upvalue, rNew);
									break;
								}
							default:
								throw new Exception("Unexpected operator " + insn.Opcode);
						}
					}
						
					int index = upvalue.Index;
					foreach (ClosureNew creator in function.Users.OfType<ClosureNew>().ToList())
					{
						IValue value = creator.OpenUpvalues[index];
						ReferenceGet getter = new ReferenceGet(value);
						creator.Block.AddAfter(creator, getter);

						creator.OpenUpvalues.RemoveAt(index);
						creator.ClosedUpvalues.Add(getter);
					}

					upvalue.Remove();
				}
			}

			return changed;
		}
	}
}

