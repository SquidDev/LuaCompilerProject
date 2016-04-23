using System;
using System.Collections.Generic;
using System.Linq;
using LuaCP.Collections;
using LuaCP.Graph;
using LuaCP.IR;
using LuaCP.IR.Components;
using LuaCP.IR.Instructions;
using LuaCP.IR.User;
using LuaCP.Passes.Analysis;

namespace LuaCP.CodeGen
{
	public static class RegisterAllocator
	{
		public static Dictionary<IValue, HashSet<Block>> GetLiveBlocks(Function function, Func<IValue, bool> predicate)
		{
			var insns = function.Blocks
				.SelectMany(x => x)
				.OfType<ValueInstruction>();
			var phis = function.Blocks
				.SelectMany(x => x.PhiNodes);

			var live = new Dictionary<IValue, HashSet<Block>>();

			foreach (var argument in function.Arguments.Where(predicate))
			{
				live.Add(argument, Liveness.LiveBlocks(argument, function.EntryPoint));
			}
			foreach (var insn in insns.Where<ValueInstruction>(predicate))
			{
				live.Add(insn, Liveness.LiveBlocks(insn, insn.Block));
			}
			foreach (var phi in phis.Where<Phi>(predicate))
			{
				live.Add(phi, Liveness.LiveBlocks(phi, phi.Block));
			}

			return live;
		}

		public static EqualityMap<IValue> BuildPhiMap(Function function, Dictionary<IValue, HashSet<Block>> live)
		{
			var map = new EqualityMap<IValue>();
			foreach (Block block in function.Blocks)
			{
				foreach (Phi phi in block.PhiNodes)
				{
					foreach (IValue value in phi.Source.Values)
					{
						if (live.ContainsKey(value))
						{
							map.Equate(phi, value);
						}
					}
				}
			}

			return map;
		}

		public static Dictionary<IValue, int> Allocate(Function function, Dictionary<IValue, HashSet<Block>> live, EqualityMap<IValue> values, out int count)
		{
			var indexes = new Allocator<IValue>();
				
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
						if (belongs == null || belongs.Owner != block) currentlyLive.Add(value.Key);

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

					if (live.ContainsKey(phi))
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
					if (value != null && live.ContainsKey(value))
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

			EqualityMap<int> map = null;
			if (values != null)
			{
				map = new EqualityMap<int>();
				map.UnionWith<IValue>(values, x =>
				{
					if (!live.ContainsKey(x)) throw new KeyNotFoundException("No key for " + x);
					return indexes[x];
				});
			}

			var solved = graph.Colour(map);
			count = solved.ColourCount;

			var result = new Dictionary<IValue, int>(live.Count);
			foreach (IValue value in live.Keys)
			{
				result.Add(value, solved.Colours[indexes[value]]);
			}

			return result;
		}
	}
}

