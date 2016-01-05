using System;
using System.Collections.Generic;

namespace LuaCP.Collections
{
	public class ValidDictionary<T>
	{
		private readonly Dictionary<Type, IValid> items = new Dictionary<Type, IValid>();
		private readonly T instance;

		public ValidDictionary(T instance)
		{
			this.instance = instance;
		}

		public TVal Evaluate<TVal>(Func<T, TVal> getter)
		{
			IValid cached;
			if (items.TryGetValue(typeof(TVal), out cached))
			{
				Valid<TVal> val = (Valid<TVal>)cached;
				return val.Evaluate();
			}

			Valid<TVal> item = new Valid<TVal>(() => getter(instance));
			items.Add(typeof(TVal), item);
			return item.Evaluate();
		}

		public void Invalidate<TVal>()
		{
			IValid item;
			if (items.TryGetValue(typeof(TVal), out item))
			{
				item.Invalidate();
			}
		}

		public void Invalidate()
		{
			foreach (IValid valid in items.Values) valid.Invalidate();
		}
	}
}

