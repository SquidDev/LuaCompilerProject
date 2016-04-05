using System.Collections.Generic;
using LuaCP.IR;
using LuaCP.IR.Components;
using LuaCP.Passes.Analysis;

namespace LuaCP.CodeGen.Lua
{
	public sealed partial class FunctionCodeGen
	{
		public readonly Function Function;

		/// <summary>
		/// Mapping of upvalues to names
		/// </summary>
		private readonly IReadOnlyDictionary<Upvalue, string> upvalues;

		/// <summary>
		/// Reference lookup
		/// </summary>
		private readonly NameAllocator<IValue> refs;

		/// <summary>
		/// Jump target lookup
		/// </summary>
		private readonly NameAllocator<Block> blocks;

		/// <summary>
		/// Function prefix lookup
		/// </summary>
		private readonly NameAllocator<Function> funcAllocator;

		/// <summary>
		/// Slot lookup
		/// </summary>
		private readonly Dictionary<IValue, int> slots;

		/// <summary>
		/// Prefix for variables
		/// </summary>
		private readonly string varPrefix;

		/// <summary>
		/// Number of temp variables
		/// </summary>
		private readonly int slotCount;

		/// <summary>
		/// The writer
		/// </summary>
		private readonly IndentedTextWriter writer;

		/// <summary>
		/// If this is the root function
		/// </summary>
		private readonly bool root;

		private readonly ControlGroup rootGroup;

		internal FunctionCodeGen(Function function, IndentedTextWriter writer, IReadOnlyDictionary<Upvalue, string> upvalues, NameAllocator<Function> funcAllocator, bool root = false)
		{
			this.upvalues = upvalues;
			Function = function;
			this.funcAllocator = funcAllocator;
			this.writer = writer;
			this.root = root;
			rootGroup = new BranchAnalysis(function).Group;

			string prefix = funcAllocator[function];
			refs = new NameAllocator<IValue>(prefix + "ref_{0}");
			blocks = new NameAllocator<Block>(prefix + "lbl_{0}");

			var live = RegisterAllocation.GetLiveBlocks(function, x => x.Kind == ValueKind.Value);
			var equal = RegisterAllocation.BuildPhiMap(function, live);
			slots = RegisterAllocation.Allocate(function, live, equal, out slotCount);
			varPrefix = prefix + "var_";
		}

		public FunctionCodeGen(Function function, IndentedTextWriter writer)
			: this(function, writer, DefaultUpvalues(function), new NameAllocator<Function>("f{0}_"), true)
		{
		}

		private static Dictionary<Upvalue, string> DefaultUpvalues(Function function)
		{
			var upvalues = new Dictionary<Upvalue, string>();

			bool isEnv = function == function.Module.EntryPoint;

			int i = 0;
			foreach (Upvalue upvalue in function.Upvalues)
			{
				if (isEnv)
				{
					isEnv = false;
					upvalues.Add(upvalue, "_ENV");
				}
				else
				{
					upvalues.Add(upvalue, "unbound_" + i);
					i++;
				}
			}

			return upvalues;
		}

		public void Write()
		{
			bool requiresPrototype = true;
			bool wrap = false;
			if (root)
			{
				switch (Function.Arguments.Count)
				{
					case 0:
						requiresPrototype = false;
						break;
					case 1:
						requiresPrototype = Function.Dots == null;
						break;
					default:
						wrap = true;
						break;
				}
			}

			HashSet<int> used = new HashSet<int>();

			if (wrap) writer.Write("return (");

			// Write arguments
			if (requiresPrototype)
			{
				writer.Write("function(");

				// Setup arguments
				bool first = true;
				foreach (Argument argument in Function.Arguments)
				{
					if (!first)
					{
						writer.Write(", ");
					}
					else
					{
						first = false;
					}

					if (argument.Kind == ValueKind.Value)
					{
						int index = slots[argument];
						used.Add(index);
						writer.Write(varPrefix + index);
					}
					else
					{
						writer.Write("...");
					}
				}
				writer.WriteLine(")");

				writer.Indent++;
			}

			// Write local predeclarations
			if (used.Count < slotCount)
			{
				writer.Write("local _");
				for (int i = 0; i < slotCount; i++)
				{
					if (used.Contains(i)) continue;

					writer.Write(", ");
					writer.Write(varPrefix + i);
				}
				writer.WriteLine();
			}

			WriteGroup(rootGroup, null);

			if (requiresPrototype)
			{
				writer.Indent--;
				writer.Write("end");
			}

			if (wrap) writer.Write(")()");
			writer.WriteLine();
		}
	}
}

