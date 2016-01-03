using System;
using LuaCP.IR.Components;
using System.Collections.Generic;
using System.Linq;
using LuaCP.Collections;
using LuaCP.Graph;
using LuaCP.IR.Instructions;
using LuaCP.IR;
using LuaCP.IR.User;

namespace LuaCP.CodeGen.Bytecode
{
	public class BytecodeCodegen
	{
		private class ValueReference
		{
			private int users;
			public readonly Register Register;
			private readonly BytecodeCodegen gen;

			public ValueReference(IValue value, BytecodeCodegen gen)
			{
				users = value.Users.TotalCount;
				this.gen = gen;

				int min = gen.stack.Min;
				Register = new Register(min);
				gen.stack.Remove(min);

				gen.registers.Add(value, this);
			}

			public Register Use()
			{
				users--;
				if (users == 0) gen.stack.Add(Register.Index);

				return Register;
			}

			public void Merge(IValue value)
			{
				gen.registers.Add(value, this);
				users += value.Users.TotalCount;
			}
		}

		private readonly IBytecodeWriter writer;
		private readonly Function function;
		private readonly SortedSet<int> stack = new SortedSet<int>(Enumerable.Range(0, 255));
		private readonly Dictionary<Block, Label> blocks = new Dictionary<Block, Label>();
		private readonly Dictionary<IValue, ValueReference> registers = new Dictionary<IValue, ValueReference>();
		private readonly Dictionary<Upvalue, int> upvalIndexes = new Dictionary<Upvalue, int>();

		public BytecodeCodegen(IBytecodeWriter writer, Function function)
		{
			this.writer = writer;
			this.function = function;

			int index = 0;
			foreach (Upvalue up in function.Upvalues)
			{
				upvalIndexes.Add(up, index++);
			}
		}

		private ILuaValue Get(IValue value)
		{
			if (value is Constant) return new LuaConstant((Constant)value);
			if (value is Upvalue)
			{
				Register register = new Register(stack.Min);
				writer.GetUpvalue(upvalIndexes[(Upvalue)value], register);
				return register;
			}

			return registers[value].Use();
		}

		private Register GetRegister(IValue value)
		{
			ILuaValue val = Get(value);
			Register reg = val as Register;
			if (reg == null)
			{
				reg = new Register(stack.Min);
				writer.LoadK((LuaConstant)val, reg);
			}

			return reg;
		}

		private Register MakeRegister(IValue value)
		{
			return new ValueReference(value, this).Use();
		}

