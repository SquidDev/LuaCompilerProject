using System;

namespace LuaCP.IR.Instructions
{
	public enum Opcode
	{
		// Unary operators
		Not = 0,
		UnaryMinus = 1,
		BNot = 2,
		Length = 3,

		// Binary operators
		Add = 4,
		Subtract = 5,
		Multiply = 6,
		Divide = 7,
		IntegerDivide = 8,
		Power = 9,
		Modulus = 10,
		Concat = 11,
		BAnd = 12,
		BOr = 13,
		BXor = 14,
		LShift = 15,
		RShift = 16,

		// Comparison
		Equals = 17,
		NotEquals = 18,
		LessThan = 19,
		LessThanEquals = 20,

		// Branch
		Branch = 21,
		BranchCondition = 22,
		Return = 23,
		ValueCondition = 24,

		// Table access
		TableGet = 25,
		TableSet = 26,
		TableNew = 27,

		// Methods and tuples
		Call = 28,
		TupleNew = 29,
		TupleGet = 30,
		TupleRemainder = 31,
		
		// Upvalue
		ReferenceGet = 32,
		ReferenceSet = 33,
		ReferenceNew = 34,
		ClosureNew = 35,
	}

	public static class OpcodeExtensions
	{
		public const int Size = 36;

		public static bool IsUnaryOperator(this Opcode x)
		{
			return x >= Opcode.Not && x <= Opcode.Length;
		}

		public static bool IsBinaryOperator(this Opcode x)
		{
			return x >= Opcode.Add && x <= Opcode.LessThanEquals;
		}

		public static bool IsComparisonOperator(this Opcode x)
		{
			return x >= Opcode.Equals && x <= Opcode.LessThanEquals;
		}

		public static bool IsTerminator(this Opcode x)
		{
			return x >= Opcode.Branch && x <= Opcode.Return;
		}

		public static bool IsReferenceInsn(this Opcode x)
		{
			// We exclude ClosureNew as that comsumes a reference rather than its value
			return x >= Opcode.ReferenceGet && x <= Opcode.ReferenceNew;
		}

		public static string GetSymbol(this Opcode x)
		{
			switch (x)
			{
				case Opcode.Not:
					return "not ";
				case Opcode.UnaryMinus:
					return "-";
				case Opcode.BNot:
					return "~";
				case Opcode.Length:
					return "#";
				case Opcode.Add:
					return "+";
				case Opcode.Subtract:
					return "-";
				case Opcode.Multiply:
					return "*";
				case Opcode.Divide:
					return "/";
				case Opcode.IntegerDivide:
					return "//";
				case Opcode.Power:
					return "^";
				case Opcode.Modulus:
					return "%";
				case Opcode.Concat:
					return "..";
				case Opcode.BAnd:
					return "&";
				case Opcode.BOr:
					return "|";
				case Opcode.BXor:
					return "~";
				case Opcode.LShift:
					return "<<";
				case Opcode.RShift:
					return ">>";
				case Opcode.Equals:
					return "==";
				case Opcode.NotEquals:
					return "~=";
				case Opcode.LessThan:
					return "<";
				case Opcode.LessThanEquals:
					return "<=";
				default:
					throw new ArgumentException("Unexpected opcode " + x, "x");
			}
		}
	}
}
