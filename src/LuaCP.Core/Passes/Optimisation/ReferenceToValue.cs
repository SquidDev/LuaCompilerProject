using System;
using System.Collections.Generic;
using System.Linq;
using LuaCP.Graph;
using LuaCP.Collections;
using LuaCP.IR;
using LuaCP.IR.Components;
using LuaCP.IR.Instructions;

namespace LuaCP.Passes.Optimisation
{
	/// <summary>
	/// SSA construction: Converts references to values
	/// </summary>
	public class ReferenceToValue
	{
		private static readonly ReferenceToValue instance = new ReferenceToValue();

		public static Pass<Function> Runner { get { return instance.Run; } }

		private IValue EvaluateReference(Block block, IValue reference, IValue value)
		{
			foreach (Instruction current in block)
			{
				switch (current.Opcode)
				{
					case Opcode.ReferenceGet:
						{
							ReferenceGet getter = (ReferenceGet)current;
							if (getter.Reference == reference) getter.ReplaceWithAndRemove(value);
						}
						break;
					case Opcode.ReferenceSet:
						{
							ReferenceSet setter = (ReferenceSet)current;
							if (setter.Reference == reference)
							{
								value = setter.Value;
								setter.Remove();
							}
						}
						break;
				}
			}
        	
			return value;
		}

		private void Remove(IEnumerable<ReferenceNew> items)
		{
			/*
              Based mostly off: http://ssabook.gforge.inria.fr/latest/book.pdf
              However I might implement some stuff from http://compilers.cs.uni-saarland.de/papers/bbhlmz13cc.pdf
             */
			foreach (ReferenceNew item in items)
			{
				// Remove redundant references - those which are never used
				if (item.Users.UniqueCount == 0)
				{
					item.Block.Remove(item);
					continue;
				}

				List<Instruction> users = item.Users.Cast<Instruction>().ToList();
				if (users.Exists(x => !x.Opcode.IsReferenceInsn())) throw new InvalidOperationException("Unexpected opcode usage");

				// If we never overwrite it, then we can just replace with the original value
				if (users.All(x => x.Opcode == Opcode.ReferenceGet))
				{
					foreach (ValueInstruction getter in users)
					{
						getter.ReplaceWithAndRemove(item.Value);
					}
					item.Remove();
					continue;
				}
                
				if (users.All(x => x is ReferenceSet))
				{
					foreach (Instruction user in users) user.Remove();
					item.Remove();
					continue;
				}

				// If everything happens in one block then it is all fine too.
				if (users.All(x => x.Block == item.Block))
				{
					EvaluateReference(item.Block, item, item.Value);
					item.Remove();
					continue;
				}
                
				Dictionary<Block, Phi> phis = InsertPhiNodes(item, users);
				Dictionary<Block, IValue> values = phis.ToDictionary<Block, IValue, Phi>();
				values.Add(item.Block, item.Value);
                
				Func<Block, IValue> getValue = block =>
				{
					while (true)
					{
						IValue value;
						if (values.TryGetValue(block, out value)) return value;
						block = block.ImmediateDominator;
					}
				};

				foreach (Block block in item.Block.DominancePreorder())
				{
					IValue value = values[block] = EvaluateReference(block, item, getValue(block));
                	
					foreach (Block next in block.Next)
					{
						Phi phi;
						if (phis.TryGetValue(next, out phi))
						{
							phi.Source.Add(block, value);
						}
					}
				}

				item.Remove();
			}
		}

		private ISet<Block> ComputeLiveInBlocks(ReferenceNew reference, List<Instruction> users)
		{
			HashSet<Block> blocks = new HashSet<Block>();
			Queue<Block> worklist = new Queue<Block>();

			HashSet<Block> setters = new HashSet<Block>(users.Where(x => x.Opcode != Opcode.ReferenceGet).Select(x => x.Block));
			HashSet<Block> getters = new HashSet<Block>(users.Where(x => x.Opcode == Opcode.ReferenceGet).Select(x => x.Block));

			// Enqueue all blocks that consume the block
			foreach (Block getter in getters)
			{
				// We will access it after creating the variable, so it doesn't need a phi node
				if (reference.Block == getter) continue;

				// If we don't set it in this block then enqueue it
				if (!setters.Contains(getter))
				{
					blocks.Add(getter);
					worklist.Enqueue(getter);
					continue;
				}

				// We set it here, check if the setter occurs before the getter
				foreach (Instruction instruction in getter)
				{
					bool found = false;
					switch (instruction.Opcode)
					{
						case Opcode.ReferenceGet:
							if (((ReferenceGet)instruction).Reference == reference)
							{
								blocks.Add(getter);
								worklist.Enqueue(getter);
								found = true;
							}
							break;
						case Opcode.ReferenceSet:
							if (((ReferenceSet)instruction).Reference == reference)
							{
								found = true;
							}
							break;
						default:
							break;
					}

					if (found) break;
				}
			}
                
			while (worklist.Count > 0)
			{
				foreach (Block previous in worklist.Dequeue().Previous)
				{
					// Since the reference is live in the current block, the previous items
					// must either be live or a setting block
					if (!setters.Contains(previous) && blocks.Add(previous))
					{
						worklist.Enqueue(previous);
					}
				}
			}

			return blocks;
		}

		private Dictionary<Block, Phi> InsertPhiNodes(ReferenceNew reference, List<Instruction> users)
		{
			Dictionary<Block, Phi> phis = new Dictionary<Block, Phi>();
            
			// users.Where(x => x.Opcode != Opcode.ReferenceGet).Select(x => x.Block)
			HashSet<Block> setters = new HashSet<Block>(ComputeLiveInBlocks(reference, users));
			Queue<Block> toVisit = new Queue<Block>(setters);
			while (toVisit.Count > 0)
			{
				Block b = toVisit.Dequeue();
				foreach (Block dom in b.DominanceFrontier)
				{
					if (!phis.ContainsKey(dom))
					{
						Phi phi = new Phi(dom);
						phis.Add(dom, phi);
            			
						if (setters.Add(dom)) toVisit.Enqueue(dom);
					}
				}
			}
            
			return phis;
		}

		private IEnumerable<ReferenceNew> GetValid(Function function)
		{
			return function.Blocks
            	.SelectMany(x => x)
            	.Where(x => x.Opcode == Opcode.ReferenceNew).Cast<ReferenceNew>()
            	.Where(r => r.Users.OfType<Instruction>().All(x => x.Opcode.IsReferenceInsn()));
		}

		public bool Run(Function function)
		{
			function.Dominators.Evaluate();
			List<ReferenceNew> references = GetValid(function).ToList();
			if (references.Count == 0) return false;
			
			Remove(references);
			return true;
		}
	}
}

