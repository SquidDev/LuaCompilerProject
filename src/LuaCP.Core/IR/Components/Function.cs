using System;
using System.Collections.Generic;
using System.Linq;

using LuaCP.Graph;
using LuaCP.IR.Instructions;
using LuaCP.IR.User;
using LuaCP.Collections;

namespace LuaCP.IR.Components
{
	public sealed class Function : IUsable<Function>
	{
		public readonly Module Module;

		private Block entryPoint;
		private readonly List<Argument> arguments;
		private readonly CountingSet<IUser<Function>> users = new CountingSet<IUser<Function>>();

		public readonly HashSet<Block> Blocks = new HashSet<Block>();
		public readonly Argument Dots;
		public readonly Valid Dominators;

		internal readonly List<Upvalue> openUpvalues = new List<Upvalue>();
		internal readonly List<Upvalue> closedUpvalues = new List<Upvalue>();

		public Function(Module module, IEnumerable<string> args, bool dots)
		{
			Dominators = new Valid(InvalidateDominator, () => EntryPoint.BuildDominators());
            
			Module = module;
			module.Functions.Add(this);
			EntryPoint = new Block(this);

			arguments = args.Select(x => Argument.Arg(this, x)).ToList();
			if (dots)
			{
				Dots = Argument.Dots(this);
				arguments.Add(Dots);
			}
		}

		public Block EntryPoint
		{
			get { return entryPoint; }
			set
			{
				if (!Blocks.Contains(value)) throw new ArgumentException("Block does not belong to this function");
				entryPoint = value;
			}
		}

		public Argument AddArgument(int index, string name)
		{
			Argument argument = Argument.Arg(this, name);
			arguments.Insert(index, argument);
			return argument;
		}

		public void AddArguments(int index, IEnumerable<string> names)
		{
			arguments.InsertRange(index, names.Select(name => Argument.Arg(this, name)));
		}

		public IReadOnlyList<Argument> Arguments { get { return arguments; } }

		public CountingSet<IUser<Function>> Users { get { return users; } }

		public IReadOnlyList<Upvalue> OpenUpvalues { get { return openUpvalues; } }

		public IReadOnlyList<Upvalue> ClosedUpvalues { get { return closedUpvalues; } }

		public IEnumerable<Upvalue> Upvalues { get { return openUpvalues.Concat(closedUpvalues); } }

		private void InvalidateDominator()
		{
			foreach (Block node in Blocks)
			{
				node.ImmediateDominator = null;
				node.DominatorTreeChildren.Clear();
				node.DominanceFrontier.Clear();
			}
		}

		public IEnumerable<Call> Callers
		{
			get
			{
				return Closures.SelectMany(x => x.Users.OfType<Call>());
			}
		}

		public IEnumerable<ClosureNew> Closures
		{
			get
			{
				return users.OfType<ClosureNew>();
			}
		}
	}
}

