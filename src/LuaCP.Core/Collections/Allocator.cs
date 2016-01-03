using System.Collections.Generic;

namespace LuaCP.Collections
{
	public class Allocator<T>
	{
		private readonly Dictionary<T, int> items = new Dictionary<T, int>();
		private int counter = 0;

		public int this[T key]
		{
			get
			{
				int index;
				if (items.TryGetValue(key, out index)) return index;
				index = counter;
				counter++;
				items.Add(key, index);
				return index;
			}
		}

		public void Add(T key)
		{
			int x = this[key];
		}
	}
}

