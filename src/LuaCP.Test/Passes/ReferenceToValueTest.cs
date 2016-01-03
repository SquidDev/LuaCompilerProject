using System;
using NUnit.Framework;
using LuaCP.IR.Components;
using LuaCP.IR.Instructions;
using System.Linq;
using LuaCP.Debug;

namespace LuaCP.Passes
{
	[TestFixture]
	public class ReferenceToValueTest
	{
		/// <summary>
		/// Test an expression in the form:
		/// <code>
		/// x = nil
		/// if(x) x = 1
		/// else x = 2
		/// end
		/// </code>
		/// </summary>
		[Test]
		public void TestTernaryStyle()
		{
			Module module = new Module();
			ConstantPool pool = module.Constants;
			Function func = module.EntryPoint;

			Block start = func.EntryPoint;
			Block setA = new Block(func), setB = new Block(func), end = new Block(func);

			// local x; if(1) { } else { }
			ReferenceNew x = start.AddLast(new ReferenceNew(pool.Nil));
			start.AddLast(new BranchCondition(pool[1], setA, setB));

			// x = 2
			setA.AddLast(new ReferenceSet(x, pool[2]));
			setA.AddLast(new Branch(end));

			// x = 2
			setB.AddLast(new ReferenceSet(x, pool[3]));
			setB.AddLast(new Branch(end));

			// return x
			ReferenceGet getter = end.AddLast(new ReferenceGet(x));
			TupleNew tuple = end.AddLast(new TupleNew(new [] { getter }, pool.Nil));
			end.AddLast(new Return(tuple));

			new Exporter(Console.Out).ModuleLong(module);

			ReferenceToValue.Runner(func);

			Assert.AreEqual(1, end.PhiNodes.Count);

			Phi phi = end.PhiNodes.First();

			Assert.Contains(setA, phi.Source.Keys.ToList());
			Assert.Contains(setB, phi.Source.Keys.ToList());

			Assert.AreEqual(pool[2], phi.Source[setA]);
			Assert.AreEqual(pool[3], phi.Source[setB]);
		}

		/// <summary>
		/// Test an expression in the form:
		/// <code>
		/// x = 2
		/// if (1) x = 3
		/// else ()
		/// return x
		/// </code>
		/// </summary>
		[Test]
		public void TestLongOrStyle()
		{
			Module module = new Module();
			ConstantPool pool = module.Constants;
			Function func = module.EntryPoint;

			Block start = func.EntryPoint;
			Block set = new Block(func), end = new Block(func), empty = new Block(func);

			// local x; if(1) { } else { }
			ReferenceNew x = start.AddLast(new ReferenceNew(pool[2]));
			start.AddLast(new BranchCondition(pool[1], empty, set));

			// x = 3
			set.AddLast(new ReferenceSet(x, pool[3]));
			set.AddLast(new Branch(end));

			// Nothing here
			empty.AddLast(new Branch(end));

			// return x
			ReferenceGet getter = end.AddLast(new ReferenceGet(x));
			TupleNew tuple = end.AddLast(new TupleNew(new [] { getter }, pool.Nil));
			end.AddLast(new Return(tuple));

			new Exporter(Console.Out).ModuleLong(module);

			ReferenceToValue.Runner(func);

			Assert.AreEqual(1, end.PhiNodes.Count);

			Phi phi = end.PhiNodes.First();

			Assert.Contains(empty, phi.Source.Keys.ToList());
			Assert.Contains(set, phi.Source.Keys.ToList());

			Assert.AreEqual(pool[2], phi.Source[empty]);
			Assert.AreEqual(pool[3], phi.Source[set]);
		}

		/// <summary>
		/// Test an expression in the form:
		/// <code>
		/// x = 2
		/// if(1) x = 3
		/// return x
		/// </code>
		/// </summary>
		[Test]
		public void TestLonghandOrStyle()
		{
			Module module = new Module();
			ConstantPool pool = module.Constants;
			Function func = module.EntryPoint;

			Block start = func.EntryPoint;
			Block set = new Block(func), end = new Block(func);

			// local x; if(1) { } else { }
			ReferenceNew x = start.AddLast(new ReferenceNew(pool[2]));
			start.AddLast(new BranchCondition(pool[1], end, set));

			// x = 3
			set.AddLast(new ReferenceSet(x, pool[3]));
			set.AddLast(new Branch(end));

			// return x
			ReferenceGet getter = end.AddLast(new ReferenceGet(x));
			TupleNew tuple = end.AddLast(new TupleNew(new [] { getter }, pool.Nil));
			end.AddLast(new Return(tuple));

			new Exporter(Console.Out).ModuleLong(module);

			ReferenceToValue.Runner(func);

			Assert.AreEqual(1, end.PhiNodes.Count);

			Phi phi = end.PhiNodes.First();

			Assert.Contains(start, phi.Source.Keys.ToList());
			Assert.Contains(set, phi.Source.Keys.ToList());

			Assert.AreEqual(pool[2], phi.Source[start]);
			Assert.AreEqual(pool[3], phi.Source[set]);
		}
	}
}

