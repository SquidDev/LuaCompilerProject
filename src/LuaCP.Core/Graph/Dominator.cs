using System;
using System.Collections.Generic;
using System.Linq;

namespace LuaCP.Graph
{
	/// <summary>
	/// Calculate dominators for a block
	/// </summary>
	public static class Dominators
	{
		public static void BuildDominators<T>(this T root)
            where T : class, IGraphNode<T>
		{
			// Variation of A Simple, Fast Dominance Algorithm
			// Keith D. Cooper, Timothy J. Harvey and Ken Kennedy
			// Doesn't use postorder numbers as they are a pain to calculate for non DAGs

			// Basic setup, clear, etc...
			// We use the eager method as we are going to use it many times
			IEnumerable<T> nodes = root.ReachableEager();
			foreach (T node in nodes)
			{
				node.ImmediateDominator = default(T);
				node.DominatorTreeChildren.Clear();
				node.DominanceFrontier.Clear();
			}
			root.ImmediateDominator = root;

			bool changed = true;
			while (changed)
			{
				changed = false;

				HashSet<T> visited = new HashSet<T>();
				foreach (T node in root.VisitPreorder(visited))
				{
					if (node == root) continue;

					T newIdiom = node.Previous.First(p => visited.Contains(p) && p != node);
					foreach (T previous in node.Previous)
					{
						if (previous != newIdiom && previous.ImmediateDominator != null)
						{
							newIdiom = FindCommonDominator(previous, newIdiom);
						}
					}

					if (newIdiom != node.ImmediateDominator)
					{
						node.ImmediateDominator = newIdiom;
						changed = true;
					}
				}
			}
                
			root.ImmediateDominator = null;
			foreach (T node in nodes.Where(x => x.ImmediateDominator != null))
			{
				node.ImmediateDominator.DominatorTreeChildren.Add(node);
			}

			root.VisitPostorder(n =>
				{
					foreach (T next in n.Next)
					{
						if (next.ImmediateDominator != n)
						{
							n.DominanceFrontier.Add(next);
						}
					}

					foreach (T f in n.DominatorTreeChildren.SelectMany(x => x.DominanceFrontier))
					{
						if (f.ImmediateDominator != n)
						{
							n.DominanceFrontier.Add(f);
						}
					}
				});
		}

		private static T FindCommonDominator<T>(T x, T y)
            where T : class, IGraphNode<T>
		{
			HashSet<T> path = new HashSet<T>();
			while (x != null && path.Add(x))
			{
				x = x.ImmediateDominator;
			}
			while (y != null)
			{
				if (path.Contains(y))
				{
					return y;
				}
				else
				{
					y = y.ImmediateDominator;
				}
			}

			throw new Exception("No common dominator found!");
		}
	}
}

