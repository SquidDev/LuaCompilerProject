using System;

namespace LuaCP.IR.Instructions
{
    public enum Opcode : byte
    {
        // Unary operators
        Not,
        UnaryMinus,
        BNot,
        Length,

        // Binary operators
        Add,
        Subtract,
        Multiply,
        Divide,
        IntegerDivide,
        Power,
        Modulus,
        Concat,
        BAnd,
        BOr,
        BXor,
        LShift,
        RShift,

        // Comparison
        Equals,
        NotEquals,
        LessThan,
        LessThanEquals,

        // Branch
        Branch,
        BranchCondition,
        ValueCondition,
        Return,

        // Table access
        TableGet,
        TableSet,
        TableNew,

        // Methods and tuples
        Call,
        TupleNew,
        TupleGet,
        TupleRemainder,
		
        // Upvalue
        ReferenceGet,
        ReferenceSet,
        ReferenceNew,
        ClosureNew,
    }

    public static class OpcodeExtensions
    {
        public static bool IsUnaryOperator(this Opcode x)
        {
            return x >= Opcode.Not && x <= Opcode.Length;
        }

        public static bool IsBinaryOperator(this Opcode x)
        {
            return x >= Opcode.Add && x <= Opcode.LessThanEquals;
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
                    return ">";
                default:
                    throw new ArgumentException("Unexpected opcode " + x, "x");
            }
        }
    }
}
