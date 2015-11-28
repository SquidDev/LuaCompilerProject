using System;
using System.Collections.Generic;
using LuaCP.IR;
using LuaCP.IR.Components;
using LuaCP.IR.Instructions;
using Edge = System.Tuple<LuaCP.IR.Components.Block, LuaCP.IR.Components.Block>;

namespace LuaCP.Optimisation.SCCP
{
	public class LatticeValue
	{
		public enum LatticeValueKind
		{
			/// <summary>
			/// Do not know the value yet
			/// </summary>
			Undefined,
			
			/// <summary>
			/// A specified constant value
			/// </summary>
			Constant,
			
			/// <summary>
			/// No clue.
			/// </summary>
			ForcedConstant,
			
			/// <summary>
			/// Know to not be a constant
			/// </summary>
			Overdefined,
			
		}
		private Constant value;
		private LatticeValueKind kind;
		
		public Constant Value { get { return value; } }
		
		public bool IsUndefined { get { return kind == LatticeValueKind.Undefined; } }
		public bool IsConstant
		{
			get { return kind == LatticeValueKind.Constant || kind == LatticeValueKind.ForcedConstant; } 
		}
		public bool IsOverdefined { get { return kind == LatticeValueKind.Overdefined; } }
		
		public bool MarkOverdefined()
		{
			if (kind == LatticeValueKind.Overdefined) return true;
			kind = LatticeValueKind.Overdefined;
			return true;
		}
		
		public bool MarkConstant(Constant constant)
		{
			switch (kind)
			{
				case LatticeValueKind.Constant:
					if (constant != value) throw new InvalidOperationException("Cannot change constant");
					return false;
				case LatticeValueKind.Undefined:
					kind = LatticeValueKind.Constant;
					value = constant;
					return true;
				case LatticeValueKind.Overdefined:
					throw new InvalidOperationException("Cannot convert Overdefined to ForcedConstant");
				case LatticeValueKind.ForcedConstant:
					// If same, stay at forced constant
					if (constant == value) return false;
					
					// Otherwise change to overdefined
					kind = LatticeValueKind.Overdefined;
					return true;
				default:
					throw new InvalidOperationException("Unknown type " + kind);
			}
		}
		
		public void MarkForcedConstant(Constant constant)
		{
			if (kind != LatticeValueKind.Undefined) throw new InvalidOperationException("Cannot convert a non-Undefined value to ForcedConstant");
			value = constant;
			kind = LatticeValueKind.ForcedConstant;
		}
	}

	public class SCCPSolver
	{
		/// <summary>
		/// States the values are in
		/// </summary>
		private readonly Dictionary<IValue, LatticeValue> valueState = new Dictionary<IValue, LatticeValue>();
		
		/// <summary>
		/// Executable blocks
		/// </summary>
		private readonly HashSet<Block> executable = new HashSet<Block>();
		
		/// <summary>
		/// All reachable edges
		/// </summary>
		private readonly HashSet<Edge> possibleEdges = new HashSet<Edge>();
		
		private readonly Queue<IValue> overdefinedWorklist = new Queue<IValue>(64);
		private readonly Queue<IValue> insnWorklist = new Queue<IValue>(64);
		private readonly Queue<Block> blockWorklist = new Queue<Block>(64);
		
		#region Block handling
		/// <summary>
		/// Mark a block as executable
		/// </summary>
		/// <param name="block">The block to mark</param>
		/// <returns>If it was executable before</returns>
		public bool MarkExecutable(Block block)
		{
			if (executable.Add(block))
			{
				blockWorklist.Enqueue(block);
				return true;
			}
			
			return false;
		}
		
		/// <summary>
		/// Check if a block is executable
		/// </summary>
		/// <param name="block">The block to check</param>
		/// <returns>If it is executable</returns>
		public bool IsExecutable(Block block)
		{
			return executable.Contains(block);
		}
		
		public void MarkEdgeExecutable(Block from, Block to)
		{
			Edge edge = new Edge(from, to);
			if (!possibleEdges.Add(edge)) return;
			
			if (!MarkExecutable(to))
			{
				foreach (Phi phi in to.PhiNodes) VisitPhi(phi);
			}
		}
		#endregion
		
		#region Value utilities
		
		public LatticeValue GetValueState(IValue value)
		{
			LatticeValue result;
			if (valueState.TryGetValue(value, out result)) return result;
			
			result = new LatticeValue();
			valueState.Add(value, result);
			
			Constant constant = value as Constant;
			if (constant != null) result.MarkConstant(constant);
			
			return result;
		}
		
