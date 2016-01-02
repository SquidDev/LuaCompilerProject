using System;
using System.Collections.Generic;

namespace LuaCP.Collections
{
	public sealed class TypeDictionary<T>
	{
		private readonly Dictionary<Type, object> items = new Dictionary<Type, object>();
		private readonly T instance;

		public TypeDictionary(T instance)
		{
			this.instance = instance;
		}

		public TVal Get<TVal>(Func<T, TVal> getter)
		{
			Object cached;
			Type type = typeof(TVal);
			if (items.TryGetValue(type, out cached)) return (TVal)cached;

			TVal item = getter(instance);
			items.Add(type, item);
			return item;
		}
	}
}

