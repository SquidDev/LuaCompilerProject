using System;
using System.Collections.Generic;
using System.Linq;

using LuaCP.IR;
using LuaCP.IR.Components;
using LuaCP.IR.Instructions;
using LuaCP.IR.User;
using LuaCP.Collections;

namespace LuaCP.Passes.Tools
{
	public abstract class BlockCloner
	{
		public abstract IValue GetValue(IValue original);

		public abstract void SetValue(IValue original, IValue replacement);

		protected abstract Block GetBlock(Block original);

		public virtual void CloneInstruction(Block toWrite, Instruction insn)
		{
			if (insn.Opcode.IsBinaryOperator())
			{
				BinaryOp op = (BinaryOp)insn;
				BinaryOp newOp = new BinaryOp(op.Opcode, GetValue(op.Left), GetValue(op.Right));

				SetValue(op, newOp);
				toWrite.AddLast(newOp);
			}
			else if (insn.Opcode.IsUnaryOperator())
			{
				UnaryOp op = (UnaryOp)insn;
				UnaryOp newOp = new UnaryOp(op.Opcode, GetValue(op.Operand));

				SetValue(op, newOp);
				toWrite.AddLast(newOp);
			}
			else
			{
				switch (insn.Opcode)
				{
					case Opcode.Branch:
						{
							Branch branch = (Branch)insn;
							toWrite.AddLast(new Branch(GetBlock(branch.Block)));
							break;
						}
					case Opcode.BranchCondition:
						{
							BranchCondition branch = (BranchCondition)insn;
							toWrite.AddLast(new BranchCondition(GetValue(branch.Test), GetBlock(branch.Success), GetBlock(branch.Failure)));
							break;
						}
					case Opcode.ValueCondition:
						{
							ValueCondition cond = (ValueCondition)insn;
							ValueCondition newCond = new ValueCondition(GetValue(cond.Test), GetValue(cond.Success), GetValue(cond.Failure));

							SetValue(cond, newCond);
							toWrite.AddLast(newCond);
							break;
						}
					case Opcode.Return:
						{
							Return ret = (Return)insn;
							toWrite.AddLast(new Return(GetValue(ret.Values)));
							break;
						}
					case Opcode.TableGet:
						{
							TableGet getter = (TableGet)insn;
							TableGet newGetter = new TableGet(GetValue(getter.Table), GetValue(getter.Key));

							SetValue(getter, newGetter);
							toWrite.AddLast(newGetter);
							break;
						}
					case Opcode.TableSet:
						{
							TableSet getter = (TableSet)insn;
							toWrite.AddLast(new TableSet(GetValue(getter.Table), GetValue(getter.Key), GetValue(getter.Value)));
							break;
						}
					case Opcode.TableNew:
						{
							TableNew creator = (TableNew)insn;
							TableNew newCreator = new TableNew(
								                      creator.ArrayPart,
								                      creator.HashPart.ToDictionary(x => GetValue(x.Key), x => GetValue(x.Key))
							                      );

							SetValue(creator, newCreator);
							toWrite.AddLast(newCreator);
							break;
						}
					case Opcode.Call:
						{
							Call call = (Call)insn;
							Call newCall = new Call(GetValue(call.Method), GetValue(call.Arguments));

							SetValue(call, newCall);
							toWrite.AddLast(newCall);
							break;
						}
					case Opcode.TupleNew:
						{
							TupleNew creator = (TupleNew)insn;
							TupleNew newCreator = new TupleNew(creator.Values.Select(GetValue), GetValue(creator.Remaining));
							SetValue(creator, newCreator);
							toWrite.AddLast(newCreator);
							break;
						}
					case Opcode.TupleGet:
						{
							TupleGet getter = (TupleGet)insn;
							TupleGet newGetter = new TupleGet(GetValue(getter.Tuple), getter.Offset);
							SetValue(getter, newGetter);
							toWrite.AddLast(newGetter);
							break;
						}
					case Opcode.TupleRemainder:
						{
							TupleRemainder getter = (TupleRemainder)insn;
							TupleRemainder newGetter = new TupleRemainder(GetValue(getter.Tuple), getter.Offset);
							SetValue(getter, newGetter);
							toWrite.AddLast(newGetter);
							break;
						}
					case Opcode.ReferenceGet:
						{
							ReferenceGet getter = (ReferenceGet)insn;
							ReferenceGet newGetter = new ReferenceGet(GetValue(getter.Reference));

							SetValue(getter, newGetter);
							toWrite.AddLast(newGetter);
							break;
						}
					case Opcode.ReferenceSet:
						{
							ReferenceSet setter = (ReferenceSet)insn;
							toWrite.AddLast(new ReferenceSet(GetValue(setter.Reference), GetValue(setter.Value)));
							break;
						}
					case Opcode.ReferenceNew:
						{
							ReferenceNew creator = (ReferenceNew)insn;
							ReferenceNew newCreator = new ReferenceNew(GetValue(creator.Value));

							SetValue(creator, newCreator);
							toWrite.AddLast(newCreator);
							break;
						}
					case Opcode.ClosureNew:
						{
							ClosureNew creator = (ClosureNew)insn;
							ClosureNew newCreator = new ClosureNew(
								                        creator.Function, 
								                        creator.OpenUpvalues.Select(GetValue), 
								                        creator.ClosedUpvalues.Select(GetValue)
							                        );

							SetValue(creator, newCreator);
							toWrite.AddLast(newCreator);
							break;
						}
					default:
						throw new Exception("Unknown opcode " + insn.Opcode);
				}
			}
		}

