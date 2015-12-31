using System;
using LuaCP.IR.Instructions;
using LuaCP.IR;
using LuaCP.IR.Components;
using System.Collections.Generic;
using System.Linq;

namespace LuaCP.Tree.Statement
{
	public class ForInNode : Node
	{
		public readonly IReadOnlyList<IDeclarable> Variables;
		public readonly IValueNode Function;
		public readonly IValueNode Table;
		public readonly INode Body;

		public ForInNode(IValueNode function, IValueNode table, IEnumerable<IDeclarable> variables, INode body)
		{
			Variables = variables.ToList();
			Function = function;
			Table = table;
			Body = body;
		}

		public override BlockBuilder Build(BlockBuilder builder)
		{
			IValue function, table;
			builder = Function.Build(builder, out function);
			if (Table == null)
			{
				if (function.Kind != ValueKind.Tuple) throw new Exception("Cannot have ForIn loop with one non-tuple argument");
				table = builder.Block.AddLast(new TupleGet(function, 1));
				function = builder.Block.AddLast(new TupleGet(function, 0));
			}
			else
			{
				builder = Table.Build(builder, out table);
			}

			BlockBuilder test = builder.MakeChild();
			builder.Block.AddLast(new Branch(test.Block));

			BlockBuilder cont = builder.Continue();
			Phi value = new Phi(test.Block);
			value.Source.Add(builder.Block, builder.Constants.Nil);

			BlockBuilder body = builder.MakeLoop(new LoopState(test, cont));

			TupleNew tuple = test.Block.AddLast(new TupleNew(new IValue[] { table, value }, builder.Constants.Nil));
			IValue call = test.Block.AddLast(new Call(function, tuple));
			IValue val = test.Block.AddLast(new TupleGet(call, 0));
			IValue isNil = test.Block.AddLast(new BinaryOp(Opcode.Equals, val, builder.Constants.Nil));

			test.Block.AddLast(new BranchCondition(isNil, cont.Block, body.Block));

			for (int i = 0; i < Variables.Count; i++)
			{
				body = Variables[i].Declare(body, body.Block.AddLast(new TupleGet(call, i)));
			}
			body = Body.Build(body);
			value.Source.Add(body.Block, val);
			if (body.Block.Last == null || !body.Block.Last.Opcode.IsTerminator())
			{
				body.Block.AddLast(new Branch(test.Block));
			}

			return cont;
		}
	}
}

