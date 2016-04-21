using System;
using LuaCP.IR.Components;
using LuaCP.Collections;
using System.Collections.Generic;
using System.Linq;
using LuaCP.IR.Instructions;
using LuaCP.Debug;
using LuaCP.Graph;

namespace LuaCP.Passes.Analysis
{
	public abstract class ControlNode
	{
		public enum ControlKind
		{
			Block,
			If,
			Jump,
		}

		public sealed class BlockNode : ControlNode
		{
			public readonly Block Block;

			public BlockNode(Block block)
				: base(ControlKind.Block)
			{ 
				Block = block;
			}

			public override IEnumerable<Block> Next
			{
				get
				{
					if (Block.Last.Opcode != Opcode.Return)
					{
						throw new InvalidOperationException("Block should not be terminator");
					}

					return Enumerable.Empty<Block>();
				}
			}

			public override IEnumerable<Block> Pending
			{
				get
				{
					if (Block.Last.Opcode != Opcode.Return)
					{
						throw new InvalidOperationException("Block should not be terminator");
					}

					return Enumerable.Empty<Block>();
				}
			}

			public override void Dump(NodeNumberer numberer, IndentedTextWriter writer)
			{
				writer.WriteLine("Block " + numberer.PrettyGetBlock(Block));
			}
		}

		public sealed class IfNode : ControlNode
		{
			public readonly ControlGroup Success;
			public readonly ControlGroup Failure;

			public IfNode(ControlGroup success, ControlGroup failure)
				: base(ControlKind.If)
			{ 
				Success = success;
				Failure = failure;
			}

			public override IEnumerable<Block> Next
			{
				get { return Success.Last.Next.Concat(Failure.Last.Next); }
			}

			public override IEnumerable<Block> Pending
			{ 
				get { return Success.Last.Pending.Concat(Failure.Last.Pending); } 
			}

			public override void Dump(NodeNumberer numberer, IndentedTextWriter writer)
			{
				writer.WriteLine("If");

				writer.Indent++;
				Success.Dump(numberer, writer);
				writer.Indent--;

				writer.WriteLine("Else");

				writer.Indent++;
				Failure.Dump(numberer, writer);
				writer.Indent--;

				writer.WriteLine("End");
			}
		}

		public sealed class JumpNode : ControlNode
		{
			public bool IsLoopTail = false;
			public readonly Block From;
			public readonly Block Target;

			public JumpNode(Block target, Block from)
				: base(ControlKind.Jump)
			{ 
				Target = target;
				From = from;
			}

			public override IEnumerable<Block> Next
			{
				get { return new [] { Target }; }
			}

			public override IEnumerable<Block> Pending
			{
				get { return From == null ? Enumerable.Empty<Block>() : new [] { From }; }
			}

			public override void Dump(NodeNumberer numberer, IndentedTextWriter writer)
			{
				writer.WriteLine("Jump " + (From == null ? "?" : numberer.PrettyGetBlock(From)) + " => " + numberer.PrettyGetBlock(Target));
				if (IsLoopTail) writer.WriteLine("(Loop Tail)");
			}
		}

		public readonly ControlKind Kind;

		internal ControlNode(ControlKind kind)
		{
			Kind = kind;
		}

		/// <summary>
		/// The next blocks
		/// </summary>
		/// <value>The next.</value>
		public abstract IEnumerable<Block> Next { get; }

		/// <summary>
		/// Blocks which require another block
		/// </summary>
		/// <value>The pending.</value>
		public abstract IEnumerable<Block> Pending { get; }

		public abstract void Dump(NodeNumberer numberer, IndentedTextWriter writer);
	}

	public sealed class ControlGroup
	{
		public bool IsLoopHead = false;
		public readonly Block Entry;
		public readonly IReadOnlyList<ControlNode> Nodes;
		public readonly List<ControlGroup> Children = new List<ControlGroup>();

		public ControlGroup(IReadOnlyList<ControlNode> nodes, Block entry)
		{
			Nodes = nodes;
			Entry = entry;
		}

		public ControlNode First { get { return Nodes[0]; } }

		public ControlNode Last { get { return Nodes[Nodes.Count - 1]; } }

