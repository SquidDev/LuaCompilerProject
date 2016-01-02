using System;
using System.Collections.Generic;

namespace LuaCP
{
	public class Valid<T>
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
	}

	public class Valid
	{
		private bool valid = false;
		private Action invalidate;
		private Action create;

		public Valid(Action invalidate, Action evaluate)
		{
			this.create = evaluate;
			this.invalidate = invalidate;
		}

		public void Evaluate()
		{
			if (!valid)
			{
				valid = true;
				create();
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
    
	public class ValidDictionary<T>
	{
		private readonly Dictionary<Type, object> items = new Dictionary<Type, object>();
		private readonly T instance;
    	
		public ValidDictionary(T instance)
		{
			this.instance = instance;
		}
    	
		public TVal Evaluate<TVal>(Func<T, TVal> getter)
		{
			Object cached;
			Type type = typeof(TVal);
			if (items.TryGetValue(type, out cached)) return (TVal)cached;

			TVal item = getter(instance);
			items.Add(type, item);
			return item;
		}
    	
		public void Invalidate<TVal>()
		{
			items.Remove(typeof(TVal));
		}
	}
}

