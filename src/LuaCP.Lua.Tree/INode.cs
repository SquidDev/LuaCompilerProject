using LuaCP.IR;
using LuaCP.Reporting;

namespace LuaCP.Tree
{
	public interface INode
	{
		BlockBuilder Build(BlockBuilder builder);

		Range Position { get; set; }
	}

	public interface IValueNode : INode
	{
		BlockBuilder Build(BlockBuilder builder, out IValue result);
	}

	public delegate BlockBuilder Assigner(BlockBuilder assignBuilder, IValue value);
	public interface IAssignable : IValueNode
	{
		BlockBuilder Assign(BlockBuilder setupBuilder, out Assigner value);
	}

	public interface IDeclarable : IAssignable
	{
		string Name { get; }

		BlockBuilder Declare(BlockBuilder builder, IValue value);
	}
}

