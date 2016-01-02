using System;
using System.Collections.Generic;
using System.Linq;

namespace LuaCP.Collections
{
	public static class CollectionExtensions
	{
		public static bool IsEmpty<T>(this IEnumerable<T> t)
		{
			return !t.GetEnumerator().MoveNext();
		}

		public static Dictionary<TKey, TValOut> ToDictionary<TKey, TValOut, TValIn>(this IDictionary<TKey, TValIn> original)
			where TValIn : TValOut
		{
			Dictionary<TKey, TValOut> ret = new Dictionary<TKey, TValOut>(original.Count);
			foreach (KeyValuePair<TKey, TValIn> entry in original) ret.Add(entry.Key, entry.Value);
			return ret;
		}

		public static bool AllEqual<T>(this IEnumerable<T> e)
		{
			if (e.IsEmpty()) return true;
			T val = e.First();
			return e.All(x => EqualityComparer<T>.Default.Equals(x, val));
		}

		public static int FindIndex<T>(this IEnumerable<T> e, T item)
		{
			int i = 0;
			foreach (T element in e)
			{
				if (EqualityComparer<T>.Default.Equals(element, item)) return i;
				i++;
			}

			return -1;
		}

		public static TVal GetOrAddDefault<TKey, TVal>(this IDictionary<TKey, TVal> dict, TKey key, Func<TVal> creator)
		{
			TVal value;
			if (dict.TryGetValue(key, out value)) return value;

			value = creator();
			dict.Add(key, value);
			return value;

		}

		public static T Last<T>(this IReadOnlyList<T> list)
		{
			return list[list.Count - 1];
		}

		public static void Populate<T>(this IList<T> list, IEnumerable<T> items, int starting = 0)
		{
			foreach (T item in items)
			{
				list[starting] = item;
				starting++;
			}
		}
	}
}

