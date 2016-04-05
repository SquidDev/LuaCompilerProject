using System;
using System.Collections.Generic;
using System.Linq;
using LuaCP.Graph;
using LuaCP.IR;
using LuaCP.IR.Components;

namespace LuaCP.CodeGen.Lua
{
	public sealed partial class FunctionCodegen
	{
		public readonly Function Function;
		private readonly IReadOnlyDictionary<Upvalue, String> Upvalues;

		private readonly NameAllocator<IValue> refs;
		private readonly NameAllocator<Block> blocks;
		private readonly NameAllocator<Function> funcAllocator;
		private readonly Dictionary<IValue, int> slots;
		private readonly String varPrefix;
		private readonly int slotCount;
		private readonly IndentedTextWriter writer;
		private readonly bool root;

		private readonly HashSet<Block> visited = new HashSet<Block>();

		internal FunctionCodegen(Function function, IndentedTextWriter writer, IReadOnlyDictionary<Upvalue, String> upvalues, NameAllocator<Function> funcAllocator, bool root = false)
		{
			Upvalues = upvalues;
			Function = function;
			this.funcAllocator = funcAllocator;
			this.writer = writer;
			this.root = root;

			string prefix = funcAllocator[function];
			refs = new NameAllocator<IValue>(prefix + "ref_{0}");
			blocks = new NameAllocator<Block>(prefix + "lbl_{0}");

			var live = RegisterAllocation.GetLiveBlocks(function, x => x.Kind == ValueKind.Value);
			var equal = RegisterAllocation.BuildPhiMap(function, live);
			slots = RegisterAllocation.Allocate(function, live, equal, out slotCount);
			varPrefix = prefix + "var_";
		}

		public FunctionCodegen(Function function, IndentedTextWriter writer)
			: this(function, writer, DefaultUpvalues(function), new NameAllocator<Function>("f{0}_"), true)
		{
		}

		private static Dictionary<Upvalue, String> DefaultUpvalues(Function function)
		{
			var upvalues = new Dictionary<Upvalue, String>();

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

		#region Format

		public string Format(IValue value)
		{
			if (value is Constant) return ((Constant)value).ToString();
			if (value is Upvalue) return Upvalues[(Upvalue)value];
			if (value.Kind == ValueKind.Reference) return refs[value];
			return GetName(value);
		}

		public string GetName(IValue value)
		{
			return varPrefix + slots[value];
		}

		public string FormatKey(IValue value, bool dot = true)
		{
			Constant constant = value as Constant;
			if (constant != null && constant.Literal.Kind == LiteralKind.String)
			{
				string contents = ((Literal.String)constant.Literal).Item;
				if ((Char.IsLetter(contents[0]) || contents[0] == '_') && contents.All(x => x == '_' || Char.IsLetterOrDigit(x)))
				{
					return dot ? "." + contents : contents;
				}
			}

			return "[" + Format(value) + "]";
		}

		#endregion

		#region Block writing

		private void WriteBlocks()
		{
			foreach (Block block in Function.EntryPoint.ReachableLazy())
			{
				if (!visited.Contains(block)) WriteBlock(block);
			}
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

			WriteBlocks();

			if (requiresPrototype)
			{
				writer.Indent--;
				writer.Write("end");
			}

			if (wrap) writer.Write(")()");
			writer.WriteLine();
		}

		#endregion
	}
}

