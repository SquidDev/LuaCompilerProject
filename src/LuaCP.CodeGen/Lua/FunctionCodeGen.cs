using System;
using LuaCP.IR.Components;
using System.Collections.Generic;
using LuaCP.IR;
using LuaCP.IR.Instructions;
using System.Linq;
using LuaCP.Graph;

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
		public readonly NameAllocator<IValue> Temps;
		public readonly NameAllocator<IValue> Refs;
		public readonly NameAllocator<Block> Blocks;
		public readonly NameAllocator<Function> FuncAllocator;
		public readonly IndentedTextWriter Writer;

		private readonly HashSet<Block> visited = new HashSet<Block>();

		public FunctionCodegen(Function function, IndentedTextWriter writer, IReadOnlyDictionary<Upvalue, String> upvalues, NameAllocator<Function> funcAllocator)
		{
			Upvalues = upvalues;
			Function = function;
			FuncAllocator = funcAllocator;
			Writer = writer;

			string prefix = funcAllocator[function];
			Phis = new NameAllocator<Phi>(prefix + "phi_{0}");
			Temps = new NameAllocator<IValue>(prefix + "temp_{0}");
			Refs = new NameAllocator<IValue>(prefix + "var_{0}");
			Blocks = new NameAllocator<Block>(prefix + "lbl_{0}");

			foreach (Argument argument in function.Arguments) Temps[argument] = argument.Name == "..." ? "..." : prefix + argument.Name;
		}

		public FunctionCodegen(Function function, IndentedTextWriter writer)
			: this(function, writer, new Dictionary<Upvalue, String>(), new NameAllocator<Function>("f{0}_"))
		{
		}

		#region Format

		public string Format(IValue value)
		{
			var constant = value as Constant;
			if (constant != null)
			{
				return constant.Literal == Literal.Nil ? "nil" : constant.ToString();
			}

			if (value is Upvalue) return Upvalues[(Upvalue)value];
			if (value is Phi) return Phis[(Phi)value];
			if (value.Kind == ValueKind.Reference) return Refs[value];

			return Temps[value];
		}

		public string FormatTuple(IValue value, bool require = false)
		{
			if (value.IsNil()) return require ? "nil" : "";
			if (value.Kind != ValueKind.Tuple) return Format(value);

			TupleNew tuple = value as TupleNew;
			if (tuple != null && tuple.Remaining.IsNil()) return String.Join(", ", tuple.Values.Select(Format));

			return Temps[value] + " --[[Probably incorrect tuple]]";
		}

		public string FormatKey(IValue value)
		{
			Constant constant = value as Constant;
			if (constant != null && constant.Literal.Kind == LiteralKind.String)
			{
				string contents = ((Literal.String)constant.Literal).Item;
				if ((Char.IsLetter(contents[0]) || contents[0] == '_') && contents.All(x => x == '_' || Char.IsLetterOrDigit(x)))
				{
					return "." + contents;
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
			bool first = true;
			foreach (Argument argument in Function.Arguments)
			{
				if (!first)
				{
					Writer.Write(", ");
				}
				else
				{
					first = true;
				}

				Writer.Write(Temps[argument]);
			}
			Writer.WriteLine(")");

			Writer.Indent++;
			WriteBlocks();
			Writer.Indent--;

			Writer.WriteLine("end");
		}

		#endregion
	}
}

