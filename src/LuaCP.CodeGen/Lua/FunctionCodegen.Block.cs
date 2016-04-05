using System;
using System.Collections.Generic;
using System.Linq;
using LuaCP.Collections;
using LuaCP.IR;
using LuaCP.IR.Components;
using LuaCP.IR.Instructions;

namespace LuaCP.CodeGen.Lua
{
	public sealed partial class FunctionCodegen
	{
		private void WriteValue(ValueInstruction insn)
		{
			if (insn.Opcode.IsBinaryOperator())
			{
				BinaryOp op = (BinaryOp)insn;
				writer.WriteLine("{0} = {1} {2} {3}", GetName(insn), Format(op.Left), op.Opcode.GetSymbol(), Format(op.Right));
			}
			else if (insn.Opcode.IsUnaryOperator())
			{
				UnaryOp op = (UnaryOp)insn;
				writer.WriteLine("{0} = {1} {2}", GetName(insn), op.Opcode.GetSymbol(), Format(op.Operand));
			}
			else
			{
				switch (insn.Opcode)
				{
					case Opcode.ValueCondition:
						{
							ValueCondition valueCond = (ValueCondition)insn;

							writer.WriteLine(
								"if {1} then {0} = {2} else {0} = {3} end", 
								GetName(insn), 
								Format(valueCond.Test),
								Format(valueCond.Success),
								Format(valueCond.Failure)
							);
							break;
						}
					case Opcode.TableGet:
						{
							TableGet getter = (TableGet)insn;
							writer.WriteLine("{0} = {1}{2}", GetName(insn), Format(getter.Table), FormatKey(getter.Key));
							break;
						}
					case Opcode.TableNew:
						{
							TableNew tblNew = (TableNew)insn;
							writer.Write("{0} =", GetName(insn));
							writer.Write("{");
							foreach (IValue value in tblNew.ArrayPart)
							{
								writer.Write(Format(value));
								writer.Write(", ");
							}

							foreach (KeyValuePair<IValue, IValue> pair in tblNew.HashPart)
							{
								writer.Write(FormatKey(pair.Key, false));
								writer.Write(" = ");
								writer.Write(Format(pair.Value));
								writer.Write(", ");
							}

							writer.WriteLine("}");
							break;
						}
					case Opcode.ClosureNew:
						{
							ClosureNew closNew = (ClosureNew)insn;

							var lookup = new Dictionary<Upvalue, string>();
							foreach (Tuple<IValue, Upvalue> closed in Enumerable.Zip(closNew.ClosedUpvalues, closNew.Function.ClosedUpvalues, Tuple.Create))
							{
								string name = refs[closed.Item1];
								writer.Write("local {0} = {1}", name, Format(closed.Item1));
								lookup.Add(closed.Item2, name);
							}

							foreach (Tuple<IValue, Upvalue> open in Enumerable.Zip(closNew.OpenUpvalues, closNew.Function.OpenUpvalues, Tuple.Create))
							{
								lookup.Add(open.Item2, Format(open.Item1));
							}

							writer.Write("{0} = ", GetName(insn));
							new FunctionCodegen(closNew.Function, writer, lookup, funcAllocator).Write();
							break;
						}
					case Opcode.ReferenceGet:
						{
							writer.WriteLine("{0} = {1}", GetName(insn), Format(((ReferenceGet)insn).Reference));
							break;
						}
					case Opcode.ReferenceNew:
						{
							ReferenceNew refNew = (ReferenceNew)insn;
							writer.WriteLine("local {0} = {1}", refs[refNew], Format(refNew.Value));
							break;
						}
					default:
						throw new ArgumentException("Expected Value, got " + insn.Opcode);
				}
			}
		}

		private static ArgumentException TupleChain(IValue expected, IValue actual, String contents)
		{
			throw new ArgumentException("Invalid tuple chain. Expected " + expected + ", got " + actual + " (" + contents + ")");
		}

