using System.Collections.Generic;
using System.Linq;
using LuaCP.Reporting;

namespace LuaCP.IR.Components
{
	public class Module
	{
		public readonly ISet<Function> Functions = new HashSet<Function>();
		public Function EntryPoint;
		public readonly ConstantPool Constants = new ConstantPool();
		public readonly IReporter Reporter = new ConsoleReporter();

		public Module()
		{
			EntryPoint = new Function(this, Enumerable.Empty<string>(), true);
		}
	}
}

