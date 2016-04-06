using System;
using System.Collections.Generic;

namespace LuaCP.IR.Instructions
{
	public class InstructionComparer : IEqualityComparer<Instruction>
	{
		private static InstructionComparer instance;

		public static InstructionComparer Instance { get { return instance ?? (instance = new InstructionComparer()); } }

		public bool Equals(Instruction a, Instruction b)
		{
			if (a.Opcode != b.Opcode) return false;
			if (a.Opcode.IsBinaryOperator())
			{
				var aOp = (BinaryOp)a;
				var bOp = (BinaryOp)b;
				return aOp.Left == bOp.Left && aOp.Right == bOp.Right;
			}
			else if (a.Opcode.IsUnaryOperator())
			{
				var aOp = (UnaryOp)a;
				var bOp = (UnaryOp)b;
				return aOp.Operand == bOp.Operand;
			}
			else
			{
				switch (a.Opcode)
				{
					case Opcode.Branch:
						{
							var aInsn = (Branch)a;
							var bInsn = (Branch)b;
							return aInsn.Target == bInsn.Target;
						}
					case Opcode.BranchCondition:
						{
							var aInsn = (BranchCondition)a;
							var bInsn = (BranchCondition)b;
							return aInsn.Test == bInsn.Test && aInsn.Success == bInsn.Success && aInsn.Failure == bInsn.Failure;
						}
					case Opcode.Return:
						{
							var aInsn = (Return)a;
							var bInsn = (Return)b;
							return aInsn.Values == bInsn.Values;
						}
					case Opcode.ValueCondition:
						{
							var aInsn = (ValueCondition)a;
							var bInsn = (ValueCondition)b;
							return aInsn.Test == bInsn.Test && aInsn.Success == bInsn.Success && aInsn.Failure == bInsn.Failure;
						}
					case Opcode.TableGet:
						{
							var aInsn = (TableGet)a;
							var bInsn = (TableGet)b;
							return aInsn.Table == bInsn.Table && aInsn.Key == bInsn.Key;
						}
					case Opcode.TableSet:
						{
							var aInsn = (TableSet)a;
							var bInsn = (TableSet)b;
							return aInsn.Table == bInsn.Table && aInsn.Key == bInsn.Key && aInsn.Value == bInsn.Value;
						}
					case Opcode.TableNew:
						{
							var aInsn = (TableNew)a;
							var bInsn = (TableNew)b;
							return aInsn.ArrayPart == bInsn.ArrayPart && aInsn.HashPart == bInsn.HashPart;
						}
					case Opcode.Call:
						{
							var aInsn = (Call)a;
							var bInsn = (Call)b;
							return aInsn.Method == bInsn.Method && aInsn.Arguments == bInsn.Arguments;
						}
					case Opcode.TupleNew:
						{
							var aInsn = (TupleNew)a;
							var bInsn = (TupleNew)b;
							return aInsn.Values == bInsn.Values && aInsn.Remaining == bInsn.Remaining;
						}
					case Opcode.TupleGet:
						{
							var aInsn = (TupleGet)a;
							var bInsn = (TupleGet)b;
							return aInsn.Tuple == bInsn.Tuple && aInsn.Offset == bInsn.Offset;
						}
					case Opcode.TupleRemainder:
						{
							var aInsn = (TupleRemainder)a;
							var bInsn = (TupleRemainder)b;
							return aInsn.Tuple == bInsn.Tuple && aInsn.Offset == bInsn.Offset;
						}

					case Opcode.ReferenceGet:
						{
							var aInsn = (ReferenceGet)a;
							var bInsn = (ReferenceGet)b;
							return aInsn.Reference == bInsn.Reference;
						}
					case Opcode.ReferenceSet:
						{
							var aInsn = (ReferenceSet)a;
							var bInsn = (ReferenceSet)b;
							return aInsn.Reference == bInsn.Reference && aInsn.Value == bInsn.Value;
						}
					case Opcode.ReferenceNew:
						{
							var aInsn = (ReferenceNew)a;
							var bInsn = (ReferenceNew)b;
							return aInsn.Value == bInsn.Value;
						}
					case Opcode.ClosureNew:
						{
							var aInsn = (ClosureNew)a;
							var bInsn = (ClosureNew)b;
							return aInsn.Function == bInsn.Function && aInsn.ClosedUpvalues == bInsn.ClosedUpvalues && aInsn.OpenUpvalues == bInsn.OpenUpvalues;
						}
					default:
						throw new InvalidOperationException("Unknown opcode " + a.Opcode);
				}
			}
		}

