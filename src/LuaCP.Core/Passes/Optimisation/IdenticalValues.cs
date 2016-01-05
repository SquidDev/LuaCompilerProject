using System;
using LuaCP.IR.Components;
using LuaCP.IR;
using System.Collections.Generic;
using LuaCP.Collections;
using LuaCP.IR.User;
using LuaCP.IR.Instructions;
using System.Linq;

namespace LuaCP.Passes.Optimisation
{
	/// <summary>
	/// A collection of passes that removes an upvalue or phi node if all values are the same.
	/// </summary>
	public static class IdenticalValues
	{
		public static Pass<Function> CheckUpvalues { get { return  DoCheckUpvalues; } }

		public static Pass<Block> CheckPhis { get { return  DoCheckPhis; } }

		private static bool DoCheckUpvalues(Function function)
		{
			if (function.ClosedUpvalues.Count == 0 || function.Users.UniqueCount == 0) return false;

			var changed = new List<KeyValuePair<Upvalue, IValue>>(0);

			foreach (Upvalue upvalue in function.ClosedUpvalues)
			{
				IValue first;
				if (upvalue.KnownValues.Select(x => x.Key).AllEqual(out first) && first is Constant)
				{
					changed.Add(new KeyValuePair<Upvalue, IValue>(upvalue, first));
				}
			}

			foreach (var pair in changed)
			{
				pair.Key.ReplaceWith(pair.Value);

				int index = pair.Key.Index;
				foreach (ClosureNew creator in function.Users.OfType<ClosureNew>())
				{
					creator.ClosedUpvalues.RemoveAt(index);
				}

				pair.Key.Remove();
			}

			return changed.Count > 0;
		}

		private static bool DoCheckPhis(Block block)
		{
			bool changed = false;
			foreach (Phi phi in block.PhiNodes)
			{
				// We'll handle this elsewhere
				if (phi.Users.UniqueCount == 0) continue;

				if (phi.Source.Count == 0) throw new Exception("Empty phi node being used");

				IValue value;
				if (phi.Source.Values.Where(x => x != phi).AllEqual(out value))
				{
					phi.ReplaceWith(value);
					phi.Remove();

					changed = true;
				}
			}

			return changed;
		}
	}
}

