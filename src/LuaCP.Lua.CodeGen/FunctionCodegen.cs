using System;
using System.Collections.Generic;
using LuaCP.IR.Components;
using LuaCP.IR.Instructions;
using LuaCP.IR;
using System.Linq;
using System.IO;
using LuaCP.Graph;

namespace LuaCP.Lua.CodeGen
{
    public sealed class FunctionCodegen
    {
        private readonly Function function;
        private readonly IReadOnlyDictionary<Upvalue, String> upvalues;

        private readonly NameAllocator<Phi> phis;
        private readonly NameAllocator<IValue> temps;
        private readonly NameAllocator<IValue> refs;
        private readonly NameAllocator<Block> blocks;
        private readonly NameAllocator<Function> funcAllocator;

        public FunctionCodegen(Function function, IReadOnlyDictionary<Upvalue, String> upvalues, NameAllocator<Function> funcAllocator)
        {
            this.upvalues = upvalues;
            this.function = function;
            function.Dominators.Evaluate();

            string prefix = funcAllocator[function];
            this.funcAllocator = funcAllocator;
            phis = new NameAllocator<Phi>(prefix + "_phi_");
            temps = new NameAllocator<IValue>(prefix + "_temp_");
            refs = new NameAllocator<IValue>(prefix + "_var_");
            blocks = new NameAllocator<Block>(prefix + "_lbl_");

            foreach (Argument argument in function.Arguments) temps[argument] = argument.Name;
        }

        public FunctionCodegen(Function function)
            : this(function, new Dictionary<Upvalue, String>(), new NameAllocator<Function>("f_"))
        {
        }

        private void WriteBlock(Block block, TextWriter writer)
        {
            if (!block.Previous.IsEmpty()) writer.WriteLine("::{0}::", blocks[block]);

            foreach (Phi phi in block.DominatorTreeChildren.SelectMany(x => x.PhiNodes))
            {
                string name = phis[phi];
                writer.WriteLine("local " + name);
            }
                
            foreach (Instruction insn in block)
            {
                if (insn.Opcode.IsBinaryOperator())
                {
                    BinaryOp op = (BinaryOp)insn;
                    writer.WriteLine("local {0} = {1} {2} {3}", temps[op], Format(op.Left), op.Opcode.GetSymbol(), Format(op.Right));
                }
                else if (insn.Opcode.IsUnaryOperator())
                {
                    UnaryOp op = (UnaryOp)insn;
                    writer.WriteLine("local {0} = {1} {2}", temps[op], op.Opcode.GetSymbol(), Format(op.Operand));
                }
                else
                {
                    switch (insn.Opcode)
                    {
                        case Opcode.Branch:
                            {
                                Branch branch = (Branch)insn;
                                WriteJump(block, branch.Target, writer);
                                break;
                            }
                        case Opcode.BranchCondition:
                            {
                                BranchCondition branchCond = (BranchCondition)insn;
                                writer.WriteLine("if {0} then", Format(branchCond.Test));
                                WriteJump(block, branchCond.Success, writer);
                                writer.WriteLine("else");
                                WriteJump(block, branchCond.Failure, writer);
                                writer.WriteLine("end");
                                break;
                            }
                        case Opcode.ValueCondition:
                            {
                                ValueCondition valueCond = (ValueCondition)insn;
                                writer.WriteLine("local {0} if {1} then {0} = {2} else {0} = {3} end", temps[valueCond], Format(valueCond.Test), Format(valueCond.Success), Format(valueCond.Failure));
                                break;
                            }
                        case Opcode.Return:
                            {
                                Return ret = (Return)insn;
                                writer.WriteLine("do return {0} end", FormatTuple(ret.Values));
                                break;
                            }
                        case Opcode.TableGet:
                            {
                                TableGet getter = (TableGet)insn;
                                writer.WriteLine("local {0} = {1}{2}", temps[getter], Format(getter.Table), FormatKey(getter.Key));
                                break;
                            }
                        case Opcode.TableSet:
                            {
                                TableSet getter = (TableSet)insn;
                                writer.WriteLine("{0}{1} = {2}", Format(getter.Table), FormatKey(getter.Key), Format(getter.Value));
                                break;
                            }
                        case Opcode.TableNew:
                            {
                                TableNew tblNew = (TableNew)insn;
                                writer.Write("local {0} = {{", temps[tblNew]);
                                foreach (IValue value in tblNew.ArrayPart)
                                {
                                    writer.Write(Format(value));
                                    writer.Write(", ");
                                }

                                foreach (KeyValuePair<IValue, IValue> pair in tblNew.hashPart)
                                {
                                    writer.Write(FormatKey(pair.Key));
                                    writer.Write(" = ");
                                    writer.Write(Format(pair.Value));
                                    writer.Write(", ");
                                }

                                writer.Write("}");
                                break;
                            }
                        case Opcode.Call:
                            {
                                Call call = (Call)insn;
                                writer.WriteLine("local {0} = {1}({2})", temps[call], Format(call.Method), FormatTuple(call.Arguments));
                                break;
                            }
                        case Opcode.TupleNew:
                            {
                                TupleNew tupNew = (TupleNew)insn;
                                writer.WriteLine("-- New tuple ({0})", temps[tupNew]);
                                break;
                            }
                        case Opcode.TupleGet:
                            {
                                TupleGet getter = (TupleGet)insn;
                                writer.WriteLine("local {2}, {0} = ({1})", temps[getter], FormatTuple(getter.Tuple, true), String.Concat(Enumerable.Repeat("_, ", getter.Offset)));
                                break;
                            }
                        case Opcode.TupleRemainder:
                            {
                                TupleRemainder getter = (TupleRemainder)insn;
                                writer.WriteLine("-- Tuple Remainder ({0})", temps[getter]);
                                break;
                            }
                        case Opcode.ReferenceGet:
                            {
                                ReferenceGet getter = (ReferenceGet)insn;
                                writer.WriteLine("local {0} = {1}", temps[getter], Format(getter.Reference));
                                break;
                            }
                        case Opcode.ReferenceSet:
                            {
                                ReferenceSet setter = (ReferenceSet)insn;
                                writer.WriteLine("{0} = {1}", Format(setter.Reference), Format(setter.Value));
                                break;
                            }
                        case Opcode.ReferenceNew:
                            {
                                ReferenceNew refNew = (ReferenceNew)insn;
                                writer.WriteLine("local {0} = {1}", refs[refNew], Format(refNew.Value));
                                break;
                            }
                        case Opcode.ClosureNew:
                            {
                                ClosureNew closNew = (ClosureNew)insn;
                                writer.Write("local {0} = ", temps[closNew]);
                                Dictionary<Upvalue, String> lookup = new Dictionary<Upvalue, string>();
                                foreach (Tuple<IValue, Upvalue> closed in Enumerable.Zip(closNew.ClosedUpvalues, closNew.Function.ClosedUpvalues, Tuple.Create))
                                {
                                    lookup.Add(closed.Item2, Format(closed.Item1));
                                }
                                foreach (Tuple<IValue, Upvalue> open in Enumerable.Zip(closNew.OpenUpvalues, closNew.Function.OpenUpvalues, Tuple.Create))
                                {
                                    lookup.Add(open.Item2, Format(open.Item1));
                                }
                                new FunctionCodegen(closNew.Function, lookup, funcAllocator).Write(writer);
                                break;
                            }
                    }
                }
            }
        }

