using System;
using System.Linq;
using LuaCP.IR;
using LuaCP.IR.Components;
using LuaCP.IR.Instructions;
using LuaCP.IR.User;

namespace LuaCP.Optimisation
{
	/// <summary>
	/// Removes useless constructs including: 
	///  - Unused instructions
	///  - Unused phi nodes
	///  - Phi nodes where all options are identical
	///  - Conditions on constants
	///  - Branches just composed of a jump instruction
	/// </summary>
	public class DeadCode
	{
		private static readonly DeadCode instance = new DeadCode();

		public static Pass<Block> Runner { get { return instance.Run; } }

		public bool Run(Block block)
		{
			bool changed = false;
			Function function = block.Function;

			// Simplify Phi nodes
			foreach (Phi phi in block.PhiNodes.ToList())
			{
				// Same as all values. If we never use it then clear it
				if (phi.Users.UniqueCount == 0)
				{
					phi.Source.Clear();
					block.PhiNodes.Remove(phi);
					changed = true;
				}
				else if (phi.Source.Count == 0)
				{
					// Something is wrong here.
					throw new ArgumentException("Empty phi node being used");
				}
				else if (phi.Source.Values.Where(x => x != phi).AllEqual())
				{
					IValue value = phi.Source.Values.Where(x => x != phi).First();
					phi.ReplaceWith(value);
				
					phi.Source.Clear();
					block.PhiNodes.Remove(phi);
					changed = true;
				}
			}

			// Merge blocks together
			if (block.Next.Count() == 1)
			{
				Block next = block.Next.First();
				if (next.Previous.Count() == 1)
				{
					block.Last.Remove();
					foreach (Instruction insn in next)
					{
						insn.Block.Remove(insn);
						block.AddLast(insn);
					}
					next.ReplaceWith(block);

					next.Function.Dominators.Invalidate();
					if (function.EntryPoint == next) function.EntryPoint = block;
					changed = true;
				}
			}
			
			foreach (Instruction element in block)
			{
				// We presume that operations don't have a side effect.
				// If they do then something is breaking
				if (element.Opcode.IsBinaryOperator() || element.Opcode.IsUnaryOperator())
				{
					IValue value = (IValue)element;
					if (value.Users.UniqueCount == 0)
					{
						element.Remove();
						changed = true;
					}
				}
				else
				{
					switch (element.Opcode)
					{
						case Opcode.ValueCondition:
							{
								ValueCondition condition = (ValueCondition)element;
								if (condition.Users.UniqueCount == 0)
								{
									// Never used? Remove!
									condition.Remove();
									changed = true;
								}
								else
								{
									// Constant term
									Constant c = condition.Test as Constant;
									if (c != null)
									{
										condition.ReplaceWithAndRemove(c.Literal.IsTruthy() ? condition.Success : condition.Failure);
										changed = true;
									}
								}
								break;
							}
						case Opcode.ReferenceNew:
						case Opcode.TableNew:
						case Opcode.TableGet:
						case Opcode.ReferenceGet:
						case Opcode.ClosureNew:
						case Opcode.TupleNew:
						case Opcode.TupleGet:
						case Opcode.TupleRemainder:
							{
								// Never used? Remove!
								// TODO: Inline tuple access & phi access
								IValue val = (IValue)element;
								if (val.Users.UniqueCount == 0)
								{
									element.Remove();
									changed = true;
								}
								break;
							}
						case Opcode.BranchCondition:
							{
								BranchCondition branch = (BranchCondition)element;
								Constant c = branch.Test as Constant;
								if (c != null)
								{
									// Fold constants!
									Block success = branch.Success, failure = branch.Failure;
									branch.Remove();
									if (c.Literal.IsTruthy())
									{
										block.AddLast(new Branch(success));
										RemoveBranch(block, failure);
									}
									else
									{
										block.AddLast(new Branch(failure));
										RemoveBranch(block, success);
									}
									changed = true;
								}
								break;
							}
					}
				}
			}
			
			return changed;
		}

		public void RemoveBranch(Block current, Block next)
		{
			next.Function.Dominators.Invalidate();
			if (!next.Previous.Contains(current))
			{
				foreach (Phi phi in next.PhiNodes) phi.Source.Remove(current);
			}
		}
	}
}
