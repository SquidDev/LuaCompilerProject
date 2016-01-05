using System.Collections.Generic;

namespace LuaCP.Collections
{
	public sealed class Allocator<T>
	{
		private readonly Dictionary<T, int> items = new Dictionary<T, int>();
		private int counter = 0;

		public int this[T key]
		{
			get { return Get(key); }
		}

		public void Add(T key)
		{
			Get(key);
		}

		private int Get(T key)
		{
			int index;
			if (items.TryGetValue(key, out index)) return index;
			index = counter;
			counter++;
			items.Add(key, index);
			return index;
		}
	}
}

