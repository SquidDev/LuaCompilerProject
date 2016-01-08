using System.Collections.Generic;
using System.Linq;
using LuaCP.IR;
using LuaCP.IR.Components;
using LuaCP.IR.Instructions;
using LuaCP.IR.User;
using LuaCP.Passes.Tools;
using LuaCP.Debug;

namespace LuaCP.Passes.Optimisation
{
	/// <summary>
	/// Attempt to inline functions
	/// </summary>
	public static class FunctionInliner
	{
		public static Pass<Function> Runner { get { return Run; } }

		private static bool Run(PassManager data, Function function)
		{
			IEnumerable<ClosureNew> closures = function.Blocks
				.SelectMany(x => x)
				.Where(x => x.Opcode == Opcode.ClosureNew)
				.Cast<ClosureNew>()
                .Where(x =>
				{
					if (x.Users.TotalCount != 1) return false;

					Call user = x.Users.First<IUser<IValue>>() as Call;
					return user != null && user.Method == x;
				})
				.ToList();

			bool changed = false;
			foreach (ClosureNew closNew in closures)
			{
				Inline(closNew);
				changed = true;
			}
			
			return changed;
		}

		public static void Inline(ClosureNew closure)
		{
			Block block = closure.Block;
			Function function = block.Function;
			
			Call caller = (Call)closure.Users.First<IUser<IValue>>();
			Block callerBlock = caller.Block;

			int argCount = closure.Function.Arguments.Count;
			bool dots = false;
			IValue[] args = new IValue[argCount];
			if (argCount > 0 && closure.Function.Arguments.Last().Kind == ValueKind.Tuple)
			{
				dots = true;
				argCount--;
			}

			for (int i = 0; i < argCount; i++)
			{
				TupleGet getter = new TupleGet(caller.Arguments, i);
				args[i] = getter;
				callerBlock.AddBefore(caller, getter);
			}
			if (dots)
			{
				TupleRemainder getter = new TupleRemainder(caller.Arguments, argCount);
				args[argCount] = getter;
				callerBlock.AddBefore(caller, getter);
			}
			
			// Segment the block
			Block contination = Segment(caller);
			ReturnCloner clone = new ReturnCloner(
				                     closure.Function, 
				                     function,
				                     contination,
				                     args, 
				                     closure.OpenUpvalues,
				                     closure.ClosedUpvalues
			                     );
			clone.Run();

			caller.ReplaceWithAndRemove(clone.Value);
			closure.Remove();
			
			callerBlock.AddLast(new Branch(clone.EntryPoint));
		}

		/// <summary>
		/// Splits a point into two
		/// </summary>
		/// <param name="splitPoint">The point at which the block is split, all after this are put in the new block</param>
		/// <returns>The new block</returns>
		public static Block Segment(Instruction splitPoint)
		{
			Block current = splitPoint.Block;
			Block contination = new Block(current.Function);
			
			Instruction next = splitPoint.Next;
			while (next != null)
			{
				Instruction value = next;
				next = value.Next;

				current.Remove(value);
				contination.AddLast(value);
			}
			
			return contination;
		}
	}
}
