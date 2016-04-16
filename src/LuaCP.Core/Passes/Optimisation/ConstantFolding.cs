using System;
using LuaCP.IR;
using LuaCP.IR.Instructions;

namespace LuaCP.Passes.Optimisation
{
	public static class ConstantFolding
	{
		public static Pass<Instruction> Runner { get { return Run; } }

		private static bool Run(PassManager data, Instruction insn)
		{
			if (insn.Opcode.IsBinaryOperator())
			{
				BinaryOp op = (BinaryOp)insn;
				if (op.Left is Constant && op.Right is Constant)
				{
					op.ReplaceWithAndRemove(insn.Block.Function.Module.Constants[BinaryOp(op.Opcode, ((Constant)op.Left).Literal, ((Constant)op.Right).Literal)]);
					return false;
				}
			}
			else if (insn.Opcode.IsUnaryOperator())
			{
				UnaryOp op = (UnaryOp)insn;
				if (op.Operand is Constant)
				{
					op.ReplaceWithAndRemove(insn.Block.Function.Module.Constants[UnaryOp(op.Opcode, ((Constant)op.Operand).Literal)]);
					return true;
				}
			}

			return false;
		}

		public static Literal UnaryOp(Opcode opcode, Literal operand)
		{
			switch (opcode)
			{
				case Opcode.Not:
					return operand.IsTruthy() ? Literal.False : Literal.True;
				case Opcode.UnaryMinus:
					switch (operand.Kind)
					{
						case LiteralKind.Number:
							{
								return new Literal.Number(-(double)operand);
							}
						case LiteralKind.Integer:
							{
								return new Literal.Integer(-(int)operand);
							}
						default:
							throw Expected(opcode, LiteralKind.Number, operand.Kind);
					}
				case Opcode.BNot:
					switch (operand.Kind)
					{
						case LiteralKind.Integer:
							{
								return new Literal.Integer(~(int)operand);
							}
						default:
							throw Expected(opcode, LiteralKind.Integer, operand.Kind);
					}
				case Opcode.Length:
					switch (operand.Kind)
					{
						case LiteralKind.String:
							{
								return new Literal.Integer(((string)operand).Length);
							}
						default:
							throw Expected(opcode, LiteralKind.String, operand.Kind);
					}
				default:
					throw new Exception("Unexpected opcode " + opcode);
			}
		}

		private static int ModulusI(int x, int y)
		{
			int val = x % y;
			if (val < 0) val += y;
			return val;
		}

		private static double ModulusD(double x, double y)
		{
			double val = x % y;
			if (val < 0) val += y;
			return val;
		}

