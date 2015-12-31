using System;
using FParsec;
using LuaCP.Debug;
using LuaCP.IR.Components;
using LuaCP.Tree;
using Microsoft.FSharp.Core;
using Con = System.Console;
using LuaCP.CodeGen.Lua;
using LuaCP.Parser;
using System.CodeDom.Compiler;
using LuaCP.IR;

namespace LuaCP.REPL
{
	public class LanguageREPL
	{
		private Language language = new Language();

		private void Add<T>()
		{
			language.Get<T>(x => (T)typeof(T).GetConstructor(new[] { typeof(Language) }).Invoke(new[] { x })); 
		}

		public INode GetSource(string message)
		{
			if (String.IsNullOrWhiteSpace(message)) return null;
			if (message.StartsWith("="))
			{
				Con.WriteLine(message.Substring(1));
				message = "return " + message.Substring(1);
			}
			CharParsers.ParserResult<INode, Unit> result = CharParsers.run(language.Script, message);
			if (result.IsSuccess)
			{
				return ((CharParsers.ParserResult<INode, Unit>.Success)result).Item1;
			}
			else
			{
				var exprParser = language.Source(language.Expression);
				CharParsers.ParserResult<IValueNode, Unit> expression = CharParsers.run(exprParser, message);
				if (expression.IsSuccess)
				{
					return ((CharParsers.ParserResult<IValueNode, Unit>.Success)expression).Item1;
				}
				else
				{
					var res = (CharParsers.ParserResult<INode, Unit>.Failure)result;
					Con.WriteLine("Error {0}", res.Item1);
					return null;
				}
			}
		}

		public void Run()
		{
			Add<Parser.Expression>();
			Add<Parser.Statement>();
			Add<Parser.Extensions.Adt>();
			Add<Parser.Extensions.Lambda>();
			Add<Parser.Extensions.OpEquals>();

			Module module = null;
			while (true)
			{
				Con.Write("> ");
				string line = Console.ReadLine();
				if (line.StartsWith("!"))
				{
					if (module == null)
					{
						Console.WriteLine("No module"); 
						continue;
					}

					switch (line.Substring(1))
					{
						case "help":
							Console.WriteLine("!help: Print this help");
							Console.WriteLine("!dump: Dump the previous source");
							Console.WriteLine("!graph: Plot the CFG of the previous source");
							break;
						case "dump":
							new Exporter(Con.Out).ModuleLong(module);
							break;
						case "graph":
							DotExporter.Write(module);
							break;
						case "code":
							new FunctionCodegen(module.EntryPoint).Write(new IndentedTextWriter(Con.Out));
							break;
						default:
							Console.WriteLine("Unknown command " + line);
							goto case "help";
					}
				}
				else
				{
					INode source = GetSource(line);
					if (source == null) continue;

					module = new Module();

					new FunctionBuilder(module, new GlobalEnvironment()).Accept(source);
					new Exporter(Con.Out).ModuleLong(module);

					PassExtensions.Default(module);

					new Exporter(Con.Out).ModuleLong(module);
				}
			}
		}
	}

	class Program
	{
		public static void Main(string[] args)
		{
			new LanguageREPL().Run();
		}
	}
}
