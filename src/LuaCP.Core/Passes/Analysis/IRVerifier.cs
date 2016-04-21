using LuaCP.IR;
using LuaCP.IR.Components;
using LuaCP.IR.Instructions;
using LuaCP.IR.User;
using LuaCP.Graph;
using System.Linq;
using System.IO;
using LuaCP.Debug;
using System;

namespace LuaCP.Passes.Analysis
{
	/// <summary>
	/// Several basic valiation methods.
	/// Checks:
	///  - Values have been defined when used
	///  - Every block has a terminator
	///  - Values are in correct function
	/// </summary>
	public static class IRVerifier
	{
		public const string WrongFunction = "Value {0} belongs to {1}, is in {2}";
		public const string UndefinedValue = "Value {0} is used in {1} before it is declared in {2}";
		public const string UnexpectedType = "Value {0} is a {1}, expected {2}";
		public const string UnexpectedNil = "Value cannot be nil";

		public const string NoTerminator = "Block has no terminator";

		public interface IMessager
		{
			void InvalidInstruction(Instruction user, string format, params object[] args);

			void InvalidBlock(Block block, string format, params object[] args);

			bool HasErrors { get; }
		}

		public sealed class WriterMessager : IMessager
		{
			private readonly TextWriter writer;
			private readonly NodeNumberer numberer;
			private readonly IRFormatProvider provider;

			public bool HasErrors { get; private set; }

			public WriterMessager(TextWriter writer, Function function)
			{
				this.writer = writer;
				numberer = new NodeNumberer(function);
				provider = new IRFormatProvider(numberer);
			}

			public void InvalidInstruction(Instruction user, string format, params object[] args)
			{
				Formatter.Default.InstructionLong(user, writer, numberer);
				writer.WriteLine(": " + String.Format(provider, format, args));
				HasErrors = true;
			}

			public void InvalidBlock(Block block, string format, params object[] args)
			{
				writer.WriteLine(numberer.PrettyGetBlock(block) + ": " + String.Format(provider, format, args));
				HasErrors = true;
			}
		}

		/// <summary>
		/// Checks values are valid:
		///  - Have been defined (Phis, Instructions)
		///  - Are in correct function (Upvalue, Argument, Instruction, Phi)
		/// </summary>
		/// <param name="value">Value to check</param>
		/// <param name="user">Instruction it is used at</param>
		/// <param name="messager">The messager to report to</param>
		public static void ValidateValue(IValue value, Instruction user, IMessager messager)
		{
			if (value is Constant) return;

			if (value is IBelongs<Function>)
			{
				var item = (IBelongs<Function>)value;
				if (item.Owner != user.Block.Function)
				{
					messager.InvalidInstruction(user, WrongFunction, value, item.Owner, user.Block.Function);
				}
			}
			else if (value is IBelongs<Block>)
			{
				var item = (IBelongs<Block>)value;
				if (item.Owner.Function != user.Block.Function)
				{
					messager.InvalidInstruction(user, WrongFunction, value, item.Owner.Function, user.Block.Function);
				}
					
				if (!item.Owner.Dominates(user.Block))
				{
					messager.InvalidInstruction(user, UndefinedValue, value, user.Block, item.Owner);
				}
			}

			if (value is Instruction)
			{
				Instruction used = (Instruction)value;
				// We only need to check the same block
				if (used.Block == user.Block)
				{
					Instruction first = used.Block.First(x => x == used || x == user);
					if (first == user) messager.InvalidInstruction(user, UndefinedValue, value, used.Block, "the same block");
				}
			}
		}

		public static void ValidateInstruction(Instruction insn, IMessager messager)
		{
			if (insn.Opcode.IsBinaryOperator())
			{
				BinaryOp binOp = (BinaryOp)insn;
				messager.CheckType(binOp, binOp.Left, ValueKind.Value);
				messager.CheckType(binOp, binOp.Right, ValueKind.Value);

				if (insn.Opcode != Opcode.Equals)
				{
					messager.CheckNotNil(binOp, binOp.Left);
					messager.CheckNotNil(binOp, binOp.Right);
				}
			}
			else if (insn.Opcode.IsUnaryOperator())
			{
				UnaryOp unOp = (UnaryOp)insn;
				messager.CheckType(unOp, unOp.Operand, ValueKind.Value);
				messager.CheckNotNil(unOp, unOp.Operand);
			}

			// TODO: Validate other instructions
		}

		public static void ValidateBlock(Block block, IMessager messager)
		{
			Instruction last = block.Last;
			if (last == null || !last.Opcode.IsTerminator()) messager.InvalidBlock(block, NoTerminator);

			foreach (Instruction insn in block)
			{
				IUser<IValue> user = insn as IUser<IValue>;
				if (user != null)
				{
					foreach (IValue value in user.GetUses())
					{
						ValidateValue(value, insn, messager);
					}
				}
			}
		}

		private static void CheckType(this IMessager messager, Instruction user, IValue value, ValueKind kind)
		{
			if (value.Kind != kind)
			{
				messager.InvalidInstruction(user, UnexpectedType, value, value.Kind, kind);
			}
		}

		private static void CheckNotNil(this IMessager messager, Instruction user, IValue value)
		{
			if (value.IsNil()) messager.InvalidInstruction(user, UnexpectedNil);
		}

		public static void Run(Function function, IMessager messager)
		{
			function.Dominators.Invalidate(); // FIXME: Ensure that all passes do this anyway.
			function.Dominators.Evaluate();
			foreach (Block block in function.EntryPoint.ReachableLazy())
			{
				ValidateBlock(block, messager);
			}
		}
	}

	public class VerificationException : Exception
	{
		public VerificationException(string message)
			: base(message)
		{
		}
	}
}