		public static Literal BinaryOp(Opcode opcode, Literal left, Literal right)
		{
			switch (opcode)
			{
				case Opcode.Add:
					return NumericOperator(opcode, left, right, (x, y) => x + y, (x, y) => x + y);
				case Opcode.Subtract:
					return NumericOperator(opcode, left, right, (x, y) => x - y, (x, y) => x - y);
				case Opcode.Multiply:
					return NumericOperator(opcode, left, right, (x, y) => x * y, (x, y) => x * y);
				case Opcode.Divide:
					return NumericOperator(opcode, left, right, (x, y) => x / y, (x, y) => x / y);
				case Opcode.IntegerDivide:
					return NumericOperator(opcode, left, right, (x, y) => x / y, (x, y) => (int)x / (int)y);
				case Opcode.Power:
					return NumericOperator(opcode, left, right, (x, y) => (int)Math.Pow(x, y), Math.Pow);
				case Opcode.Modulus:
					return NumericOperator(opcode, left, right, ModulusI, ModulusD);
				case Opcode.BAnd:
					return IntegerOperator(opcode, left, right, (x, y) => x & y);
				case Opcode.BOr:
					return IntegerOperator(opcode, left, right, (x, y) => x | y);
				case Opcode.LShift:
					return IntegerOperator(opcode, left, right, (x, y) => x << y);
				case Opcode.RShift:
					return IntegerOperator(opcode, left, right, (x, y) => x >> y);
				case Opcode.BXor:
					return IntegerOperator(opcode, left, right, (x, y) => x ^ y);
				case Opcode.Concat:
					if (left.Kind != LiteralKind.String) throw Expected(opcode, LiteralKind.String, left.Kind);
					if (right.Kind != LiteralKind.String) throw Expected(opcode, LiteralKind.String, right.Kind);
					return new Literal.String((string)left + (string)right);
				case Opcode.Equals:
					if (left.Kind != right.Kind)
					{
						return Literal.False;
					}
					else
					{
						switch (left.Kind)
						{
							case LiteralKind.Integer:
								return (int)left == (int)right ? Literal.True : Literal.False;
							case LiteralKind.Number:
								return (double)left == (double)right ? Literal.True : Literal.False;
							case LiteralKind.String:
								return (string)left == (string)right ? Literal.True : Literal.False;
							case LiteralKind.Boolean:
								return (bool)left == (bool)right ? Literal.True : Literal.False;
							case LiteralKind.Nil:
								return true;
							default:
								throw new InvalidOperationException("Unknown kind " + left.Kind);
						}
					}
				case Opcode.NotEquals:
					if (left.Kind != right.Kind)
					{
						return Literal.True;
					}
					else
					{
						switch (left.Kind)
						{
							case LiteralKind.Integer:
								return (int)left != (int)right ? Literal.True : Literal.False;
							case LiteralKind.Number:
								return (double)left != (double)right ? Literal.True : Literal.False;
							case LiteralKind.String:
								return (string)left != (string)right ? Literal.True : Literal.False;
							case LiteralKind.Boolean:
								return (bool)left != (bool)right ? Literal.True : Literal.False;
							case LiteralKind.Nil:
								return false;
							default:
								throw new InvalidOperationException("Unknown kind " + left.Kind);
						}
					}
				case Opcode.LessThan:
					if (left.Kind != right.Kind)
					{
						return Literal.False;
					}
					else
					{
						switch (left.Kind)
						{
							case LiteralKind.Integer:
								return (int)left < (int)right ? Literal.True : Literal.False;
							case LiteralKind.Number:
								return (double)left < (double)right ? Literal.True : Literal.False;
							case LiteralKind.String:
								return String.Compare(((string)left), (string)right, StringComparison.InvariantCulture) < 0 ? Literal.True : Literal.False;
							case LiteralKind.Boolean:
								throw new InvalidOperationException("Cannot compare boolean values");
							case LiteralKind.Nil:
								throw new InvalidOperationException("Cannot compare nil values");
							default:
								throw new InvalidOperationException("Unknown kind " + left.Kind);
						}
					}
				case Opcode.LessThanEquals:
					if (left.Kind != right.Kind)
					{
						return Literal.False;
					}
					else
					{
						switch (left.Kind)
						{
							case LiteralKind.Integer:
								return (int)left <= (int)right ? Literal.True : Literal.False;
							case LiteralKind.Number:
								return (double)left <= (double)right ? Literal.True : Literal.False;
							case LiteralKind.String:
								return String.Compare(((string)left), (string)right, StringComparison.InvariantCulture) <= 0 ? Literal.True : Literal.False;
							case LiteralKind.Boolean:
								throw new InvalidOperationException("Cannot compare boolean values");
							case LiteralKind.Nil:
								throw new InvalidOperationException("Cannot compare nil values");
							default:
								throw new InvalidOperationException("Unknown kind " + left.Kind);
						}
					}
				default:
					throw new Exception("Unexpected Opcode " + opcode);
			}


		}

		private static Literal NumericOperator(Opcode opcode, Literal left, Literal right, Func<int, int, int> opInt, Func<double, double, double> opDouble)
		{
			switch (left.Kind)
			{
				case LiteralKind.Integer:
					{
						switch (right.Kind)
						{
							case LiteralKind.Integer:
								return new Literal.Integer(opInt((int)left, (int)right));
							case LiteralKind.Number:
								return new Literal.Number(opDouble((int)left, (double)right));
							default:
								throw Expected(opcode, LiteralKind.Number, right.Kind);
						}
					}
				case LiteralKind.Number:
					if (!right.IsNumeric()) throw Expected(opcode, LiteralKind.Number, right.Kind);
					return new Literal.Number(opDouble((double)left, (double)right));
				default:
					throw Expected(opcode, LiteralKind.Number, left.Kind);
			}
		}

		private static Literal IntegerOperator(Opcode op, Literal left, Literal right, Func<int, int, int> opInt)
		{
			if (left.Kind != LiteralKind.Integer) throw Expected(op, LiteralKind.Integer, left.Kind);
			if (right.Kind != LiteralKind.Integer) throw Expected(op, LiteralKind.Integer, right.Kind);

			return new Literal.Integer(opInt((int)left, (int)right));
		}

		private static Exception Expected(Opcode opcode, LiteralKind expected, LiteralKind got)
		{
			return new InvalidOperationException(String.Format("[{0}]: Expected {1}, got {2}", opcode, expected, got));
		}
	}
}

