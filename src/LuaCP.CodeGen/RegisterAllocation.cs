using System;
using System.Collections.Generic;
using LuaCP.IR;
using LuaCP.Graph;
using LuaCP.IR.Components;
using LuaCP.IR.Instructions;
using System.Linq;
using LuaCP.Passes.Analysis;
using LuaCP.Collections;
using LuaCP.IR.User;
using LuaCP.Debug;

namespace LuaCP.CodeGen
{
	public static class RegisterAllocation
	{
		public static Dictionary<IValue, int> Allocate(Function function, out int count)
		{
			// We exclude tuples & references because they require special handling
			var insns = function.Blocks
				.SelectMany(x => x)
				.OfType<ValueInstruction>();
			var phis = function.Blocks
				.SelectMany(x => x.PhiNodes);

			var live = new Dictionary<IValue, HashSet<Block>>();
			var indexes = new Allocator<IValue>();

			foreach (var argument in function.Arguments.Where(x => x.Kind == ValueKind.Value))
			{
				live.Add(argument, Liveness.LiveBlocks(argument, function.EntryPoint));
			}
			foreach (var insn in insns.Where(x => x.Kind == ValueKind.Value))
			{
				live.Add(insn, Liveness.LiveBlocks(insn, insn.Block));
			}
			foreach (var phi in phis.Where(x => x.Kind == ValueKind.Value))
			{
				live.Add(phi, Liveness.LiveBlocks(phi, phi.Block));
			}
				
			var graph = new UndirectedGraph(live.Count);
			foreach (Block block in function.Blocks)
			{
				var currentlyLive = new HashSet<IValue>();
				var boundaries = new Multimap<IUser<IValue>, IValue>();
				foreach (var value in live)
				{
					if (value.Value.Contains(block))
					{
						var belongs = value.Key as IBelongs<Block>;
						if (belongs == null || belongs.Owner != block)
						{
							currentlyLive.Add(value.Key);
						}

						var boundary = Liveness.Boundary(value.Key, block, value.Value);
						if (boundary != null) boundaries.Add(boundary, value.Key);
					}
				}

				// Add initial connections
				foreach (IValue a in currentlyLive)
				{
					int index = indexes[a];
					foreach (IValue b in currentlyLive)
					{
						graph.AddEdge(index, indexes[b]);
					}
				}

				foreach (Phi phi in block.PhiNodes)
				{
					foreach (var value in boundaries.GetEnumerable(phi))
					{
						currentlyLive.Remove(value);
					}

					if (phi.Kind == ValueKind.Value)
					{
						int index = indexes[phi];
						foreach (IValue other in currentlyLive)
						{
							graph.AddEdge(index, indexes[other]);
						}

						currentlyLive.Add(phi);
					}
				}

				foreach (Instruction insn in block)
				{
					var user = insn as IUser<IValue>;
					if (user != null) currentlyLive.ExceptWith(boundaries.GetEnumerable(user));

					var value = insn as IValue;
					if (value != null && value.Kind == ValueKind.Value)
					{
						int index = indexes[value];
						foreach (IValue other in currentlyLive)
						{
							graph.AddEdge(index, indexes[other]);
						}

						currentlyLive.Add(value);
					}
				}
			}

			var solved = graph.Colour();
			count = solved.ColourCount;

			var result = new Dictionary<IValue, int>(live.Count);
			int i = 0;
			foreach (IValue value in live.Keys)
			{
				result.Add(value, solved.Colours[i]);
				i++;
			}

			return result;
		}
	}
}