		public void Dump(NodeNumberer numberer, IndentedTextWriter writer)
		{
			if (Entry != null) writer.WriteLine("Group: " + numberer.PrettyGetBlock(Entry));
			writer.Indent++;
			if (IsLoopHead) writer.WriteLine("(Loop Head)");

			foreach (ControlNode node in Nodes) node.Dump(numberer, writer);
			foreach (ControlGroup group in Children) group.Dump(numberer, writer);

			writer.Indent--;
		}
	}

	/// <summary>
	/// Converts a CFG into a series of jumps and if statements
	/// TODO: Create loops too
	/// </summary>
	public sealed class BranchAnalysis
	{
		/*
		 * Test cases:
		 * 
		 * if a then b() else c() end
		 * while true do end
		 * local a = 0 while true do a = a + 1 end
		 * for i = 0, 10 do end
		 */

		public readonly ControlGroup EntryPoint;

		public IReadOnlyDictionary<Block, ControlGroup> Groups { get { return groups; } }

		private readonly HashSet<Block> todo;
		private readonly HashSet<Block> jumps = new HashSet<Block>();
		private readonly Dictionary<Block, ControlGroup> groups = new Dictionary<Block, ControlGroup>();

		public BranchAnalysis(Function function)
		{
			todo = function.EntryPoint.ReachableEager();
			jumps.Add(function.EntryPoint);

			var rootGroups = new Dictionary<Block, ControlGroup>();
			while (jumps.Count > 0)
			{
				var group = CreateGroup(jumps.First());
				rootGroups.Add(group.Entry, group);
			}

			if (todo.Count > 0) throw new Exception("Unvisited blocks");

			foreach (ControlGroup group in groups.Values)
			{
				var last = group.Last;
				if (last.Kind != ControlNode.ControlKind.Jump) continue;

				var jump = (ControlNode.JumpNode)last;
				if (jump.Target.Dominates(jump.From))
				{
					groups[jump.Target].IsLoopHead = true;
					jump.IsLoopTail = true;
				}
			}

			foreach (ControlGroup group in rootGroups.Values)
			{
				if (group.Entry == function.EntryPoint) continue;
				Block block = group.Entry.ImmediateDominator;

				while (true)
				{
					ControlGroup parent;
					if (groups.TryGetValue(block, out parent))
					{
						parent.Children.Add(group);
						break;
					}

					block = block.ImmediateDominator;
				}
			}

			EntryPoint = groups[function.EntryPoint];
		}

		private ControlGroup CreateGroup(Block entry)
		{
			Block block = entry;
			var blocks = new List<ControlNode>();
			while (true)
			{
				if (!todo.Remove(block)) throw new Exception("Block already generated");

				blocks.Add(new ControlNode.BlockNode(block));
				jumps.Remove(block);

				var last = block.Last;
				switch (last.Opcode)
				{
					case Opcode.Return:
						// End of line.
						return AddGroup(blocks, entry);
					case Opcode.Branch:
						{
							var next = ((Branch)last).Target;
							if (next.Previous.Count() > 1)
							{
								if (todo.Contains(next)) jumps.Add(next);
								blocks.Add(new ControlNode.JumpNode(next, block));
								return AddGroup(blocks, entry);
							}

							block = next;
							break;
						}
					case Opcode.BranchCondition:
						{
							var cond = ((BranchCondition)last);
							var branch = new ControlNode.IfNode(
								             CreateJump(cond.Success, block), 
								             CreateJump(cond.Failure, block)
							             );

							blocks.Add(branch);
							var next = branch.Next.ToSet();

							if (next.Count == 1)
							{
								var target = next.First();
								var pending = branch.Pending.ToSet();
								if (pending.SetEquals(target.Previous))
								{
									block = target;
									continue;
								}
							}

							return AddGroup(blocks, entry);
						}
					default:
						throw new Exception(last.Opcode + " is not a terminator");
				}
			}
		}

		private ControlGroup CreateJump(Block block, Block previous)
		{
			if (block.Previous.Count() > 1)
			{
				if (todo.Contains(block)) jumps.Add(block);
				return new ControlGroup(new List<ControlNode>(1) { new ControlNode.JumpNode(block, previous) }, null);
			}
			else
			{
				return CreateGroup(block);
			}
		}

		private ControlGroup AddGroup(List<ControlNode> blocks, Block entry)
		{
			var group = new ControlGroup(blocks, entry);
			groups.Add(entry, group);
			return group;
		}
	}
}

