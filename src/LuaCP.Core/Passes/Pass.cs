using System;
using System.Collections.Generic;
using System.Linq;
using LuaCP.IR.Components;
using LuaCP.IR.Instructions;
using LuaCP.Passes.Optimisation;

namespace LuaCP.Passes
{
	public delegate bool Pass<T>(PassManager handler, T item);

	public static class PassExtensions
	{
		public static readonly Pass<Module> Default = new Pass<Module>[]
		{
			UnreachableCode.ForModule,
			DemoteUpvalue.Runner,
			new Pass<Function>[]
			{
				UnreachableCode.ForFunction,
				DeadCode.Runner.AsFunction(),
				ReferenceToValue.Runner,
				ConstantFolding.Runner.AsFunction(),
				FunctionInliner.Runner,
				TupleInliner.Runner.AsFunction(),
				BranchToValue.Runner.AsFunction(),
				IdenticalValues.CheckPhis.AsFunction(),
				IdenticalValues.CheckUpvalues,
				ClosureLifting.Runner,
			}.Group().Repeat().AsModule(),
		}.Group().Repeat();

		public static Pass<T> Repeat<T>(this Pass<T> pass)
		{
			return (data, x) =>
			{
				bool changed = false;
				while (pass(data, x)) changed = true;
				return changed;
			};
		}

		public static Pass<T> Group<T>(this IEnumerable<Pass<T>> passes)
		{
			return (data, x) =>
			{
				bool changed = false;
				foreach (Pass<T> pass in passes)
				{
					changed |= pass(data, x);
				}

				return changed;
			};
		}

		public static Pass<TOut> Select<TIn, TOut>(this Pass<TIn> pass, Func<TOut, IEnumerable<TIn>> selector)
		{
			return (data, x) =>
			{
				bool changed = false;
				foreach (TIn item in selector(x))
				{
					changed |= pass(data, item);
				}
				return changed;
			};
		}

		public static Pass<Block> AsBlock(this Pass<Instruction> pass)
		{
			return pass.Select<Instruction, Block>(x => x);
		}

		public static Pass<Function> AsFunction(this Pass<Block> pass)
		{
			return pass.Select<Block, Function>(x => x.Blocks);
		}

		public static Pass<Function> AsFunction(this Pass<Instruction> pass)
		{
			return pass.Select<Instruction, Function>(x => x.Blocks.SelectMany(y => y));
		}

		public static Pass<Module> AsModule(this Pass<Function> pass)
		{
			return pass.Select<Function, Module>(x => x.Functions);
		}
	}
}
