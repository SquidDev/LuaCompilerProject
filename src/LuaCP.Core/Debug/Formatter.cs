using System;
using System.Collections.Generic;
using System.IO;
using LuaCP.IR.Components;
using LuaCP.IR.Instructions;
using LuaCP.IR;
using LuaCP.Collections;
using System.Globalization;

namespace LuaCP.Debug
{
	public class Formatter
	{
		public static readonly Formatter Default = new Formatter();

		public string Phi(Phi phi, NodeNumberer numberer)
		{
			StringWriter writer = new StringWriter();
			Phi(phi, writer, numberer);
			return writer.ToString();
		}

		public void Phi(Phi phi, TextWriter writer, NodeNumberer numberer)
		{
			writer.Write(numberer.PrettyGetPhi(phi));
			writer.Write(":");
			foreach (KeyValuePair<Block, IValue> item in phi.Source)
			{
				writer.Write(" [");
				writer.Write(numberer.PrettyGetBlock(item.Key));
				writer.Write(" => ");
				Value(item.Value, writer, numberer);
				writer.Write("]");
			}
		}

		public void Unknown(object obj, TextWriter writer)
		{
			writer.Write("<unknown: ");
			writer.Write(obj.GetType().Name);
			writer.Write(">");
		}

		public string Value(IValue value, NodeNumberer numberer)
		{
			StringWriter writer = new StringWriter();
			Value(value, writer, numberer);
			return writer.ToString();
		}

		public void Value(IValue value, TextWriter writer, NodeNumberer numberer)
		{
			if (value == null)
			{
				writer.Write("<null>");
			}
			else if (value is Instruction)
			{
				Instruction insn = (Instruction)value;
				if (insn.Block == null) Console.WriteLine("Block is null for " + insn.Opcode);
				writer.Write(numberer.PrettyGetInstruction(insn));
			}
			else if (value is Constant)
			{
				writer.Write(value);
			}
			else if (value is Phi)
			{
				Phi phi = (Phi)value;
				writer.Write(numberer.PrettyGetPhi(phi));
			}
			else if (value is Upvalue)
			{
				writer.Write("!");
				Upvalue upvalue = (Upvalue)value;
				writer.Write(upvalue.Function.Upvalues.FindIndex(upvalue));
			}
			else if (value is Argument)
			{
				writer.Write("^{");
				writer.Write(((Argument)value).Name);
				writer.Write("}");
			}
			else
			{
				Unknown(value, writer);
			}
		}

		public string Block(Block block, NodeNumberer numberer)
		{
			return numberer.PrettyGetBlock(block);
		}

		public void Block(Block block, TextWriter writer, NodeNumberer numberer)
		{
			writer.Write(numberer.PrettyGetBlock(block));
		}

		public string InstructionLong(Instruction insn, NodeNumberer numberer)
		{
			StringWriter writer = new StringWriter();
			InstructionLong(insn, writer, numberer);
			return writer.ToString();
		}

