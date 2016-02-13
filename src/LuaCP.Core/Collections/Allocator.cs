using System;
using System.Collections.Generic;
using System.Text;

namespace LuaCP.Collections
{
	public class Allocator<T>
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

	public class StringAllocator<T>
	{
		private readonly Dictionary<T, string> items = new Dictionary<T, string>();
		private int counter = 0;

		public string this[T key]
		{
			get { return Get(key); }
		}

		public void Add(T key)
		{
			Get(key);
		}

		private string Get(T key)
		{
			string contents;
			if (items.TryGetValue(key, out contents)) return contents;

			StringBuilder builder = new StringBuilder();
			int current = counter;
			while (current > 0)
			{
				builder.Append((char)((current % 26) + 'a'));
				if (current >= 26)
				{
					current /= 26;
				}
				else
				{
					break;
				}
			}

			counter++;
			items.Add(key, contents);
			return contents;
		}
	}
}

