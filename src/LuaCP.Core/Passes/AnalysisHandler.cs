using System;
using LuaCP.IR.Components;
using LuaCP.Collections;

namespace LuaCP.Passes
{
	/// <summary>
	/// Manages data throughout a series of passes
	/// </summary>
	public sealed class PassManager
	{
		public readonly Module Module;
		private readonly ValidDictionary<Module> moduleData;

		public PassManager(Module module)
		{
			Module = module;
			moduleData = new ValidDictionary<Module>(module);
		}

		public bool RunPass(Pass<Module> pass)
		{
			return RunPass(pass, Module);
		}

		public bool RunPass<T>(Pass<T> pass, T item)
		{
			if (pass(this, item))
			{
				moduleData.Invalidate();
				return true;
			}

			return false;
		}

		public T Get<T>(Func<Module, T> factory)
		{
			return moduleData.Evaluate<T>(factory);
		}

		public void Invalidate<T>()
		{
			moduleData.Invalidate<T>();
		}

		public static void Run(Module module, Pass<Module> pass)
		{
			new PassManager(module).RunPass(pass);
		}
	}
}