		public virtual void CloneBlock(Block block)
		{
			Block toWrite = GetBlock(block);
			foreach (Phi phi in block.PhiNodes)
			{
				SetValue(phi, new Phi(phi.Source.ToDictionary(x => GetBlock(x.Key), x => GetValue(x.Value)), toWrite));
			}

			foreach (Instruction insn in block)
			{
				CloneInstruction(toWrite, insn);
			}
		}

		/// <summary>
		/// A value that can be used for temporary value fetching
		/// </summary>
		protected sealed class TempValue : IValue
		{
			private readonly ValueKind kind;
			private readonly CountingSet<IUser<IValue>> users = new CountingSet<IUser<IValue>>();

			public TempValue(ValueKind kind)
			{
				this.kind = kind;
			}

			public ValueKind Kind { get { return kind; } }

			public CountingSet<IUser<IValue>> Users { get { return users; } }
		}
	}

	public class SingleBlockCloner : BlockCloner
	{
		private readonly Dictionary<IValue, IValue> valueLookup = new Dictionary<IValue, IValue>();

		public override void SetValue(IValue original, IValue replacement)
		{
			valueLookup.Add(original, replacement);
		}

		public override IValue GetValue(IValue original)
		{
			if (original is Constant) return original;

			IValue lookup;
			if (valueLookup.TryGetValue(original, out lookup)) return lookup;

			return original;
		}

		protected override Block GetBlock(Block original)
		{
			return original;
		}
	}

	public class FunctionCloner : BlockCloner
	{
		private readonly Dictionary<IValue, IValue> valueLookup = new Dictionary<IValue, IValue>();
		private readonly Dictionary<Block, Block> blockLookup;
		private readonly Function function;

		public Block EntryPoint { get { return blockLookup[function.EntryPoint]; } }

		public FunctionCloner(
			Function function, 
			Function target, 
			IList<IValue> args = null, 
			IList<IValue> openUpvalues = null,
			IList<IValue> closedUpvalues = null 
		)
		{
			this.function = function;
			
			if (args != null)
			{
				// TODO: Handle variable arguments
				for (int i = 0; i < args.Count; i++) valueLookup.Add(function.Arguments[i], args[i]);
			}
			
			if (closedUpvalues != null)
			{
				for (int i = 0; i < closedUpvalues.Count; i++) valueLookup.Add(function.ClosedUpvalues[i], closedUpvalues[i]);
			}
			
			if (openUpvalues != null)
			{
				for (int i = 0; i < openUpvalues.Count; i++) valueLookup.Add(function.OpenUpvalues[i], openUpvalues[i]);
			}
			
			blockLookup = new Dictionary<Block, Block>(function.Blocks.Count);
			foreach (Block block in function.Blocks) blockLookup.Add(block, new Block(target));
		}

		#region Item management

		public override void SetValue(IValue oldVal, IValue newVal)
		{
			IValue lookup;
			if (valueLookup.TryGetValue(oldVal, out lookup))
			{
				valueLookup[oldVal] = newVal;
				lookup.ReplaceWith(newVal);
			}
			else
			{
				valueLookup.Add(oldVal, newVal);
			}
		}

		public override IValue GetValue(IValue oldVal)
		{
			if (oldVal is Constant) return oldVal;
			
			IValue lookup;
			if (valueLookup.TryGetValue(oldVal, out lookup)) return lookup;
			
			lookup = new TempValue(oldVal.Kind);
			valueLookup.Add(oldVal, lookup);
			return lookup;
		}

		protected override Block GetBlock(Block block)
		{
			return blockLookup[block];
		}

		#endregion

		public virtual void Run()
		{
			foreach (Block block in function.Blocks)
			{
				CloneBlock(block);
			}

		}
	}
}
