using System.Collections.Generic;
using System.Linq;

namespace LuaCP.IR.User
{
	public sealed class UsingDictionary<TKey, TVal, TUser> : IDictionary<TKey, TVal>
		where TKey : IUsable<TKey>
		where TVal : IUsable<TVal>
		where TUser : IUser<TKey>, IUser<TVal>
	{
		private readonly TUser user;
		private readonly Dictionary<TKey, TVal> items;

		public UsingDictionary(TUser user)
		{
			this.user = user;
			this.items = new Dictionary<TKey, TVal>();
		}

		public UsingDictionary(TUser user, IDictionary<TKey, TVal> items)
		{
			this.user = user;
			this.items = new Dictionary<TKey, TVal>(items);
			items.Keys.Increment(user);
			items.Values.Increment(user);
		}

		public TVal this[TKey key]
		{
			get { return items[key]; }
			set
			{
				TVal val;
				if (items.TryGetValue(key, out val)) val.Users.Decrement(user);
				
				items[key] = value;
				value.Users.Increment(user);
			}
		}

		public ICollection<TKey> Keys { get { return items.Keys; } }

		public ICollection<TVal> Values { get { return items.Values; } }

		public int Count { get { return items.Count; } }

		public bool IsReadOnly { get { return false; } }

		public bool ContainsKey(TKey key)
		{
			return items.ContainsKey(key);
		}

		public void Add(TKey key, TVal value)
		{
			items.Add(key, value);
			key.Users.Increment(user);
			value.Users.Increment(user);
		}

		public bool Remove(TKey key)
		{
			TVal val; 
			if (items.TryGetValue(key, out val))
			{
				items.Remove(key);
				key.Users.Decrement(user);
				val.Users.Decrement(user);
				return true;
			}
			
			return false;
		}

		public bool TryGetValue(TKey key, out TVal value)
		{
			return items.TryGetValue(key, out value);
		}

		public void Add(KeyValuePair<TKey, TVal> item)
		{
			Add(item.Key, item.Value);
		}

		public void Clear()
		{
			items.Keys.Decrement(user);
			items.Values.Decrement(user);
			items.Clear();
		}

		public bool Contains(KeyValuePair<TKey, TVal> item)
		{
			return ((ICollection<KeyValuePair<TKey, TVal>>)items).Contains(item);
		}

		public void CopyTo(KeyValuePair<TKey, TVal>[] array, int arrayIndex)
		{
			((ICollection<KeyValuePair<TKey, TVal>>)items).CopyTo(array, arrayIndex);
		}

		public bool Remove(KeyValuePair<TKey, TVal> item)
		{
			if (items.Remove(item.Key))
			{
				item.Key.Users.Decrement(user);
				item.Value.Users.Decrement(user);
				return true;
			}
			return false;
		}

		public void ReplaceKey(TKey original, TKey replace)
		{
			TVal value;
			if (items.TryGetValue(original, out value))
			{
				items.Remove(original);
				items.Add(replace, value);

				original.Users.Decrement(user);
				replace.Users.Increment(user);
			}
		}

		public void ReplaceValue(TVal original, TVal replace)
		{
			List<KeyValuePair<TKey, TVal>> found = items
				.Where(x => EqualityComparer<TVal>.Default.Equals(x.Value, original))
				.ToList();
			
			foreach (KeyValuePair<TKey, TVal> item in found)
			{
				item.Value.Users.Decrement(user);
				replace.Users.Increment(user);
				items[item.Key] = replace;
			}
		}

		public IEnumerator<KeyValuePair<TKey, TVal>> GetEnumerator()
		{
			return items.GetEnumerator();
		}

		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
		{
			return items.GetEnumerator();
		}
	}
}
