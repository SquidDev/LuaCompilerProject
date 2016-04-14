using System;
using LuaCP.IR.Instructions;
using LuaCP.IR;

namespace LuaCP.CodeGen.Bytecode
{
	public enum VarargType
	{
		None,
		Exists,
		Used,
	}

	/// <summary>
	/// Marker interface for Lua values
	/// </summary>
	public interface ILuaValue
	{
	}

	public class Register : ILuaValue
	{
		public static readonly Register PseudoRegister = new Register(0);

		public readonly int Index;

		public Register(int index)
		{
			Index = index;
		}

		public override string ToString()
		{
			return "#" + Index;
		}
	}

	public class LuaConstant : ILuaValue
	{
		public readonly Literal Constant;

		public LuaConstant(Constant constant)
		{
			Constant = constant.Literal;
		}

		public LuaConstant(Literal constant)
		{
			Constant = constant;
		}

		public override string ToString()
		{
			return "k(" + Constant.ToString() + ")";
		}
	}

	/// <summary>
	/// A marker class used for jumps
	/// </summary>
	public class Label
	{
	}

	public interface IBytecodeWriter : IDisposable
	{
		void Move(Register from, Register to);

		void LoadK(LuaConstant constant, Register to);

		void LoadBoolean(bool value, bool jump, Register to);

		void LoadNil(Register start, int count);

		void GetUpvalue(int index, Register to);

		void GetTableUpvalue(int index, ILuaValue key, Register to);

		void GetTable(ILuaValue table, ILuaValue key, Register to);

		void SetUpvalue(int index, ILuaValue value);

		void SetTableUpvalue(int index, ILuaValue key, ILuaValue value);

		void SetTable(ILuaValue table, ILuaValue key, ILuaValue value);

		void NewTable(int arraySize, int hashSize, Register to);

		void Self(Register table, ILuaValue key, Register to);

		void BinaryOperator(Opcode opcode, ILuaValue left, ILuaValue right, Register to);

		void UnaryOperator(Opcode opcode, ILuaValue left, Register to);

		void Concat(Register start, Register finish, Register to);

		void Jump(Label target, Register close = null);

		void Comparison(Opcode opcode, bool expected, ILuaValue left, ILuaValue right, Label success, Register close = null);

		void Test(bool expected, ILuaValue value, Label success, Register close = null);

		void TestSet(bool expected, ILuaValue value, Label success, Register target, Register close = null);

		void Call(Register function, int nArgs, int nReturn);

		void TailCall(Register function, int nArgs);

		void Return(Register start, int nReturn);

		void ForLoop(Register start, Label target);

		void ForPrep(Register start, Label target);

		void TForCall(Register start, int nReturn);

		void TForLoop(Register start, Label target);

		void SetList(Register start, int offset, int count);

		void Closure(IBytecodeWriter closure, Register to);

		void Vararg(Register start, int count);

		void Label(Label label);

		IBytecodeWriter Function(int upvalues, int args, VarargType vararg);
	}
}

