using System.Collections.Generic;

namespace LuaCP.Collections
{
	public class UniqueQueue<T>
	{
		private readonly Queue<T> queue = new Queue<T>();
		private readonly HashSet<T> visited = new HashSet<T>();

		public int Count { get { return queue.Count; } }

		public void Enqueue(T item)
		{
			if (visited.Add(item)) queue.Enqueue(item);
		}

		public T Dequeue()
		{ 
			return queue.Dequeue();
		}

		public T Peek()
		{ 
			return queue.Peek();
		}
	}
}

