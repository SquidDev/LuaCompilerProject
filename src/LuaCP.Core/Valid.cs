using System;

namespace LuaCP
{
	public interface IValid
	{
		void Evaluate();

		void Invalidate();
	}

	public class Valid<T> : IValid
	{
		private T instance;
		private bool valid = false;
		private Func<T> create;

		public Valid(Func<T> create)
		{
			this.create = create;
		}

		public T Evaluate()
		{
			if (valid) return instance;
			valid = true;
			return instance = create();
		}

		public void Invalidate()
		{
			valid = false;
		}

		void IValid.Evaluate()
		{
			Evaluate();
		}
	}

	public class Valid : IValid
	{
		private bool valid = false;
		private readonly Action invalidate;
		private readonly Action evaluate;

		public Valid(Action invalidate, Action evaluate)
		{
			this.evaluate = evaluate;
			this.invalidate = invalidate;
		}

		public void Evaluate()
		{
			if (!valid)
			{
				valid = true;
				evaluate();
			}
		}

		public void Invalidate()
		{
			if (valid)
			{
				valid = false;
				invalidate();
			}
		}
	}
}

