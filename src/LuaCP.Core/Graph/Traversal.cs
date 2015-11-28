using System;
using System.Collections.Generic;

namespace LuaCP.Graph
{
	public static class Traversal
	{
		public static IEnumerable<T> VisitPreorder<T>(this T node)
            where T : class, IGraphNode<T>
		{
			return VisitPreorder(node, new HashSet<T>());
		}

		public static IEnumerable<T> VisitPreorder<T>(this T node, ISet<T> visited)
            where T : class, IGraphNode<T>
		{
			Stack<T> toVisit = new Stack<T>();
			toVisit.Push(node);
        	
			while (toVisit.Count > 0)
			{
				T item = toVisit.Pop();
				if (visited.Add(item))
				{
					yield return item;
	        		
					foreach (T child in item.Next) toVisit.Push(child);
				}
			}
		}

		public static void VisitPostorder<T>(this T node, Action<T> action)
            where T : class, IGraphNode<T>
		{
			VisitPostorder(node, (n, visited) => action(n));
		}

		public static void VisitPostorder<T>(this T node, Action<T, ISet<T>> action)
            where T : class, IGraphNode<T>
		{
			VisitPostorder(node, action, new HashSet<T>());
		}

		public static void VisitPostorder<T>(this T node, Action<T, ISet<T>> action, ISet<T> visited)
            where T : class, IGraphNode<T>
		{
			// TODO: Make this no longer recursive
			// https://github.com/vasilyvlasov/algorithms/blob/master/src/main/java/algorithms/data/graph/DFS.java

			if (!visited.Add(node)) return;
			foreach (T child in node.Next)
			{
				VisitPostorder(child, action, visited);
			}

			action(node, visited);
		}
	}
}

