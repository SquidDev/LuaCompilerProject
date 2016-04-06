using System;
using LuaCP.IR.Instructions;
using LuaCP.IR;
using LuaCP.IR.Components;
using System.Collections.Generic;
using System.Linq;
using LuaCP.Tree;

namespace LuaCP.Lua.Tree.Statement
{
	public class ForInNode : Node
	{
		public readonly IReadOnlyList<IDeclarable> Variables;
		public readonly IReadOnlyList<IValueNode> Args;
		public readonly INode Body;

		public ForInNode(IEnumerable<IValueNode> args, IEnumerable<IDeclarable> variables, INode body)
		{
			Variables = variables.ToList();
			Args = args.ToList();
			Body = body;
		}

		public override BlockBuilder Build(BlockBuilder builder)
		{
			IValue setup;
			builder = Args.BuildAsTuple(builder, out setup);

			IValue function = builder.Block.AddLast(new TupleGet(setup, 0));
			IValue table = builder.Block.AddLast(new TupleGet(setup, 1));
			IValue initial = builder.Block.AddLast(new TupleGet(setup, 2));

			BlockBuilder test = builder.MakeChild();
			builder.Block.AddLast(new Branch(test.Block));

			BlockBuilder cont = builder.Continue();
			Phi value = new Phi(test.Block);
			value.Source.Add(builder.Block, initial);

			BlockBuilder body = builder.MakeLoop(new LoopState(test, cont));

			TupleNew args = test.Block.AddLast(new TupleNew(new IValue[] { table, value }, builder.Constants.Nil));
			IValue call = test.Block.AddLast(new Call(function, args));
			IValue val = test.Block.AddLast(new TupleGet(call, 0));
			IValue isNil = test.Block.AddLast(new BinaryOp(Opcode.Equals, val, builder.Constants.Nil));

			test.Block.AddLast(new BranchCondition(isNil, cont.Block, body.Block));

			for (int i = 0; i < Variables.Count; i++)
			{
				body = Variables[i].Declare(body, body.Block.AddLast(new TupleGet(call, i)));
			}
			body = Body.Build(body);
			value.Source.Add(body.Block, val);
			if (!body.Block.IsTerminated()) body.Block.AddLast(new Branch(test.Block));

			return cont;
		}
	}
}

