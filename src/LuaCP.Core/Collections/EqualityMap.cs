using System;
using System.Collections.Generic;
using System.Linq;

namespace LuaCP.Collections
{
	/// <summary>
	/// Maps a list of items to be equal
	/// </summary>
	public class EqualityMap<T>
	{
		private readonly Dictionary<T, HashSet<T>> equality = new Dictionary<T, HashSet<T>>();

		public void UnionWith<TOther>(EqualityMap<TOther> others, Func<TOther, T> map)
		{
			var lookup = new Dictionary<HashSet<TOther>, HashSet<T>>();
			foreach (var other in others.equality)
			{
				equality.Add(map(other.Key), lookup.GetOrAddDefault(other.Value, x => x.Select(map).ToSet()));
			}
		}

		public void Equate(T left, T right)
		{
			HashSet<T> leftSet, rightSet;
			bool hasLeft = equality.TryGetValue(left, out leftSet);
			bool hasRight = equality.TryGetValue(right, out rightSet);

			if (hasLeft && hasRight)
			{
				if (leftSet != rightSet)
				{
					foreach (T item in rightSet)
					{
						equality[item] = leftSet;
					}
					leftSet.UnionWith(rightSet);
				}
			}
			else if (hasLeft)
			{
				equality.Add(right, leftSet);
				leftSet.Add(right);
			}
			else if (hasRight)
			{
				equality.Add(left, rightSet);
				rightSet.Add(left);
			}
			else
			{
				var equalSet = new HashSet<T>() { left, right };
				equality.Add(left, equalSet);
				equality.Add(right, equalSet);
			}
		}

		public bool AreEqual(T left, T right)
		{
			HashSet<T> equalSet;
			return equality.TryGetValue(left, out equalSet) && equalSet.Contains(right);
		}

		public IEnumerable<T> GetEqual(T item)
		{
			HashSet<T> equalSet;
			return equality.TryGetValue(item, out equalSet) ? equalSet.Where(x => !EqualityComparer<T>.Default.Equals(x, item)) : Enumerable.Empty<T>();
		}
	}
}

