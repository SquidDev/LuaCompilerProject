using System.Collections.Generic;
using System.Linq;
using LuaCP.IR;
using LuaCP.IR.Components;
using LuaCP.IR.Instructions;
using LuaCP.Tree;

namespace LuaCP.Lua.Tree
{
	public class FunctionBuilder
	{
		public readonly BlockBuilder EntryPoint;
		public readonly Function Function;
		public readonly List<IValue> Upvalues = new List<IValue>();

		public FunctionBuilder(Module module)
		{
			Function = module.EntryPoint;
			EntryPoint = new BlockBuilder(Function);
			EntryPoint.Scopes.Add<IVariableScope>(new VariableScope());
			EntryPoint.Scopes.Add(new LabelScope(Function));
			EntryPoint.Scopes.Add(new TypeScope());
			EntryPoint.Get<IVariableScope>().Declare(VariableScope.GlobalTable, new Upvalue(Function, true));
		}

		public FunctionBuilder(BlockBuilder builder, IEnumerable<string> args, bool dots)
		{
			Function = new Function(builder.Block.Function.Module, args, dots);

			ScopeDictionary scopeDictionary = new ScopeDictionary(null)
			{
				(IVariableScope)new VariableScope(new FunctionVariableScope(builder.Get<IVariableScope>(), this)),
				new LabelScope(Function),
				new TypeScope(builder.Get<TypeScope>()),
			};

			EntryPoint = new BlockBuilder(Function.EntryPoint, null, scopeDictionary, null);
		}

		public BlockBuilder Accept(INode node)
		{
			BlockBuilder builder = node.Build(EntryPoint);
			if (builder.Block.Last == null || !builder.Block.Last.Opcode.IsTerminator())
			{
				IValue value = builder.Block.AddLast(new TupleNew(Enumerable.Empty<IValue>(), builder.Constants.Nil));
				builder.Block.AddLast(new Return(value));
			}

			EntryPoint.Get<LabelScope>().Validate();

			return builder;
		}
	}
}
