using System;
using System.IO;
using LuaCP.Collections;
using LuaCP.IR.Components;
using LuaCP.Passes.Analysis;
using LuaCP.Debug;

namespace LuaCP.Passes
{
	/// <summary>
	/// Manages data throughout a series of passes
	/// </summary>
	public sealed class PassManager
	{
		public readonly Module Module;
		private readonly ValidDictionary<Module> moduleData;
		private readonly bool verify;

		public PassManager(Module module, bool verify = false)
		{
			Module = module;
			moduleData = new ValidDictionary<Module>(module);
			this.verify = verify;
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

				Validate();
				return true;
			}

			return false;
		}

		public void Validate()
		{
			if (verify)
			{
				// TODO: Fix this slightly
				StringWriter writer = new StringWriter();
				writer.WriteLine("Validation errors:");
				bool errors = false;
				int index = 0;
				foreach (Function function in Module.Functions)
				{
					using (StringWriter funcWriter = new StringWriter())
					{
						var messager = new IRVerifier.WriterMessager(funcWriter, function);
						IRVerifier.Run(function, messager);
						if (messager.HasErrors)
						{
							errors = true;
							writer.WriteLine("Function " + index);
							writer.Write(funcWriter);
							new Exporter(writer).FunctionLong(function);
						}
					}

					index++;
				}


				if (errors) throw new VerificationException(writer.ToString());
			}
		}

		public T Get<T>(Func<Module, T> factory)
		{
			return moduleData.Evaluate<T>(factory);
		}

		public void Invalidate<T>()
		{
			moduleData.Invalidate<T>();
		}

		public static void Run(Module module, Pass<Module> pass, bool verify = false)
		{
			new PassManager(module, verify).RunPass(pass);
		}
	}
}

