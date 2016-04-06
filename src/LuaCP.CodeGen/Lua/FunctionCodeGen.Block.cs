using System;
using System.Collections.Generic;
using System.Linq;
using LuaCP.IR;
using LuaCP.IR.Components;
using LuaCP.IR.Instructions;
using LuaCP.Passes.Analysis;

namespace LuaCP.CodeGen.Lua
{
	public sealed partial class FunctionCodeGen
	{
		private void WriteGroup(ControlGroup group, ControlNode next)
		{
			var nodes = group.Nodes;
			var children = group.Children;
			for (int i = 0; i < nodes.Count; i++)
			{
				var node = nodes[i];
				switch (node.Kind)
				{
					case ControlNode.ControlKind.Block:
						{
							Block block = ((ControlNode.BlockNode)node).Block;

							// We we have an if next then pass that instead
							if (i + 1 < nodes.Count)
							{
								var ifNode = nodes[i + 1];
								if (ifNode.Kind == ControlNode.ControlKind.If)
								{
									node = ifNode;
									i++;
								}
							}

							ControlNode nextNode;
							if (i + 1 < nodes.Count)
							{
								nextNode = nodes[i + 1];
							}
							else if (children.Count > 0)
							{
								nextNode = children[0].Nodes[0];
							}
							else
							{
								nextNode = next;
							}

							WriteBlock(block, node, nextNode);
							break;
						}
					case ControlNode.ControlKind.Jump:
						{
							ControlNode nextNode;
							if (i + 1 < nodes.Count)
							{
								nextNode = nodes[i + 1];
							}
							else if (children.Count > 0)
							{
								nextNode = children[0].Nodes[0];
							}
							else
							{
								nextNode = next;
							}

							Block target = ((ControlNode.JumpNode)node).Target;
							Block nextBlock = nextNode != null && nextNode.Kind == ControlNode.ControlKind.Block ? ((ControlNode.BlockNode)nextNode).Block : null;
							if (nextBlock != target) writer.WriteLine("goto " + blocks[target]);
							break;
						}
					default:
						throw new InvalidOperationException("Unexpected " + node.Kind);
				}
			}

			for (int i = 0; i < children.Count; i++)
			{
				var child = children[i];
				ControlNode nextNode;
				nextNode = i + 1 < children.Count - 1 ? children[i + 1].Nodes[0] : next;

				writer.WriteLine("do");
				writer.Indent++;
				WriteGroup(child, nextNode);
				writer.Indent--;
				writer.WriteLine("end");
			}
		}

		private void WriteBlock(Block block, ControlNode node, ControlNode next)
		{
			if (block != null && block.Previous.Count() > 1) writer.WriteLine("::{0}::", blocks[block]);

			Instruction insn = block.First;
			while (insn != null)
			{
				var value = insn as ValueInstruction;
				if (value != null && value.Kind != ValueKind.Tuple)
				{
					WriteValue(value);
				}
				else
				{
					switch (insn.Opcode)
					{
						case Opcode.Branch:
							{
								Branch branch = (Branch)insn;
								WritePhis(block, branch.Target);
								break;
							}
						case Opcode.BranchCondition:
							{
								BranchCondition branchCond = (BranchCondition)insn;
								var ifNode = (ControlNode.IfNode)node;

								writer.WriteLine("if {0} then", Format(branchCond.Test));

								writer.Indent++;
								WritePhis(block, branchCond.Success);
								WriteGroup(ifNode.Success, next);
								writer.Indent--;

								writer.WriteLine("else");

								writer.Indent++;
								WritePhis(block, branchCond.Failure);
								WriteGroup(ifNode.Failure, next);
								writer.Indent--;

								writer.WriteLine("end");
								break;
							}
						case Opcode.TableSet:
							{
								TableSet getter = (TableSet)insn;
								writer.WriteLine("{0}{1} = {2}", Format(getter.Table), FormatKey(getter.Key), Format(getter.Value));
								break;
							}
						case Opcode.ReferenceSet:
							{
								ReferenceSet setter = (ReferenceSet)insn;
								writer.WriteLine("{0} = {1}", Format(setter.Reference), Format(setter.Value));
								break;
							}
						case Opcode.Call:
						case Opcode.TupleGet:	
						case Opcode.TupleNew:	
						case Opcode.TupleRemainder:	
						case Opcode.Return:	
							insn = WriteTuples(insn);
							break;
						default:
							throw new ArgumentException("Unknown opcode " + insn.Opcode);
					}
				}

				insn = insn.Next;
			}
		}

		private void WritePhis(Block source, Block target)
		{
			foreach (Phi phi in target.PhiNodes)
			{
				if (phi.Kind == ValueKind.Tuple)
				{
					// TODO: Handle tuple phis somehow.
					writer.WriteLine("-- ERROR: Tuple phi");
				}
				else
				{
					string name = GetName(phi);
					string sourceName = Format(phi.Source[source]);
					if (name != sourceName) writer.WriteLine("{0} = {1}", name, sourceName);
				}
			}
		}
	}
}