		public int GetHashCode(Instruction obj)
		{
			if (obj == null) return 0;

			int hash = (int)obj.Opcode;
			if (obj.Opcode.IsBinaryOperator())
			{
				var aOp = (BinaryOp)obj;
				hash = hash * 31 + aOp.Left.GetHashCode();
				hash = hash * 31 + aOp.Right.GetHashCode();
			}
			else if (obj.Opcode.IsUnaryOperator())
			{
				var aOp = (UnaryOp)obj;
				hash = hash * 31 + aOp.Operand.GetHashCode();
			}
			else
			{
				switch (obj.Opcode)
				{
					case Opcode.Branch:
						{
							var insn = (Branch)obj;
							hash = hash * 31 + insn.Target.GetHashCode();
							break;
						}
					case Opcode.BranchCondition:
						{
							var insn = (BranchCondition)obj;
							hash = hash * 31 + insn.Test.GetHashCode();
							hash = hash * 31 + insn.Success.GetHashCode();
							hash = hash * 31 + insn.Failure.GetHashCode();
							break;
						}
					case Opcode.Return:
						{
							var insn = (Return)obj;
							hash = hash * 31 + insn.Values.GetHashCode();
							break;
						}
					case Opcode.ValueCondition:
						{
							var insn = (ValueCondition)obj;
							hash = hash * 31 + insn.Test.GetHashCode();
							hash = hash * 31 + insn.Success.GetHashCode();
							hash = hash * 31 + insn.Failure.GetHashCode();
							break;
						}
					case Opcode.TableGet:
						{
							var insn = (TableGet)obj;
							hash = hash * 31 + insn.Table.GetHashCode();
							hash = hash * 31 + insn.Key.GetHashCode();
							break;
						}
					case Opcode.TableSet:
						{
							var insn = (TableSet)obj;
							hash = hash * 31 + insn.Table.GetHashCode();
							hash = hash * 31 + insn.Key.GetHashCode();
							hash = hash * 31 + insn.Value.GetHashCode();
							break;
						}
					case Opcode.TableNew:
						{
							var insn = (TableNew)obj;
							hash = hash * 31 + insn.ArrayPart.GetHashCode();
							hash = hash * 31 + insn.HashPart.GetHashCode();
							break;
						}
					case Opcode.Call:
						{
							var insn = (Call)obj;
							hash = hash * 31 + insn.Method.GetHashCode();
							hash = hash * 31 + insn.Arguments.GetHashCode();
							break;
						}
					case Opcode.TupleNew:
						{
							var insn = (TupleNew)obj;
							hash = hash * 31 + insn.Values.GetHashCode();
							hash = hash * 31 + insn.Remaining.GetHashCode();
							break;
						}
					case Opcode.TupleGet:
						{
							var insn = (TupleGet)obj;
							hash = hash * 31 + insn.Tuple.GetHashCode();
							hash = hash * 31 + insn.Offset.GetHashCode();
							break;
						}
					case Opcode.TupleRemainder:
						{
							var insn = (TupleRemainder)obj;
							hash = hash * 31 + insn.Tuple.GetHashCode();
							hash = hash * 31 + insn.Offset.GetHashCode();
							break;
						}

					case Opcode.ReferenceGet:
						{
							var insn = (ReferenceGet)obj;
							hash = hash * 31 + insn.Reference.GetHashCode();
							break;
						}
					case Opcode.ReferenceSet:
						{
							var insn = (ReferenceSet)obj;
							hash = hash * 31 + insn.Reference.GetHashCode();
							hash = hash * 31 + insn.Reference.GetHashCode();
							break;
						}
					case Opcode.ReferenceNew:
						{
							var insn = (ReferenceNew)obj;
							hash = hash * 31 + insn.Value.GetHashCode();
							break;
						}
					case Opcode.ClosureNew:
						{
							var insn = (ClosureNew)obj;
							hash = hash * 31 + insn.Function.GetHashCode();
							hash = hash * 31 + insn.ClosedUpvalues.GetHashCode();
							hash = hash * 31 + insn.OpenUpvalues.GetHashCode();
							break;
						}
					default:
						throw new InvalidOperationException("Unknown opcode " + obj.Opcode);
				}
			}

			return hash;
		}
	}
}

