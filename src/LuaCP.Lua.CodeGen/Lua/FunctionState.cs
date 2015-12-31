using System;
using LuaCP.IR.Components;
using System.Collections.Generic;
using LuaCP.IR;
using LuaCP.IR.Instructions;
using System.Linq;

namespace LuaCP.CodeGen.Lua
{
    public sealed class FunctionState
    {
        public readonly Function Function;
        public readonly IReadOnlyDictionary<Upvalue, String> Upvalues;

        public readonly NameAllocator<Phi> Phis;
        public readonly NameAllocator<IValue> Temps;
        public readonly NameAllocator<IValue> Refs;
        public readonly NameAllocator<Block> Blocks;
        public readonly NameAllocator<Function> FuncAllocator;

        public FunctionState(Function function, IReadOnlyDictionary<Upvalue, String> upvalues, NameAllocator<Function> funcAllocator)
        {
            Upvalues = upvalues;
            Function = function;
            FuncAllocator = funcAllocator;

            string prefix = funcAllocator[function];
            Phis = new NameAllocator<Phi>(prefix + "phi_{0}");
            Temps = new NameAllocator<IValue>(prefix + "temp_{0}");
            Refs = new NameAllocator<IValue>(prefix + "var_{0}");
            Blocks = new NameAllocator<Block>(prefix + "lbl_{0}");

            foreach (Argument argument in function.Arguments) Temps[argument] = prefix + argument.Name;
        }

        public FunctionState(Function function)
            : this(function, new Dictionary<Upvalue, String>(), new NameAllocator<Function>("f{0}_"))
        {
        }

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
    }
}

