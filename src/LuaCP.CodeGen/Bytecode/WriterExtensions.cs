namespace LuaCP.CodeGen.Bytecode
{
	public static class WriterExtensions
	{
		public static void Load(this IBytecodeWriter writer, ILuaValue value, Register to)
		{
			Register from = value as Register;
			if (from == null)
			{
				writer.LoadK((LuaConstant)value, to);
			}
			else
			{
				writer.Move(from, to);
			}
		}
	}
}

