using System.Collections.Generic;
using LuaCP.IR.Components;
using LuaCP.IR.User;

namespace LuaCP.IR.Instructions
{
	public sealed class Branch : Instruction, IUser<Block>
	{
		private Block target;

		public Block Target
		{
			get { return target; }
			set { target = UserExtensions.Replace(this, target, value); }
		}

		public Branch(Block target)
			: base(Opcode.Branch)
		{
			Target = target;
		}

		public IEnumerable<Block> GetUses()
		{
			yield return target;
		}

		public void Replace(Block original, Block replace)
		{
			if (target == original) Target = replace;
		}

		public override void ForceDestroy()
		{
			target.Users.Decrement(this);
			target = null;
		}
	}

	public sealed class BranchCondition : Instruction, IUser<Block>, IUser<IValue>
	{
		private IValue test;

		public IValue Test
		{
			get { return test; }
			set { test = UserExtensions.Replace(this, test, value); }
		}

		private Block success;

		public Block Success
		{
			get { return success; }
			set { success = UserExtensions.Replace(this, success, value); }
		}

		private Block failure;

		public Block Failure
		{
			get { return failure; }
			set { failure = UserExtensions.Replace(this, failure, value); }
		}

        
		public BranchCondition(IValue test, Block success, Block failure)
			: base(Opcode.BranchCondition)
		{
			Test = test;
			Success = success;
			Failure = failure;
		}

		IEnumerable<Block> IUser<Block>.GetUses()
		{
			yield return success;
			yield return failure;
		}

		void IUser<Block>.Replace(Block original, Block replace)
		{
			if (success == original) Success = replace;
			if (failure == original) Failure = replace;
		}

		IEnumerable<IValue> IUser<IValue>.GetUses()
		{
			yield return Test;
		}

		void IUser<IValue>.Replace(IValue original, IValue replace)
		{
			if (test == original) Test = replace;
		}

		public override void ForceDestroy()
		{
			test.Users.Decrement(this);
			test = null;

			success.Users.Decrement(this);
			success = null;

			failure.Users.Decrement(this);
			failure = null;
		}
	}

	public sealed class Return : Instruction, IUser<IValue>
	{
		private IValue values;

		public IValue Values
		{ 
			get { return values; } 
			set { values = UserExtensions.Replace(this, values, value); }
		}

		public Return(IValue values)
			: base(Opcode.Return)
		{
			Values = values;
		}

		public IEnumerable<IValue> GetUses()
		{
			yield return values;
		}

		public void Replace(IValue original, IValue replace)
		{
			if (values == original) Values = replace;
		}

		public override void ForceDestroy()
		{
			values.Users.Decrement(this);
			values = null;
		}
	}

	public sealed class ValueCondition : ValueInstruction, IUser<IValue>
	{
		private IValue test;

		public IValue Test
		{
			get { return test; }
			set { test = UserExtensions.Replace(this, test, value); }
		}

		private IValue success;

		public IValue Success
		{
			get { return success; }
			set { success = UserExtensions.Replace(this, success, value); }
		}

		private IValue failure;

		public IValue Failure
		{
			get { return failure; }
			set { failure = UserExtensions.Replace(this, failure, value); }
		}

		public ValueCondition(IValue test, IValue success, IValue fail)
			: base(Opcode.ValueCondition, ValueKind.Value)
		{
			Test = test;
			Success = success;
			Failure = fail;
		}

		public IEnumerable<IValue> GetUses()
		{
			yield return test;
			yield return success;
			yield return failure;
		}

		public void Replace(IValue original, IValue replace)
		{
			if (test == original) Test = replace;
			if (success == original) Success = replace;
			if (failure == original) Failure = replace;
		}

		public override void ForceDestroy()
		{
			test.Users.Decrement(this);
			test = null;

			success.Users.Decrement(this);
			success = null;

			failure.Users.Decrement(this);
			failure = null;
		}
	}
}
