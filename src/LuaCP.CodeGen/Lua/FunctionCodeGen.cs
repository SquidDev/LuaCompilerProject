using System;
using LuaCP.IR.Components;
using System.Collections.Generic;
using LuaCP.IR;
using LuaCP.IR.Instructions;
using System.Linq;
using LuaCP.Graph;
using LuaCP.Collections;

namespace LuaCP.CodeGen.Lua
{
	/// <summary>
	/// Non reusable class for writing to a stream
	/// </summary>
	public sealed class FunctionCodegen
	{
		public readonly Function Function;
		public readonly IReadOnlyDictionary<Upvalue, String> Upvalues;

		public readonly NameAllocator<Phi> Phis;
		public readonly NameAllocator<IValue> Refs;
		public readonly NameAllocator<Block> Blocks;
		public readonly NameAllocator<Function> FuncAllocator;
		public readonly Dictionary<IValue, int> Slots;
		public readonly String Prefix;
		public readonly int SlotCount;
		public readonly IndentedTextWriter Writer;

		private readonly HashSet<Block> visited = new HashSet<Block>();

		internal FunctionCodegen(Function function, IndentedTextWriter writer, IReadOnlyDictionary<Upvalue, String> upvalues, NameAllocator<Function> funcAllocator)
		{
			Upvalues = upvalues;
			Function = function;
			FuncAllocator = funcAllocator;
			Writer = writer;

			string prefix = funcAllocator[function];
			Phis = new NameAllocator<Phi>(prefix + "phi_{0}");
			Refs = new NameAllocator<IValue>(prefix + "ref_{0}");
			Blocks = new NameAllocator<Block>(prefix + "lbl_{0}");
			Slots = RegisterAllocation.Allocate(function, out SlotCount);
			Prefix = prefix + "var_";
		}

		public FunctionCodegen(Function function, IndentedTextWriter writer)
			: this(function, writer, DefaultUpvalues(function), new NameAllocator<Function>("f{0}_"))
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
			if (value.Kind == ValueKind.Reference) return Refs[value];
			return Prefix + Slots[value];
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

		public void WriteBlock(Block block)
		{
			if (visited.Add(block))
			{
				new BlockCodegen(block, this, Writer).Write();
			}
			else
			{
				throw new ArgumentException("Already written block");
			}
		}

		private void WriteBlocks()
		{
			foreach (Block block in Function.EntryPoint.ReachableLazy())
			{
				if (!visited.Contains(block)) WriteBlock(block);
			}
		}

		public void Write()
		{
			Writer.Write("function(");

			// Setup arguments
			HashSet<int> used = new HashSet<int>();
			bool first = true;
			foreach (Argument argument in Function.Arguments)
			{
				if (!first)
				{
					Writer.Write(", ");
				}
				else
				{
					first = false;
				}

				if (argument.Kind == ValueKind.Value)
				{
					int index = Slots[argument];
					used.Add(index);
					Writer.Write(Prefix + index);
				}
				else
				{
					Writer.Write("...");
				}
			}
			Writer.WriteLine(")");

			Writer.Indent++;

			// Write local predeclarations
			if (used.Count < SlotCount)
			{
				Writer.Write("local _");
				for (int i = 0; i < SlotCount; i++)
				{
					if (used.Contains(i)) continue;

					Writer.Write(", ");
					Writer.Write(Prefix + i);
				}
			}
			Writer.WriteLine();

			WriteBlocks();
			Writer.Indent--;

			Writer.WriteLine("end");
		}

		#endregion
	}
}

