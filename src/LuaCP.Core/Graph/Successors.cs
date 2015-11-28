using System;
using System.Collections.Generic;

namespace LuaCP.Graph
{
	public static class Successors
	{
		public static IEnumerable<T> ReachableLazy<T>(this T node)
            where T : class, IGraphNode<T>
		{
			HashSet<T> visited = new HashSet<T>();
			Queue<T> toVisit = new Queue<T>();

			visited.Add(node);
			toVisit.Enqueue(node);

			while (toVisit.Count > 0)
			{
				T item = toVisit.Dequeue();
				yield return item;

				foreach (T next in item.Next)
				{
					if (next == null) throw new ArgumentNullException();
					if (visited.Add(next)) toVisit.Enqueue(next);
				}
			}
		}
		
		public static HashSet<T> ReachableEager<T>(this T node)
            where T : class, IGraphNode<T>
		{
			HashSet<T> visited = new HashSet<T>();
			Queue<T> toVisit = new Queue<T>();

			visited.Add(node);
			toVisit.Enqueue(node);

			while (toVisit.Count > 0)
			{
				T item = toVisit.Dequeue();
				foreach (T next in item.Next)
				{
					if (next == null) throw new ArgumentNullException();
					if (visited.Add(next)) toVisit.Enqueue(next);
				}
			}

			return visited;
		}
		
		public static IEnumerable<T> SuccessorsLazy<T>(this T node)
            where T : class, IGraphNode<T>
		{
			HashSet<T> visited = new HashSet<T>();
			Queue<T> toVisit = new Queue<T>();

			foreach (T next in node.Next)
			{
				visited.Add(next);
				toVisit.Enqueue(next);
			}

			while (toVisit.Count > 0)
			{
				T item = toVisit.Dequeue();
				yield return item;

				foreach (T next in item.Next)
				{
					if (next == null) throw new ArgumentNullException();
					if (visited.Add(next)) toVisit.Enqueue(next);
				}
			}
		}
		
		public static HashSet<T> SuccessorsEager<T>(this T node)
            where T : class, IGraphNode<T>
		{
			HashSet<T> visited = new HashSet<T>();
			Queue<T> toVisit = new Queue<T>();

			foreach (T next in node.Next)
			{
				visited.Add(next);
				toVisit.Enqueue(next);
			}

			while (toVisit.Count > 0)
			{
				T item = toVisit.Dequeue();
				foreach (T next in item.Next)
				{
					if (next == null) throw new ArgumentNullException();
					if (visited.Add(next)) toVisit.Enqueue(next);
				}
			}

			return visited;
		}
		
		public static IEnumerable<T> PredecessorsLazy<T>(this T node)
            where T : class, IGraphNode<T>
		{
			HashSet<T> visited = new HashSet<T>();
			Queue<T> toVisit = new Queue<T>();

			foreach (T previous in node.Previous)
			{
				visited.Add(previous);
				toVisit.Enqueue(previous);
			}

			while (toVisit.Count > 0)
			{
				T item = toVisit.Dequeue();
				yield return item;

				foreach (T previous in item.Previous)
				{
					if (previous == null) throw new ArgumentNullException();
					if (visited.Add(previous)) toVisit.Enqueue(previous);
				}
			}
		}
		
		public static HashSet<T> PredecessorsEager<T>(this T node)
            where T : class, IGraphNode<T>
		{
			HashSet<T> visited = new HashSet<T>();
			Queue<T> toVisit = new Queue<T>();

			foreach (T previous in node.Previous)
			{
				visited.Add(previous);
				toVisit.Enqueue(previous);
			}

			while (toVisit.Count > 0)
			{
				T item = toVisit.Dequeue();
				foreach (T previous in item.Previous)
				{
					if (previous == null) throw new ArgumentNullException();
					if (visited.Add(previous)) toVisit.Enqueue(previous);
				}
			}

			return visited;
		}
	}
}
