using LuaCP.IR.Components;
using System.Collections.Generic;
using LuaCP.IR.Instructions;
using System.Linq;
using LuaCP.IR;
using LuaCP.IR.User;

namespace LuaCP.Passes.Optimisation
{
	public static class ClosureLifting
	{
		public static Pass<Function> Runner { get { return Run; } }

		public static bool Run(PassManager data, Function function)
		{
			if (function.OpenUpvalues.Count > 0) return false;

			List<ClosureNew> uses = function.Users.OfType<ClosureNew>().ToList();

			// We'll trim it anyway
			if (uses.Count == 0) return false;

			// We can't hoist it if we are in the root function
			if (uses.Exists(x => x.Block.Function == function.Module.EntryPoint)) return false;

			if (uses.SelectMany<ClosureNew, IUser<IValue>>(x => x.Users).All(x => x is Call))
			{
				function.AddArguments(0, function.ClosedUpvalues.Select((x, i) => "upvalue" + i));

				for (int i = function.ClosedUpvalues.Count - 1; i >= 0; i--)
				{
					Upvalue upvalue = function.ClosedUpvalues[i];
					upvalue.ReplaceWith<IValue>(function.Arguments[i]);
					upvalue.Remove();
				}

				var upvalues = uses
					.Select(x => x.Block.Function)
					.Distinct()
					.ToDictionary(x => x, f =>
				{
					Upvalue upvalue = new Upvalue(f, true);
					foreach (ClosureNew closure in f.Closures)
					{
						ClosureNew factory = new ClosureNew(function, Enumerable.Empty<IValue>(), Enumerable.Empty<IValue>());
						closure.Block.AddBefore(closure, factory);
						closure.ClosedUpvalues.Add(factory);
					}

					return upvalue;
				});

				foreach (ClosureNew closure in uses)
				{
					foreach (Call call in closure.Users.OfType<Call>())
					{
						TupleNew args = new TupleNew(closure.ClosedUpvalues, call.Arguments);
						call.Block.AddBefore(call, args);
						call.Arguments = args;
					}

					closure.ReplaceWithAndRemove(upvalues[closure.Block.Function]);
				}

				return true;
			}

			return false;
		}
	}
}

