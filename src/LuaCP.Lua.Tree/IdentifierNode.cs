using LuaCP.IR;
using LuaCP.IR.Instructions;

namespace LuaCP.Tree
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
				if (!setupBuilder.Variables.TryGet(Name, out reference))
				{
					IValue globals = builder.Block.AddLast(new ReferenceGet(builder.Variables.Globals));
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

		public BlockBuilder Declare(BlockBuilder builder, IValue value)
		{
			builder.Variables.Declare(Name, builder.Block.AddLast(new ReferenceNew(value)));
			return builder;
		}

		public override BlockBuilder Build(BlockBuilder builder, out IValue result)
		{
			if (!builder.Variables.TryGet(Name, out result))
			{
				IValue globals = builder.Block.AddLast(new ReferenceGet(builder.Variables.Globals));
				TableGet tableGet = new TableGet(globals, builder.Constants[new Literal.String(Name)]);
				builder.Block.AddLast(tableGet);
				result = tableGet;
			}

			return builder;
		}
	}
}
