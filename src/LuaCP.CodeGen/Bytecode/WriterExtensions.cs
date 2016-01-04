using LuaCP.IR.Components;
using LuaCP.IR;

namespace LuaCP.CodeGen.Bytecode
{
	public static class WriterExtensions
	{
		public static void Load(this IBytecodeWriter writer, ILuaValue value, Register to, bool force = false)
		{
			Register from = value as Register;
			if (from == null)
			{
				writer.LoadK((LuaConstant)value, to);
			}
			else
			{
				if(force || from.Index != to.Index) writer.Move(from, to);
			}
		}

		public static IBytecodeWriter Function(this IBytecodeWriter writer, Function function)
		{
			int args = function.Arguments.Count;
			VarargType vararg = VarargType.None;

			Argument arg = function.Arguments[args - 1];
			if (arg.Kind == ValueKind.Tuple)
			{
				args--;
				vararg = arg.Users.TotalCount == 0 ? VarargType.Exists : VarargType.Used;
			}

			return writer.Function(function.OpenUpvalues.Count + function.ClosedUpvalues.Count, args, vararg);
		}
	}
}

