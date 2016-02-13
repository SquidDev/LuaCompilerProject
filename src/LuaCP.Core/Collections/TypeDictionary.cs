using System;
using System.Collections.Generic;
using System.Collections;

namespace LuaCP.Collections
{
	public class TypeDictionary<T, TItem> : IEnumerable<TItem>
	{
		protected readonly Dictionary<Type, TItem> Items = new Dictionary<Type, TItem>();
		protected readonly T Instance;

		public TypeDictionary(T instance)
		{
			Instance = instance;
		}

		public TVal Get<TVal>(Func<T, TVal> getter)
			where TVal : TItem
		{
			TItem cached;
			Type type = typeof(TVal);
			if (Items.TryGetValue(type, out cached)) return (TVal)cached;

			TVal item = getter(Instance);
			Items.Add(type, item);
			return item;
		}

		public void Add<TVal>(TVal val)
			where TVal : TItem
		{
			Items.Add(typeof(TVal), val);
		}

		public IEnumerator<TItem> GetEnumerator()
		{
			return Items.Values.GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return Items.Values.GetEnumerator();
		}
	}

	public sealed class TypeDictionary<T> : TypeDictionary<T, object>
	{
		public TypeDictionary(T instance)
			: base(instance)
		{
		}
	}
}

