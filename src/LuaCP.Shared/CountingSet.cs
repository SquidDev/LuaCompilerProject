using System;
using System.Collections.Generic;
using System.Linq;
using System.Collections;

namespace LuaCP
{
	public sealed class CountingSet<T> : ICollection<T>, IEnumerable<KeyValuePair<T, int>>
	{
		private readonly Dictionary<T, int> counts = new Dictionary<T, int>();

		public int UniqueCount { get { return counts.Count; } }

		public int TotalCount { get { return counts.Values.Sum(); } }

		public int this[T key]
		{
			get
			{
				int result;
				return counts.TryGetValue(key, out result) ? result : 0;
			}
		}

		public int Increment(T key)
		{
			int count = this[key] + 1;
			counts[key] = count;
			return count;
		}

		public int Decrement(T key)
		{
			int count = counts[key] - 1;
			if (count <= 0)
			{
				counts.Remove(key);
			}
			else
			{
				counts[key] = count;
			}
			return count;
		}

		#region ICollection

		int ICollection<T>.Count { get { return counts.Count; } }

		bool ICollection<T>.IsReadOnly { get { return false; } }

		void ICollection<T>.Add(T item)
		{
			Increment(item);
		}

		void ICollection<T>.Clear()
		{
			throw new InvalidOperationException();
		}

		bool ICollection<T>.Contains(T item)
		{
			return counts.ContainsKey(item);
		}

		void ICollection<T>.CopyTo(T[] array, int arrayIndex)
		{
			counts.Keys.CopyTo(array, arrayIndex);
		}

		bool ICollection<T>.Remove(T item)
		{
			int count = 0;
			if (counts.TryGetValue(item, out count))
			{
				count--;
				if (count <= 0)
				{
					counts.Remove(item);
				}
				else
				{
					counts[item] = count;
				}
				
				return true;
			}
			
			return false;
		}

		IEnumerator<T> IEnumerable<T>.GetEnumerator()
		{
			return counts.Keys.GetEnumerator();
		}

		IEnumerator<KeyValuePair<T, int>> IEnumerable<KeyValuePair<T, int>>.GetEnumerator()
		{
			return counts.GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return counts.Keys.GetEnumerator();
		}

		#endregion
	}
}