		public void InstructionLong(Instruction insn, TextWriter writer, NodeNumberer numberer)
		{
			writer.Write(numberer.PrettyGetInstruction(insn));
			writer.Write(": ");

			Opcode opcode = insn.Opcode;
			writer.Write(opcode);
			writer.Write(" ");
			if (opcode.IsBinaryOperator())
			{
				BinaryOp op = (BinaryOp)insn;
				Value(op.Left, writer, numberer);
				writer.Write(" ");
				Value(op.Right, writer, numberer);
			}
			else if (opcode.IsUnaryOperator())
			{
				Value(((UnaryOp)insn).Operand, writer, numberer);
			}
			else
			{
				switch (opcode)
				{
					case Opcode.Branch:
						Block(((Branch)insn).Target, writer, numberer);
						break;
					case Opcode.BranchCondition:
						{
							BranchCondition condition = (BranchCondition)insn;
							Value(condition.Test, writer, numberer);
							writer.Write(" ? ");
							Block(condition.Success, writer, numberer);
							writer.Write(" : ");
							Block(condition.Failure, writer, numberer);
							break;
						}
					case Opcode.ValueCondition:
						{
							ValueCondition condition = (ValueCondition)insn;
							Value(condition.Test, writer, numberer);
							writer.Write(" ? ");
							Value(condition.Success, writer, numberer);
							writer.Write(" : ");
							Value(condition.Failure, writer, numberer);
							break;
						}
					case Opcode.Return:
						{
							Return ret = (Return)insn;
							Value(ret.Values, writer, numberer);
							break;
						}
					case Opcode.TableGet:
						{
							TableGet tGet = (TableGet)insn;
							Value(tGet.Table, writer, numberer);
							writer.Write("[");
							Value(tGet.Key, writer, numberer);
							writer.Write("]");
							break;
						}
					case Opcode.TableSet:
						{
							TableSet tSet = (TableSet)insn;
							Value(tSet.Table, writer, numberer);
							writer.Write("[");
							Value(tSet.Key, writer, numberer);
							writer.Write("] = ");
							Value(tSet.Value, writer, numberer);
							break;
						}
					case Opcode.TableNew:
						{
							TableNew tNew = (TableNew)insn;
							writer.Write("{[");
							writer.Write(tNew.AdditionalArray);
							writer.Write(", ");
							writer.Write(tNew.AdditionalHash);
							writer.Write("] ");

							foreach (IValue value in tNew.ArrayPart)
							{
								Value(value, writer, numberer);
								writer.Write(" ");
							}

							foreach (KeyValuePair<IValue, IValue> item in tNew.HashPart)
							{
								Value(item.Key, writer, numberer);
								writer.Write(" => ");
								Value(item.Value, writer, numberer);
								writer.Write(" ");
							}

							writer.Write("}");
							break;
						}
					case Opcode.Call:
						{
							Call call = (Call)insn;
							Value(call.Method, writer, numberer);
							writer.Write("(");
							Value(call.Arguments, writer, numberer);
							writer.Write(")");
							break;
						}
					case Opcode.TupleNew:
						{
							TupleNew creator = (TupleNew)insn;
							writer.Write("[");
							foreach (IValue value in creator.Values)
							{
								Value(value, writer, numberer);
								writer.Write(" ");
							}
							Value(creator.Remaining, writer, numberer);
							writer.Write("...]");
							break;
						}
					case Opcode.TupleGet:
						{
							TupleGet eGet = (TupleGet)insn;
							Value(eGet.Tuple, writer, numberer);
							writer.Write("{");
							writer.Write(eGet.Offset);
							writer.Write("}");
							break;
						}
					case Opcode.TupleRemainder:
						{
							TupleRemainder eGet = (TupleRemainder)insn;
							Value(eGet.Tuple, writer, numberer);
							writer.Write("{");
							writer.Write(eGet.Offset);
							writer.Write("}");
							break;
						}
					case Opcode.ReferenceGet:
						Value(((ReferenceGet)insn).Reference, writer, numberer);
						break;
					case Opcode.ReferenceSet:
						{
							ReferenceSet rSet = (ReferenceSet)insn;
							Value(rSet.Reference, writer, numberer);
							writer.Write(" := ");
							Value(rSet.Value, writer, numberer);
							break;
						}
					case Opcode.ReferenceNew:
						Value(((ReferenceNew)insn).Value, writer, numberer);
						break;
					case Opcode.ClosureNew:
						{
							ClosureNew closure = (ClosureNew)insn;
							writer.Write("Open[");
							foreach (IValue open in closure.OpenUpvalues)
							{
								Value(open, writer, numberer);
								writer.Write(", ");
							}

							writer.Write("] Closed[");
							foreach (IValue open in closure.ClosedUpvalues)
							{
								Value(open, writer, numberer);
								writer.Write(", ");
							}
							writer.Write("]");
							break;
						}
					default:
						writer.Write("<unknown instruction>");
						break;
				}
			}
		}
	}

	public class IRFormatProvider : IFormatProvider, ICustomFormatter
	{
		private readonly NodeNumberer numberer;

		public IRFormatProvider(NodeNumberer numberer)
		{
			this.numberer = numberer;
		}

		public object GetFormat(Type formatType)
		{
			return formatType == typeof(ICustomFormatter) ? this : null;
		}

		public string Format(string format, object arg, IFormatProvider formatProvider)
		{
			if (arg is IValue)
			{
				using (StringWriter writer = new StringWriter())
				{
					Formatter.Default.Value((IValue)arg, writer, numberer);
					return writer.ToString();
				}
			}
			if (arg is Block) return numberer.PrettyGetBlock((Block)arg);

			if (arg is IFormattable) return ((IFormattable)arg).ToString(format, CultureInfo.CurrentCulture);
			if (arg != null) return arg.ToString();

			return String.Empty;
		}
	}
}

