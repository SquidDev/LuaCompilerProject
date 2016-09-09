using System;
using System.Collections.Generic;
using LuaCP.IR.Components;
using LuaCP.IR.User;
using LuaCP.IR;
using LuaCP.IR.Instructions;
using System.Linq;
using LuaCP.Collections;

namespace LuaCP.Passes.Analysis
{
	public class UpvalueAnalysis
	{
		private readonly Dictionary<Upvalue, bool> writtenClosure = new Dictionary<Upvalue, bool>();
		private readonly Dictionary<Upvalue, bool> writtenParent = new Dictionary<Upvalue, bool>();
		private readonly MultiMap<Upvalue, IValue> sources = new MultiMap<Upvalue, IValue>();

		public UpvalueAnalysis(Module module)
		{
			foreach (Function function in module.Functions)
			{
				if (function.OpenUpvalues.Count == 0) continue;

				foreach (Upvalue upvalue in function.OpenUpvalues)
				{
					EverWrittenParent(upvalue);
					EverWritenClosure(upvalue);
					GatherSources(upvalue);
				}
			}
		}

		private bool WrittenClosure(Upvalue upvalue)
		{
			foreach (IUser<IValue> user in (IEnumerable<IUser<IValue>>)upvalue.Users)
			{
				if (user is ReferenceSet) return true;

				ClosureNew closure = user as ClosureNew;
				if (closure == null) continue;

				int index = closure.OpenUpvalues.IndexOf(upvalue);
				if (index == -1) throw new Exception("Upvalue is not on open upvalue list");

				if (EverWritenClosure(closure.Function.OpenUpvalues[index])) return true;
			}

			return false;
		}

		/// <summary>
		/// Checks if an upvalue is ever written in a child method.
		/// </summary>
		/// <returns>If an upvalue is written</returns>
		/// <param name="upvalue">The upvalue to check</param>
		public bool EverWritenClosure(Upvalue upvalue)
		{
			if (upvalue.Closed) throw new ArgumentException("Expected open upvalue");

			bool status;
			if (writtenClosure.TryGetValue(upvalue, out status)) return status;

			status = WrittenClosure(upvalue);
			writtenClosure.Add(upvalue, status);

			return status;
		}

		private bool WrittenParent(Upvalue upvalue)
		{
			foreach (KeyValuePair<IValue, ClosureNew> value in upvalue.KnownValues.ToList())
			{
				ReferenceNew reference = value.Key as ReferenceNew;

				// Must be another upvalue. We'll ignore it for now
				if (reference == null) return false;
				//foreach (ReferenceSet setter in reference.Users.OfType<ReferenceSet>())
				foreach (Instruction insn in reference.Users.OfType<Instruction>())
				{
					switch (insn.Opcode)
					{
						case Opcode.ReferenceGet:
							break;
						case Opcode.ReferenceSet:
							return true;
						case Opcode.ClosureNew:
							{
								ClosureNew closure = (ClosureNew)insn;
								if (closure.Function == upvalue.Function) continue;

								int index = closure.OpenUpvalues.IndexOf(value.Key);
								if (index == -1) throw new Exception("Cannot find closure, but it is being used");

								if (EverWritenClosure(closure.Function.OpenUpvalues[index])) return true;
								break;
							}
					}
				}
			}

			return false;
		}

		/// <summary>
		/// If an upvalue is written in a parent method.
		/// </summary>
		/// <param name="upvalue">Upvalue.</param>
		/// <returns>If the upvalue is written</returns>
		public bool EverWrittenParent(Upvalue upvalue)
		{
			if (upvalue.Closed) throw new ArgumentException("Expected open upvalue");

			bool status;
			if (writtenParent.TryGetValue(upvalue, out status)) return status;

			status = WrittenParent(upvalue);
			writtenParent.Add(upvalue, status);

			return status;
		}

		public bool NeverWritten(Upvalue upvalue)
		{
			return !EverWritenClosure(upvalue) && !EverWrittenParent(upvalue);
		}

		public ISet<IValue> GatherSources(Upvalue upvalue)
		{
			ISet<IValue> vSource;
			if (sources.TryGetValue(upvalue, out vSource)) return vSource;

			vSource = sources[upvalue];
			foreach (IValue source in upvalue.KnownValues.Select(x => x.Key))
			{
				var sourceUp = source as Upvalue;
				if (sourceUp != null)
				{
					vSource.UnionWith(GatherSources(sourceUp));
				}
				else
				{
					vSource.Add(source);
				}
			}

			return vSource;
		}
	}
}

