using System;
using System.Collections.Generic;
using LuaCP.IR.Components;
using LuaCP.IR.Instructions;
using System.Linq;
using LuaCP.Optimisation;

namespace LuaCP
{
	public delegate bool Pass<T>(T item);

	public static class PassExtensions
	{
		public static readonly Pass<Module> Default = new Pass<Module>[]
		{
			new UnreachableCode().RunModule,
			new Pass<Function>[]
			{
				new UnreachableCode().RunFunction,
				DeadCode.Runner.AsFunction(),
				ReferenceToValue.Runner,
				ConstantFolding.Runner.AsFunction(),
				FunctionInliner.Runner,
				TupleInliner.Runner.AsFunction(),
			}.Group().Repeat().AsModule(),
		}.Group().Repeat();

		public static Pass<T> Repeat<T>(this Pass<T> pass)
		{
			return x =>
			{
				bool changed = false;
				while (pass(x)) changed = true;
				return changed;
			};
		}

		public static Pass<T> Group<T>(this IEnumerable<Pass<T>> passes)
		{
			return x =>
			{
				bool changed = false;
				foreach (Pass<T> pass in passes)
				{
					if (pass(x)) changed = true;
				}

				return changed;
			};
		}

		public static Pass<U> Select<T, U>(this Pass<T> pass, Func<U, IEnumerable<T>> selector)
		{
			return x =>
			{
				bool changed = false;
				foreach (T item in selector(x))
				{
					if (pass(item)) changed = true;
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