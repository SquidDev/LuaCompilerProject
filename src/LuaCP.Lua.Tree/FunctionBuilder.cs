using System.Collections.Generic;
using System.Linq;
using LuaCP.IR;
using LuaCP.IR.Components;
using LuaCP.IR.Instructions;

namespace LuaCP.Tree
{
	public class FunctionBuilder
	{
		public readonly BlockBuilder EntryPoint;
		public readonly Function Function;
		public readonly List<IValue> Upvalues = new List<IValue>();

		public FunctionBuilder(Module module, IValue globals)
		{
			Function = module.EntryPoint;
			EntryPoint = new BlockBuilder(Function);
			EntryPoint.Variables.Declare(VariableScope.GlobalTable, globals);
		}

		public FunctionBuilder(BlockBuilder builder, IEnumerable<string> args, bool dots)
		{
			Function = new Function(builder.Block.Function.Module, args, dots);
			EntryPoint = new BlockBuilder(
				Function.EntryPoint, 
				null, 
				new VariableScope(new FunctionVariableScope(builder.Variables, this)),
				new LabelScope(Function), null
			);
			
			int i = 0;
			foreach (string arg in args)
			{
				ReferenceNew rNew = EntryPoint.Block.AddLast(new ReferenceNew(Function.Arguments[i]));
				EntryPoint.Variables.Declare(arg, rNew);
				i++;
			}
		}

		public BlockBuilder Accept(INode node)
		{
			BlockBuilder builder = node.Build(EntryPoint);
			if (builder.Block.Last == null || !builder.Block.Last.Opcode.IsTerminator())
			{
				IValue value = builder.Block.AddLast(new TupleNew(Enumerable.Empty<IValue>(), builder.Constants.Nil));
				builder.Block.AddLast(new Return(value));
			}

			EntryPoint.Labels.Validate();

			return builder;
		}
	}
}
