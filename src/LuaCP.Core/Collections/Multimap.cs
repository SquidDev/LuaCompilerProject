using System.Collections;
using System.Collections.Generic;

namespace LuaCP.Collections
{
	public class Multimap<TKey, TValue> : IDictionary<TKey, ISet<TValue>>
	{
		private readonly IDictionary<TKey, ISet<TValue>> items = new Dictionary<TKey, ISet<TValue>>();

		public ISet<TValue> this[TKey key]
		{
			get
			{ 
				ISet<TValue> value;
				if (!items.TryGetValue(key, out value))
				{
					value = new HashSet<TValue>();
					items.Add(key, value);
				}
				return value; 
			}

			set { items[key] = value; }
		}

		public int Count
		{
			get { return items.Count; }
		}

		public bool IsReadOnly
		{
			get { return false; }
		}

		public ICollection<TKey> Keys
		{
			get { return items.Keys; }
		}

		public ICollection<ISet<TValue>> Values
		{
			get { return items.Values; }
		}

		public void Add(KeyValuePair<TKey, ISet<TValue>> item)
		{
			Add(item.Key, item.Value);
		}

		public void Add(TKey key, ISet<TValue> values)
		{
			ISet<TValue> current = this[key];
			foreach (TValue value in values)
			{
				current.Add(value);
			}
		}
		
		public void Add(TKey key, TValue value)
		{
			this[key].Add(value);
		}

		public void Clear()
		{
			items.Clear();
		}

		public bool Contains(KeyValuePair<TKey, ISet<TValue>> item)
		{
			ISet<TValue> current;
			return items.TryGetValue(item.Key, out current) ? item.Value.IsSubsetOf(current) : false;
		}
		
		public bool Contains(TKey key, TValue value)
		{
			ISet<TValue> current;
			return items.TryGetValue(key, out current) ? current.Contains(value) : false;
		}

		public bool ContainsKey(TKey key)
		{
			return items.ContainsKey(key);
		}

		public void CopyTo(KeyValuePair<TKey, ISet<TValue>>[] array, int arrayIndex)
		{
			items.CopyTo(array, arrayIndex);
		}

		public bool Remove(KeyValuePair<TKey, ISet<TValue>> item)
		{
			ISet<TValue> current;
			if (items.TryGetValue(item.Key, out current))
			{
				bool changed = true;
				foreach (TValue value in item.Value)
				{
					changed &= current.Remove(value);
				}
                
				return changed;
			}
			
			return false;
		}

		public bool Remove(TKey key)
		{
			return items.Remove(key);
		}
		
		public bool Remove(TKey key, TValue value)
		{
			ISet<TValue> current;
			if (items.TryGetValue(key, out current))
			{
				return current.Remove(value);
			}
			
			return false;
		}

		public bool TryGetValue(TKey key, out ISet<TValue> value)
		{
			return items.TryGetValue(key, out value);
		}

		public IEnumerator<KeyValuePair<TKey, ISet<TValue>>> GetEnumerator()
		{
			return items.GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return items.GetEnumerator();
		}
	}
}