        private void WriteJump(Block block, Block target, TextWriter writer)
        {
            foreach (Phi phi in target.PhiNodes)
            {
                writer.WriteLine("{0} = {1}", phis[phi], Format(phi.Source[block]));
            }

            writer.WriteLine("goto {0}", blocks[target]);
        }

        private string Format(IValue value)
        {
            if (value is Constant)
            {
                Literal lit = ((Constant)value).Literal;
                return lit == Literal.Nil ? "nil" : lit.ToString();
            }
            if (value is Upvalue) return upvalues[(Upvalue)value];
            if (value is Phi) return phis[(Phi)value];
            if (value.Kind == ValueKind.Reference) return refs[value];

            return temps[value];
        }

        private string FormatTuple(IValue value, bool some = false)
        {
            if (value is Constant && ((Constant)value).Literal == Literal.Nil) return some ? "nil" : "";
            if (value.Kind != ValueKind.Tuple) return Format(value);

            var tuple = value as TupleNew;
            if (tuple != null)
            {
                Constant remaining = tuple.Remaining as Constant;
                if (remaining != null && remaining.Literal == Literal.Nil)
                {
                    return String.Join(", ", tuple.Values.Select(Format));
                }
            }

            return temps[value] + " --[[Probably incorrect tuple]]";
        }

        private string FormatKey(IValue value)
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

        public void Write(TextWriter writer)
        {
            writer.Write("function(");
            bool first = true;
            foreach (Argument argument in function.Arguments)
            {
                if (!first)
                {
                    writer.Write(", ");
                }
                else
                {
                    first = true;
                }

                writer.Write(temps[argument]);
            }
            writer.WriteLine(")");

            foreach (Block block in function.EntryPoint.ReachableLazy())
            {
                WriteBlock(block, writer);
            }

            writer.WriteLine("end");
        }
    }
}

