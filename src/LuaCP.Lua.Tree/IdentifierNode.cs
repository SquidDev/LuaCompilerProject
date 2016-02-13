using LuaCP.IR;
using LuaCP.IR.Instructions;
using LuaCP.Tree;

namespace LuaCP.Lua.Tree
{
	public class IdentifierNode : ValueNode, IAssignable, IDeclarable
	{
		private readonly string name;

		public string Name { get { return name; } }

		public IdentifierNode(string name)
		{
			this.name = name;
		}

		public BlockBuilder Assign(BlockBuilder setupBuilder, out Assigner assigner)
		{
			assigner = (builder, value) =>
			{
				IValue reference;
				if (!setupBuilder.Get<IVariableScope>().TryGet(Name, out reference))
				{
					IValue globals = builder.Block.AddLast(new ReferenceGet(builder.Get<IVariableScope>().Globals));
					builder.Block.AddLast(new TableSet(globals, builder.Constants[new Literal.String(Name)], value));
				}
				else
				{
					builder.Block.AddLast(new ReferenceSet(reference, value));
				}
				return builder;
			};

			return setupBuilder;
		}

		public virtual BlockBuilder Declare(BlockBuilder builder, IValue value)
		{
			builder.Get<IVariableScope>().Declare(Name, builder.Block.AddLast(new ReferenceNew(value)));
			return builder;
		}

		public override BlockBuilder Build(BlockBuilder builder, out IValue result)
		{
			if (!builder.Get<IVariableScope>().TryGet(Name, out result))
			{
				IValue globals = builder.Block.AddLast(new ReferenceGet(builder.Get<IVariableScope>().Globals));
				TableGet tableGet = new TableGet(globals, builder.Constants[new Literal.String(Name)]);
				builder.Block.AddLast(tableGet);
				result = tableGet;
			}

			return builder;
		}
	}
}
