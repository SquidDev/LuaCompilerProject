using System.Collections.Generic;
using LuaCP.Collections;
using LuaCP.IR.Instructions;
using System;
using System.Text;

namespace LuaCP.CodeGen.Bytecode
{
	public class LasmBytecodeWriter : IBytecodeWriter
	{
		private readonly Dictionary<Label, int> labels = new Dictionary<Label, int>();
		private readonly MultiMap<Label, Tuple<string, int>> labelLookup = new MultiMap<Label, Tuple<string, int>>();

		private readonly Allocator<IBytecodeWriter> funcAllocator;
		private readonly StringBuilder builder;
		private int pc = 0;

		public LasmBytecodeWriter(StringBuilder builder, VarargType type)
			: this(builder, new Allocator<IBytecodeWriter>(), 1, 0, type)
		{
		}

		public LasmBytecodeWriter(StringBuilder builder, Allocator<IBytecodeWriter> funcAllocator, int upvalues, int args, VarargType type)
		{
			this.builder = builder;
			this.funcAllocator = funcAllocator;

			funcAllocator.Add(this);

			builder.AppendLine(".function");
			if (upvalues > 0) Write(".upvalues {0}", upvalues);
			if (args > 0) Write(".args {0}", args);
			if (type != VarargType.None) Write(".varargs {0}", type.ToString().ToLower());
		}

		public void Move(Register from, Register to)
		{
			WriteInsn("move {1} {0}", from, to);
			pc++;
		}

		public void LoadK(LuaConstant constant, Register to)
		{
			WriteInsn("loadk {0} {1}", to, constant);
			pc++;
		}

		public void LoadBoolean(bool value, bool jump, Register to)
		{
			WriteInsn("loadbool {0} {1} {2}", to, value, jump);
			pc++;
		}

		public void LoadNil(Register start, int count)
		{
			WriteInsn("loadnil {0} {1}", start, count);
			pc++;
		}

		public void GetUpvalue(int index, Register to)
		{
			WriteInsn("getupval {1} {0}", index, to);
			pc++;
		}

		public void GetTableUpvalue(int index, ILuaValue key, Register to)
		{
			WriteInsn("gettabup {2} {0} {1}", index, key, to);
			pc++;
		}

		public void GetTable(ILuaValue table, ILuaValue key, Register to)
		{
			WriteInsn("gettable {2} {0} {1}", table, key, to);
			pc++;
		}

		public void SetUpvalue(int index, ILuaValue value)
		{
			WriteInsn("setupval {1} {0}", index, value);
			pc++;
		}

		public void SetTableUpvalue(int index, ILuaValue key, ILuaValue value)
		{
			WriteInsn("settabupl {0} {1} {2}", index, key, value);
			pc++;
		}

		public void SetTable(ILuaValue table, ILuaValue key, ILuaValue value)
		{
			WriteInsn("settable {0} {1} {2}", table, key, value);
			pc++;
		}

		public void NewTable(int arraySize, int hashSize, Register to)
		{
			WriteInsn("newtable {2} {0} {1}", arraySize, hashSize, to);
			pc++;
		}

		public void Self(Register table, ILuaValue key, Register to)
		{
			WriteInsn("self {2} {0} {1}", table, key, to);
			pc++;
		}

		public void BinaryOperator(Opcode opcode, ILuaValue left, ILuaValue right, Register to)
		{
			if (opcode > Opcode.RShift || opcode < Opcode.Add) throw new ArgumentException("Expected a binary operator");
			if (opcode == Opcode.Concat) throw new ArgumentException("Use concat instead");

			WriteInsn("{0} {3} {1} {2}", opcode.ToString().ToLowerInvariant(), left, right, to);
			pc++;
		}

		public void UnaryOperator(Opcode opcode, ILuaValue left, Register to)
		{
			if (!opcode.IsUnaryOperator()) throw new ArgumentException("Expected a unary operator");

			WriteInsn("{0} {2} {1}", opcode.ToString().ToLowerInvariant(), left, to);
			pc++;
		}

