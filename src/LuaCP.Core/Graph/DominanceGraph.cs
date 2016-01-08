using System.Collections.Generic;

namespace LuaCP.Graph
{
	public static class DominanceTraversal
	{
		public static IEnumerable<T> DominancePreorder<T>(this T node)
			where T : class, IGraphNode<T>
		{
			return DominancePreorder(node, new HashSet<T>());
		}

		private static IEnumerable<T> DominancePreorder<T>(T node, HashSet<T> visited)
			where T : class, IGraphNode<T>
		{
			if (visited.Add(node))
			{
				yield return node;

				foreach (T child in node.DominatorTreeChildren)
				{
					foreach (T item in DominancePreorder(child, visited)) yield return item;
				}
			}
		}

		public static bool Dominates<T>(this T dominator, T node)
			where T : class, IGraphNode<T>
		{
			while (node != null)
			{
				if (dominator == node) return true;
				node = node.ImmediateDominator;
			}
			
			return false;
		}
	}
}
