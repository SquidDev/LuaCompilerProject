using System;
using System.Collections.Generic;

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
			T val;
			return e.AllEqual(out val);
		}

		public static bool AllEqual<T>(this IEnumerable<T> e, out T first)
		{
			IEnumerator<T> enumerator = e.GetEnumerator();
			if (!enumerator.MoveNext())
			{
				first = default(T);
				return true;
			}

			first = enumerator.Current;
			while (enumerator.MoveNext())
			{
				if (!EqualityComparer<T>.Default.Equals(enumerator.Current, first)) return false;
			}

			return true;
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

		public static TVal GetOrAddDefault<TKey, TVal>(this IDictionary<TKey, TVal> dict, TKey key)
			where TVal : new()
		{
			TVal value;
			if (dict.TryGetValue(key, out value)) return value;

			value = new TVal();
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

		public static T[] Repeat<T>(this T item, int count)
		{
			T[] output = new T[count];
			for (int i = 0; i < count; i++) output[i] = item;
			return output;
		}

		public static HashSet<T> ToSet<T>(this IEnumerable<T> items)
		{
			return new HashSet<T>(items);
		}
	}
}