		private Instruction WriteTuples(Instruction insn)
		{
			Block block = insn.Block;
			IValue previous = block.Function.Module.Constants.Nil;
			String previousContents = "";

			while (true)
			{
				switch (insn.Opcode)
				{
					case Opcode.Call:
						{
							Call call = (Call)insn;
							if (!call.Arguments.Equals(previous)) throw TupleChain(call.Arguments, previous, previousContents);

							previous = call;
							previousContents = String.Format("{0}({1})", Format(call.Method), previousContents);
							break;
						}
					case Opcode.TupleNew:
						{
							TupleNew tupNew = (TupleNew)insn;
							if (!tupNew.Remaining.Equals(previous)) throw TupleChain(tupNew.Remaining, previous, previousContents);

							previous = tupNew;
							if (tupNew.Remaining.IsNil())
							{
								previousContents = String.Join(", ", tupNew.Values.Select(Format));
							}
							else
							{
								previousContents = String.Join(", ", tupNew.Values.Select(Format)) + ", " + previousContents;
							}
							break;
						}
					case Opcode.TupleRemainder:
						{
							TupleRemainder remainder = (TupleRemainder)insn;
							if (!remainder.Tuple.Equals(previous)) throw TupleChain(remainder.Tuple, previous, previousContents);

							previous = remainder;
							previousContents = String.Format("select({0}, {1}", remainder.Offset + 1, previousContents);
							break;
						}
					case Opcode.Return:
						{
							Return ret = (Return)insn;
							if (!ret.Values.Equals(previous)) throw TupleChain(ret.Values, previous, previousContents);

							writer.WriteLine("do return {0} end", previousContents);
							return insn;
						}
					case Opcode.TupleGet:
						{
							var locals = new List<TupleGet>();
							var mappings = new List<KeyValuePair<TupleGet, TupleGet>>();

							while (insn != null && insn.Opcode == Opcode.TupleGet)
							{
								TupleGet getter = (TupleGet)insn;
								if (!getter.Tuple.Equals(previous)) throw TupleChain(getter.Tuple, previous, previousContents);

								if (getter.Offset >= locals.Count) locals.Resize(getter.Offset + 1, null);
								TupleGet current = locals[getter.Offset];
								if (current == null)
								{
									locals[getter.Offset] = getter;
								}
								else
								{
									mappings.Add(new KeyValuePair<TupleGet, TupleGet>(getter, current));
								}

								insn = insn.Next;
							}

							writer.Write(String.Join(", ", locals.Select(x => x == null ? "_" : Format(x))));
							writer.Write(" = ");
							writer.Write(previousContents);
							writer.WriteLine(";");

							foreach (var mapping in mappings)
							{
								writer.WriteLine("{0} = {1}", Format(mapping.Key), Format(mapping.Value));
							}

							return insn == null ? block.Last : insn.Previous;
						}
					default:
						writer.Write(previousContents);
						writer.WriteLine(";");
						return insn.Previous;
				}

				insn = insn.Next;
			}
		}

		private void WriteBlock(Block block)
		{
			if (!visited.Add(block)) throw new ArgumentException("Already written block");

			if (block.Previous.Count() > 1) writer.WriteLine("::{0}::", blocks[block]);

			Instruction insn = block.First;
			while (insn != null)
			{
				var value = insn as ValueInstruction;
				if (value != null && value.Kind != ValueKind.Tuple)
				{
					WriteValue(value);
				}
				else
				{
					switch (insn.Opcode)
					{
						case Opcode.Branch:
							{
								Branch branch = (Branch)insn;
								WriteJump(block, branch.Target);
								break;
							}
						case Opcode.BranchCondition:
							{
								BranchCondition branchCond = (BranchCondition)insn;

								writer.WriteLine("if {0} then", Format(branchCond.Test));

								writer.Indent++;
								WriteJump(block, branchCond.Success);

								writer.Indent--;

								writer.WriteLine("else");

								writer.Indent++;
								WriteJump(block, branchCond.Failure);
								writer.Indent--;

								writer.WriteLine("end");
								break;
							}
						case Opcode.TableSet:
							{
								TableSet getter = (TableSet)insn;
								writer.WriteLine("{0}{1} = {2}", Format(getter.Table), FormatKey(getter.Key), Format(getter.Value));
								break;
							}
						case Opcode.ReferenceSet:
							{
								ReferenceSet setter = (ReferenceSet)insn;
								writer.WriteLine("{0} = {1}", Format(setter.Reference), Format(setter.Value));
								break;
							}
						case Opcode.Call:
						case Opcode.TupleGet:	
						case Opcode.TupleNew:	
						case Opcode.TupleRemainder:	
						case Opcode.Return:	
							insn = WriteTuples(insn);
							break;
						default:
							throw new ArgumentException("Unknown opcode " + insn.Opcode);
					}
				}

				insn = insn.Next;
			}
		}

		private void WriteJump(Block source, Block target)
		{
			foreach (Phi phi in target.PhiNodes)
			{
				string name = GetName(phi);
				string sourceName = Format(phi.Source[source]);
				if (name != sourceName) writer.WriteLine("{0} = {1}", name, sourceName);
			}

			if (target.Previous.Count() == 1)
			{
				WriteBlock(target);
			}
			else
			{
				writer.WriteLine("goto {0}", blocks[target]);
			}
		}
	}
}

