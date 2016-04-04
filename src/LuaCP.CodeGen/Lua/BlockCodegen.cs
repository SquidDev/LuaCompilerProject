using System;
using System.Collections.Generic;
using LuaCP.IR;
using LuaCP.IR.Components;
using System.Linq;
using LuaCP.IR.Instructions;
using System.Text;
using LuaCP.IR.User;

namespace LuaCP.CodeGen.Lua
{
	public class BlockCodegen
	{
		private readonly Block block;
		private readonly FunctionCodegen state;
		private readonly IndentedTextWriter writer;

		public BlockCodegen(Block block, FunctionCodegen state, IndentedTextWriter writer)
		{
			this.block = block;
			this.state = state;
			this.writer = writer;
		}

		private void WriteValue(ValueInstruction insn)
		{
			if (insn.Opcode.IsBinaryOperator())
			{
				BinaryOp op = (BinaryOp)insn;
				writer.WriteLine("{0} = {1} {2} {3}", state.Format(insn), state.Format(op.Left), op.Opcode.GetSymbol(), state.Format(op.Right));
			}
			else if (insn.Opcode.IsUnaryOperator())
			{
				UnaryOp op = (UnaryOp)insn;
				writer.WriteLine("{0} = {1} {2}", state.Format(insn), op.Opcode.GetSymbol(), state.Format(op.Operand));
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
								state.Format(insn), 
								state.Format(valueCond.Test),
								state.Format(valueCond.Success),
								state.Format(valueCond.Failure)
							);
							break;
						}
					case Opcode.TableGet:
						{
							TableGet getter = (TableGet)insn;
							writer.WriteLine("{0} = {1}{2}", state.Format(insn), state.Format(getter.Table), state.FormatKey(getter.Key));
							break;
						}
					case Opcode.TableNew:
						{
							TableNew tblNew = (TableNew)insn;
							writer.Write("{0} =", state.Format(insn));
							writer.Write("{");
							foreach (IValue value in tblNew.ArrayPart)
							{
								writer.Write(state.Format(value));
								writer.Write(", ");
							}

							foreach (KeyValuePair<IValue, IValue> pair in tblNew.HashPart)
							{
								writer.Write(state.FormatKey(pair.Key, false));
								writer.Write(" = ");
								writer.Write(state.Format(pair.Value));
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
								string name = state.Refs[closed.Item1];
								writer.Write("local {0} = {1}", name, state.Format(closed.Item1));
								lookup.Add(closed.Item2, name);
							}

							foreach (Tuple<IValue, Upvalue> open in Enumerable.Zip(closNew.OpenUpvalues, closNew.Function.OpenUpvalues, Tuple.Create))
							{
								lookup.Add(open.Item2, state.Format(open.Item1));
							}

							writer.Write("{0} = ", state.Format(insn));
							new FunctionCodegen(closNew.Function, writer, lookup, state.FuncAllocator).Write();
							break;
						}
					case Opcode.ReferenceGet:
						{
							writer.WriteLine("{0} = {1}", state.Format(insn), state.Format(((ReferenceGet)insn).Reference));
							break;
						}
					case Opcode.ReferenceNew:
						{
							ReferenceNew refNew = (ReferenceNew)insn;
							writer.WriteLine("local {0} = {1}", state.Refs[refNew], state.Format(refNew.Value));
							break;
						}
					default:
						throw new ArgumentException("Expected Value, got " + insn.Opcode);
				}
			}
		}

		private String WriteTuple(ValueInstruction insn, IValue previous, String previousContents)
		{
			return "";
//			switch (insn.Opcode)
//			{
//				case Opcode.Call:
//					{
//						Call call = (Call)insn;
//						if (call.Arguments != previous) throw new ArgumentException("Invalid tuple chain");
//						return String.Format("{0}({1})", state.Format(call.Method), previousContents);
//					}
//				case Opcode.TupleNew:
//					{
//						TupleNew tupNew = (TupleNew)insn;
//						return FormatTuple(tupNew);
//					}
//				case Opcode.TupleRemainder:
//					{
//						TupleRemainder getter = (TupleRemainder)insn;
//						return String.Format("--[[NYI: Tuple Remainder {0}]] nil", state.Temps[getter]);
//					}
//				default:
//					throw new ArgumentException("Expected Value, got " + insn.Opcode);
//			}
		}

		public void Write()
		{
			if (block.Previous.Count() > 1) writer.WriteLine("::{0}::", state.Blocks[block]);

			foreach (Instruction insn in block)
			{
				var value = insn as ValueInstruction;
				if (value != null)
				{
					if (value.Kind != ValueKind.Tuple) WriteValue(value);
				}
				else
				{
					switch (insn.Opcode)
					{
						case Opcode.Branch:
							{
								Branch branch = (Branch)insn;
								WriteJump(branch.Target);
								break;
							}
						case Opcode.BranchCondition:
							{
								BranchCondition branchCond = (BranchCondition)insn;

								writer.WriteLine("if {0} then", state.Format(branchCond.Test));

								writer.Indent++;
								WriteJump(branchCond.Success);

								writer.Indent--;

								writer.WriteLine("else");

								writer.Indent++;
								WriteJump(branchCond.Failure);
								writer.Indent--;

								writer.WriteLine("end");
								break;
							}
						case Opcode.Return:
							{
								Return ret = (Return)insn;
								writer.WriteLine("do return {0} end", ""); // FormatTuple(ret.Values)
								break;
							}
						case Opcode.TableSet:
							{
								TableSet getter = (TableSet)insn;
								writer.WriteLine("{0}{1} = {2}", state.Format(getter.Table), state.FormatKey(getter.Key), state.Format(getter.Value));
								break;
							}
						case Opcode.ReferenceSet:
							{
								ReferenceSet setter = (ReferenceSet)insn;
								writer.WriteLine("{0} = {1}", state.Format(setter.Reference), state.Format(setter.Value));
								break;
							}
						default:
							throw new ArgumentException("Unknown opcode " + insn.Opcode);
					}
				}
			}
		}

		private void WriteJump(Block target)
		{
			foreach (Phi phi in target.PhiNodes)
			{
				writer.WriteLine("{0} = {1}", state.Phis[phi], state.Format(phi.Source[block]));
			}

			if (target.Previous.Count() == 1)
			{
				state.WriteBlock(target);
			}
			else
			{
				writer.WriteLine("goto {0}", state.Blocks[target]);
			}
		}
	}
}