		private void WriteBlock(Block block)
		{
			writer.Label(blocks.GetOrAddDefault(block));

			foreach (Phi phi in block.DominatorTreeChildren.SelectMany(x => x.PhiNodes))
			{
				registers.Add(phi, new ValueReference(phi, this));
			}

			Instruction insn = block.First;
			while (insn != null)
			{
				if (insn.Opcode.IsBinaryOperator())
				{
					BinaryOp op = (BinaryOp)insn;
					if (insn.Opcode >= Opcode.Equals)
					{
						// TODO: Optimise for conditional branch
						Label success = new Label();
						writer.Comparison(insn.Opcode, true, Get(op.Left), Get(op.Right), success);

						Register reg = MakeRegister(op);
						writer.LoadBoolean(false, true, reg);

						writer.Label(success);
						writer.LoadBoolean(true, false, reg);
					}
					else
					{
						writer.BinaryOperator(op.Opcode, Get(op.Left), Get(op.Right), MakeRegister(op));
					}
				}
				else if (insn.Opcode.IsUnaryOperator())
				{
					UnaryOp op = (UnaryOp)insn;
					writer.UnaryOperator(op.Opcode, Get(op.Operand), MakeRegister(op));
				}
				else
				{
					switch (insn.Opcode)
					{
						case Opcode.Branch:
							{
								Branch branch = (Branch)insn;
								// TODO: Inline blocks, write phis
								foreach (Phi phi in branch.Target.PhiNodes)
								{
									writer.Load(Get(phi.Source[block]), registers[phi].Register);
								}
								writer.Jump(blocks.GetOrAddDefault(branch.Target));
								break;
							}
						case Opcode.BranchCondition:
							{
								BranchCondition branchCond = (BranchCondition)insn;
								// TODO: Inline blocks, write phis
								Label failureTarget = branchCond.Failure.PhiNodes.Count == 0 ? blocks.GetOrAddDefault(branchCond.Failure) : new Label();
								writer.Test(true, Get(branchCond.Test), failureTarget);

								// Success
								foreach (Phi phi in branchCond.Success.PhiNodes)
								{
									writer.Load(Get(phi.Source[block]), registers[phi].Register);
								}
								writer.Jump(blocks.GetOrAddDefault(branchCond.Success));

								// Failure
								if (branchCond.Failure.PhiNodes.Count > 0)
								{
									writer.Label(failureTarget);
									foreach (Phi phi in branchCond.Failure.PhiNodes)
									{
										writer.Load(Get(phi.Source[block]), registers[phi].Register);
									}
									writer.Jump(blocks.GetOrAddDefault(branchCond.Failure));
								}
								break;
							}
						case Opcode.ValueCondition:
							{
								ValueCondition valueCond = (ValueCondition)insn;

								ILuaValue test = Get(valueCond.Test);
								ILuaValue success = Get(valueCond.Success);
								ILuaValue failure = Get(valueCond.Failure);
								Register result = MakeRegister(valueCond);

								Label cont = new Label();
								if (valueCond.Test == valueCond.Success)
								{
									writer.TestSet(true, test, cont, result);

									writer.Load(failure, result);
								}
								else if (valueCond.Test == valueCond.Failure)
								{
									writer.TestSet(false, test, cont, result);

									writer.Load(success, result);
								}
								else
								{
									Label suc = new Label();
									writer.Test(true, test, suc);

									writer.Load(failure, result);
									writer.Jump(cont);

									writer.Label(suc);
									writer.Load(success, result);
								}

								writer.Label(cont);
								break;
							}
						case Opcode.Return:
							{
								Return ret = (Return)insn;
								if (ret.Values.IsNil())
								{
									writer.Return(Register.PseudoRegister, 1);	
								}
								else if (ret.Values.Kind != ValueKind.Tuple)
								{
									writer.Return(GetRegister(ret.Values), 2);
								}
								else
								{
									TupleNew tup = ret.Values as TupleNew;
									if (tup != null && tup.Remaining.IsNil())
									{
										writer.Return(GetRegister(ret.Values), tup.Values.Count + 1);
									}
									else
									{
										writer.Return(GetRegister(ret.Values), 0);
									}
								}
								break;
							}
						case Opcode.TableGet:
							{
								TableGet getter = (TableGet)insn;
								ReferenceGet reference = getter.Table as ReferenceGet;
								if (reference != null && reference.Reference is Upvalue)
								{
									Upvalue upvalue = (Upvalue)reference.Reference;
									writer.GetTableUpvalue(upvalIndexes[upvalue], Get(getter.Key), MakeRegister(getter));
								}
								else if (getter.Table is Upvalue)
								{
									Upvalue upvalue = (Upvalue)getter.Table;
									writer.GetTableUpvalue(upvalIndexes[upvalue], Get(getter.Key), MakeRegister(getter));
								}
								else
								{
									writer.GetTable(Get(getter.Table), Get(getter.Key), MakeRegister(getter));
								}
								break;
							}
						case Opcode.TableSet:
							{
								TableSet setter = (TableSet)insn;
								if (setter.Table is Upvalue)
								{
									Upvalue upvalue = (Upvalue)setter.Table;
									writer.SetTableUpvalue(upvalIndexes[upvalue], Get(setter.Key), Get(setter.Value));
								}
								else
								{
									writer.SetTable(Get(setter.Table), Get(setter.Key), Get(setter.Value));
								}
								break;
							}
						case Opcode.TableNew:
							{
								//TODO: Implement me
								break;
							}
						case Opcode.Call:
							{
								// TODO: Lots of tuple goodness!
								break;
							}
						case Opcode.ReferenceGet:
							{
								ReferenceGet getter = (ReferenceGet)insn;
								Instruction next = insn.Next;

								if (getter.Reference is Upvalue)
								{
									// Upvalue getting/setting is handled in the tableget/tableset instructions
									if (next.Opcode == Opcode.TableGet)
									{
										TableGet tGetter = (TableGet)next;
										if (tGetter.Table == getter) break;
									}
									else if (next.Opcode == Opcode.TableSet)
									{
										TableSet tSetter = (TableSet)next;
										if (tSetter.Table == getter) break;
									}
									else
									{
										// Otherwise just save it
										writer.GetUpvalue(upvalIndexes[(Upvalue)getter.Reference], MakeRegister(getter));
									}
								}
								else
								{
									if (getter.Users.UniqueCount == 1 && getter.Users.First<IUser<IValue>>() == next)
									{
										// We're not backing up the value, to no need to move
										// TODO: Ensure better checking.
										registers[getter.Reference].Merge(getter);
									}
									else
									{
										writer.Move(GetRegister(getter.Reference), MakeRegister(getter));
									}
								}
								break;
							}
						default:
							Console.WriteLine("Cannot handle {0}", insn.Opcode);
							break;
							
					}
				}

				insn = insn.Next;
			}
		}

		public void Write()
		{
			foreach (Block block in function.EntryPoint.ReachableLazy())
			{
				WriteBlock(block);
			}
		}
	}
}