		public void Concat(Register start, Register finish, Register to)
		{
			WriteInsn("concat {2} {0} {1}", start, finish, to);
			pc++;
		}

		public void Jump(Label target, Register close = null)
		{
			WriteInsn("jump {0} {1}", close, GetLabel(target));
			pc++;
		}

		public void Comparison(Opcode opcode, bool expected, ILuaValue left, ILuaValue right, Label success, Register close = null)
		{
			if (opcode < Opcode.Equals || opcode > Opcode.LessThanEquals) throw new ArgumentException("Expected a comparison operator");
			if (opcode == Opcode.NotEquals)
			{
				opcode = Opcode.Equals;
				expected = !expected;
			}

			WriteInsn("{0} {1} {2} {3}", opcode.ToString().ToLowerInvariant(), expected, left, right);
			pc++;

			Jump(success, close);
		}

		public void Test(bool expected, ILuaValue value, Label success, Register close = null)
		{
			WriteInsn("test {0} {1}", value, expected);
			pc++;

			Jump(success, close);
		}

		public void TestSet(bool expected, ILuaValue value, Label success, Register target, Register close = null)
		{
			WriteInsn("testset {0} {1} {2}", target, value, expected);
			pc++;

			Jump(success, close);
		}

		public void Call(Register function, int nArgs, int nReturn)
		{
			WriteInsn("call {0} {1} {2}", function, nArgs, nReturn);
			pc++;
		}

		public void TailCall(Register function, int nArgs)
		{
			WriteInsn("tailcall {0} {1}", function, nArgs);
			pc++;
		}

		public void Return(Register start, int nReturn)
		{
			WriteInsn("return {0} {1}", start, nReturn);
			pc++;
		}

		public void ForLoop(Register start, Label target)
		{
			WriteInsn("forloop {0} {1}", start, GetLabel(target));
			pc++;
		}

		public void ForPrep(Register start, Label target)
		{
			WriteInsn("forprep {0} {1}", start, GetLabel(target));
			pc++;
		}

		public void TForCall(Register start, int nReturn)
		{
			WriteInsn("tforcall {0} {1}", start, nReturn);
			pc++;
		}

		public void TForLoop(Register start, Label target)
		{
			WriteInsn("tforloop {0} {1}", start, GetLabel(target));
			pc++;
		}

		public void SetList(Register start, int offset, int count)
		{
			WriteInsn("setlist {0} {1} {2}", start, offset, count);
			pc++;
		}

		public void Closure(IBytecodeWriter closure, Register to)
		{
			WriteInsn("closure {1} {0}", funcAllocator[closure], to);
			pc++;
		}

		public void Vararg(Register start, int count)
		{
			WriteInsn("vararg {0} {1}", start, count);
			pc++;
		}

		public void Label(Label label)
		{
			labels.Add(label, pc);

			ISet<Tuple<string, int>> items;
			if (labelLookup.TryGetValue(label, out items))
			{
				foreach (Tuple<string, int> pair in items)
				{
					builder.Replace(pair.Item1, "$" + (pc - pair.Item2 - 1));
				}

				labelLookup.Remove(label);
			}
		}

		protected string GetLabel(Label label)
		{
			int targetPc;
			if (labels.TryGetValue(label, out targetPc)) return "$" + (targetPc - pc);

			string name = "$<" + Guid.NewGuid() + ">$";
			labelLookup.Add(label, Tuple.Create(name, pc));
			return name;
		}

		public IBytecodeWriter Function(int upvalues, int args, VarargType vararg)
		{
			return new LasmBytecodeWriter(builder, funcAllocator, upvalues, args, vararg);
		}

		public void Dispose()
		{
			builder.AppendLine(".end");
		}

		private void Write(string format, params object[] args)
		{
			builder.Append("\t").AppendFormat(format, args).AppendLine();
		}
			
		private void WriteInsn(string format, params object[] args)
		{
			builder.Append("\t.").AppendFormat(format, args).AppendFormat(" ; Insn {0}", pc + 1).AppendLine();
		}
	}
}

