using System.Collections.Generic;

namespace LuaCP.IR.User
{
	public sealed class UsingList<T> : IList<T>
		where T : IUsable<T>
	{
		private readonly IUser<T> user;
		private readonly List<T> items;
		
		public T this[int x]
		{
			get { return items[x]; }
			set { items[x] = UserExtensions.Replace(user, items[x], value); }
		}
		
		public UsingList(IUser<T> user)
		{
			this.user = user;
			this.items = new List<T>();
		}
		
		public UsingList(IUser<T> user, IEnumerable<T> items)
		{
			this.user = user;
			this.items = new List<T>(items);
			items.Increment(user);
		}
		
		public int Count { get { return items.Count; } }
		public bool IsReadOnly { get { return false; } }
		
		public int IndexOf(T item)
		{
			return items.IndexOf(item);
		}
		
		public void Insert(int index, T item)
		{
			items.Insert(index, item);
			item.Users.Increment(user);
		}
		
		public void RemoveAt(int index)
		{
			T item = items[index];
			item.Users.Decrement(user);
			items.RemoveAt(index);
		}
		
		public void Add(T item)
		{
			items.Add(item);
			item.Users.Increment(user);
		}
		
		public void Clear()
		{
			items.Decrement(user);
			items.Clear();
		}
		
		public bool Contains(T item)
		{
			return items.Contains(item);
		}
		
		public void CopyTo(T[] array, int arrayIndex)
		{
			items.CopyTo(array, arrayIndex);
		}
		
		public bool Remove(T item)
		{
			if (items.Remove(item))
			{
				item.Users.Decrement(user);
				return true;
			}
			
			return false;
		}
		
		public IEnumerator<T> GetEnumerator()
		{
			return items.GetEnumerator();
		}
		
		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}
		
		public void Replace(T original, T replace)
		{
			for (int i = 0; i < items.Count; i++)
			{
				T element = items[i];
				if (EqualityComparer<T>.Default.Equals(element, original))
				{
					element.Users.Decrement(user);
					replace.Users.Increment(user);
					items[i] = replace;
				}
			}
		}
		
	}
}