		public void MarkConstant(LatticeValue lattice, IValue value, Constant constant)
		{
			if (!lattice.MarkConstant(constant)) return;
			Enqueue(lattice, value);
		}
		
		public void MarkConstant(IValue value, Constant constant)
		{
			MarkConstant(valueState[value], value, constant);
		}
		
		public void MarkForcedConstant(IValue value, Constant constant)
		{
			LatticeValue lattice = valueState[value];
			lattice.MarkForcedConstant(constant);
			Enqueue(lattice, value);
		}
		
		public void MarkOverdefined(LatticeValue lattice, IValue value)
		{
			if (!lattice.MarkOverdefined()) return;
			overdefinedWorklist.Enqueue(value);
		}
		
		public void MarkOverdefined(IValue value)
		{
			MarkOverdefined(valueState[value], value);
		}
		
		private void Enqueue(LatticeValue lattice, IValue value)
		{
			if (lattice.IsOverdefined)
			{
				overdefinedWorklist.Enqueue(value);
			}
			else
			{
				insnWorklist.Enqueue(value);
			}
		}
		
		public void MergeIn(LatticeValue lattice, IValue value, LatticeValue mergeWith)
		{
			if (lattice.IsOverdefined || mergeWith.IsUndefined) return;
			
			if (mergeWith.IsOverdefined)
			{
				MarkOverdefined(lattice, value);
			}
			else if (lattice.IsUndefined)
			{
				MarkConstant(lattice, value, mergeWith.Value);
			}
			else if (lattice.Value != mergeWith.Value)
			{
				MarkOverdefined(lattice, value);
			}
		}
		
		public void MergeIn(IValue value, LatticeValue mergeWith)
		{
			MergeIn(valueState[value], value, mergeWith);
		}
		#endregion
		
		public IEnumerable<Block> GetFeasibleSuccessors(Block block)
		{
			Instruction last = block.Last;
			switch (last.Opcode)
			{
				case Opcode.Branch:
					yield return ((Branch)last).Block;
					break;
				case Opcode.BranchCondition:
					BranchCondition cond = (BranchCondition)last;
					LatticeValue condLattice = GetValueState(cond.Test);
					if (condLattice.IsConstant)
					{
						yield return condLattice.Value.Literal.IsTruthy() ? cond.Success : cond.Failure;
					}
					else if (condLattice.IsOverdefined)
					{
						yield return cond.Success;
						yield return cond.Failure;
					}
					break;
			}
		}
		
		public bool IsEdgeFeasible(Block from, Block to)
		{
			Instruction last = from.Last;
			switch (last.Opcode)
			{
				case Opcode.Branch:
					return to == ((Branch)last).Block;
				case Opcode.BranchCondition:
					BranchCondition cond = (BranchCondition)last;
					LatticeValue condLattice = GetValueState(cond.Test);
					if (condLattice.IsConstant)
					{
						return (condLattice.Value.Literal.IsTruthy() ? cond.Success : cond.Failure) == to;
					}
					else if (condLattice.IsOverdefined)
					{
						return cond.Success == to || cond.Failure == to;
					}
					else
					{
						// Undefined shouldn't be visited yet
						return false;
					}
			}

			return false;
		}
		
		#region Visitors
		private void VisitPhi(Phi phi)
		{
			LatticeValue latticeState = GetValueState(phi);
			if (latticeState.IsOverdefined) return;
			
			Constant current = null;
			foreach (KeyValuePair<Block, IValue> edgeValue in phi.Source)
			{
				LatticeValue item = GetValueState(edgeValue.Value);
				
				// Ignore undefined for the time being
				if (item.IsUndefined) continue;
				if (!IsEdgeFeasible(edgeValue.Key, phi.Block)) continue;
				
				if (item.IsOverdefined)
				{
					MarkOverdefined(phi);
					return;
				}
				
				if (current == null)
				{
					current = item.Value;
					continue;
				}
				
				if (current != item.Value)
				{
					MarkOverdefined(phi);
					return;
				}
			}
			
			if (current != null) MarkConstant(phi, current);
		}
		
		private void VisitTerminator(Instruction insn)
		{
			foreach (Block next in GetFeasibleSuccessors(insn.Block))
			{
				MarkEdgeExecutable(insn.Block, next);
			}
		}
		#endregion
	}
}
