using System;
using System.Collections.Generic;

namespace LuaCP.CodeGen
{
	public class NameAllocator
	{
		private int counter = -1;
		private readonly string format;

		public NameAllocator(string format)
		{
			this.format = format;
		}

		public string Next()
		{
			return String.Format(format, ++counter);
		}
	}

	public class NameAllocator<T>
	{
		private readonly Dictionary<T, String> lookup = new Dictionary<T, String>();
		private readonly NameAllocator allocator;

		public NameAllocator(string format)
		{
			allocator = new NameAllocator(format);
		}

		public string this[T key]
		{
			get
			{
				string name;
				if (lookup.TryGetValue(key, out name)) return name;

				name = allocator.Next();
				lookup.Add(key, name);
				return name;
			} 

			set
			{
				lookup.Add(key, value);
			}
		}

		public IReadOnlyDictionary<T, String> Lookup { get { return lookup; } }
	}
}

