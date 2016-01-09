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
		private struct ExpressionValue
		{
			public readonly ValueInstruction Value;
			public readonly string Contents;

			public ExpressionValue(ValueInstruction value, string contents)
			{
				Value = value;
				Contents = contents;
			}
		}

		private readonly Stack<ExpressionValue> expressions = new Stack<ExpressionValue>();
		private readonly ISet<ValueInstruction> values = new HashSet<ValueInstruction>();

		private readonly Block block;
		private readonly FunctionCodegen state;
		private readonly IndentedTextWriter writer;

		public  BlockCodegen(Block block, FunctionCodegen state, IndentedTextWriter writer)
		{
			this.block = block;
			this.state = state;
			this.writer = writer;
		}

		#region Format/Expression Popping

		private string Format(IValue value)
		{
			ValueInstruction insn = value as ValueInstruction;
			if (insn != null && insn.Block == block && values.Contains(insn))
			{
				return PopUntil(insn);
			}

			return state.Format(value);
		}

		private string FormatTuple(IValue value, bool require = false)
		{
			ValueInstruction insn = value as ValueInstruction;
			if (insn != null && insn.Block == block && values.Contains(insn))
			{
				return PopUntil(insn);
			}

			if (value.IsNil()) return require ? "nil" : "";
			if (value.Kind != ValueKind.Tuple) return Format(value);

			TupleNew tuple = value as TupleNew;
			if (tuple != null && tuple.Remaining.IsNil())
			{
				string[] results = new string[tuple.Values.Count];
				for (int i = tuple.Values.Count - 1; i >= 0; i--)
				{
					results[i] = Format(tuple.Values[i]);
				}

				return String.Join(", ", results);
			}

			return state.Temps[value] + " --[[Probably incorrect tuple]]";
		}

		private string FormatKey(IValue value)
		{
			ValueInstruction insn = value as ValueInstruction;
			if (insn != null && insn.Block == block && values.Contains(insn))
			{
				return "[" + PopUntil(insn) + "]";
			}

			return state.FormatKey(value);
		}

		private void PopExpressions()
		{
			while (expressions.Count > 0)
			{
				ExpressionValue expr = expressions.Pop();
				values.Remove(expr.Value);
				writer.WriteLine("local {0} = {1}", state.Temps[expr.Value], expr.Contents);
			}
		}

		private string PopUntil(ValueInstruction value)
		{
			while (true)
			{
				ExpressionValue expr = expressions.Pop();
				values.Remove(expr.Value);

				if (expr.Value == value)
				{
					return expr.Contents;
				}
				else
				{
					string name = state.Temps[expr.Value];
					// Only write if it isn't declared
					if (name != expr.Contents)
					{
						writer.WriteLine("local {0} = {1}", name, expr.Contents);
					}
					else
					{
						writer.WriteLine("-- Writing {0}", name);
					}
				}
			}
		}

		#endregion

		private string WriteExpression(Instruction insn)
		{
			if (insn.Opcode.IsBinaryOperator())
			{
				BinaryOp op = (BinaryOp)insn;

				string right = Format(op.Right);
				string left = Format(op.Left);
				return String.Format("({0} {1} {2})", left, op.Opcode.GetSymbol(), right);
			}
			else if (insn.Opcode.IsUnaryOperator())
			{
				UnaryOp op = (UnaryOp)insn;
				return String.Format("({0}{1})", op.Opcode.GetSymbol(), Format(op.Operand));
			}
			else
			{
				switch (insn.Opcode)
				{
					case Opcode.ValueCondition:
						{
							ValueCondition valueCond = (ValueCondition)insn;
							string name = state.Temps[valueCond];

							writer.WriteLine(
								"local {0} if {3} then {0} = {2} else {0} = {1} end", 
								name, 
								Format(valueCond.Failure),
								Format(valueCond.Success),
								Format(valueCond.Test)
							);
							return name;
						}
					case Opcode.TableGet:
						{
							TableGet getter = (TableGet)insn;
							return String.Format("{1}{0}", FormatKey(getter.Key), Format(getter.Table));
						}
					case Opcode.TableNew:
						{
							TableNew tblNew = (TableNew)insn;
							StringBuilder builder = new StringBuilder();
							builder.Append("{");
							foreach (IValue value in tblNew.ArrayPart)
							{
								builder.Append(Format(value));
								builder.Append(", ");
							}

							foreach (KeyValuePair<IValue, IValue> pair in tblNew.hashPart)
							{
								string value = Format(pair.Value);
								builder.Append(FormatKey(pair.Key));
								builder.Append(" = ");
								builder.Append(value);
								builder.Append(", ");
							}

							builder.Append("}");
							return builder.ToString();
						}
					case Opcode.Call:
						{
							Call call = (Call)insn;
							return String.Format("{1}({0})", FormatTuple(call.Arguments), Format(call.Method));
						}
					case Opcode.TupleNew:
						{
							TupleNew tupNew = (TupleNew)insn;
							return FormatTuple(tupNew);
						}
					case Opcode.TupleGet:
						{
							TupleGet getter = (TupleGet)insn;
							if (getter.Offset == 0)
							{
								return "(" + FormatTuple(getter.Tuple, true) + ")";
							}
							else
							{
								string name = state.Temps[getter];
								writer.WriteLine("local {2} {0} = {1}", name, FormatTuple(getter.Tuple, true), String.Concat(Enumerable.Repeat("_, ", getter.Offset)));
								return name;
							}
						}
					case Opcode.TupleRemainder:
						{
							TupleRemainder getter = (TupleRemainder)insn;
							return String.Format("--[[NYI: Tuple Remainder {0}]] nil", state.Temps[getter]);
						}
					case Opcode.ReferenceGet:
						{
							ReferenceGet getter = (ReferenceGet)insn;
							return Format(getter.Reference);
						}
					case Opcode.ReferenceNew:
						{
							ReferenceNew refNew = (ReferenceNew)insn;
							string name = state.Refs[refNew];
							state.Temps[refNew] = name;
							writer.WriteLine("local {0} = {1}", name, Format(refNew.Value));
							return name;
						}
					case Opcode.ClosureNew:
						{
							ClosureNew closNew = (ClosureNew)insn;
							string name = state.Temps[closNew];
							writer.Write("local {0} = ", name);
							Dictionary<Upvalue, String> lookup = new Dictionary<Upvalue, string>();
							foreach (Tuple<IValue, Upvalue> closed in Enumerable.Zip(closNew.ClosedUpvalues, closNew.Function.ClosedUpvalues, Tuple.Create))
							{
								lookup.Add(closed.Item2, Format(closed.Item1));
							}
							foreach (Tuple<IValue, Upvalue> open in Enumerable.Zip(closNew.OpenUpvalues, closNew.Function.OpenUpvalues, Tuple.Create))
							{
								lookup.Add(open.Item2, Format(open.Item1));
							}
							new FunctionCodegen(closNew.Function, writer, lookup, state.FuncAllocator).Write();
							return name;
						}
					default:
						throw new ArgumentException("Expected Value, got " + insn.Opcode);
				}
			}
		}

		public void Write()
		{
			if (block.Previous.Count() > 1) writer.WriteLine("::{0}::", state.Blocks[block]);

			foreach (Phi phi in block.DominatorTreeChildren.SelectMany(x => x.PhiNodes))
			{
				string name = state.Phis[phi];
				writer.WriteLine("local " + name);
			}

			foreach (Instruction insn in block)
			{
				var value = insn as ValueInstruction;
				if (value != null)
				{
					string contents = WriteExpression(insn);

					expressions.Push(new ExpressionValue(value, contents));
					values.Add(value);

					if (value.Users.TotalCount == 1)
					{
						Instruction user = value.Users.First<IUser<IValue>>() as Instruction;
						if (user == null || user.Block != block)
						{
							PopExpressions();
						}
					}
					else
					{
						PopExpressions();
					}
				}
				else
				{
					PopExpressions();

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

								writer.WriteLine("if {0} then", Format(branchCond.Test));

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
								writer.WriteLine("do return {0} end", FormatTuple(ret.Values));
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
						default:
							throw new ArgumentException("Unknown opcode " + insn.Opcode);
					}
				}
			}

			PopExpressions();
		}

		private void WriteJump(Block target)
		{
			foreach (Phi phi in target.PhiNodes)
			{
				writer.WriteLine("{0} = {1}", state.Phis[phi], Format(phi.Source[block]));
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

