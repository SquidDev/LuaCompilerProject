using System;
using System.Collections.Generic;

namespace LuaCP.Graph
{
	/// <summary>
	/// A basic undirected graph
	/// </summary>
	public sealed class Graph
	{
		private readonly bool[,] neighbours;
		private readonly int nodes;

		public Graph(int nodes)
		{
			this.nodes = nodes;
			this.neighbours = new bool[nodes, nodes];
		}

		public void AddEdge(int left, int right)
		{
			neighbours[left, right] = true;
		}

		public void AddEdges(IEnumerable<Tuple<int, int>> edges)
		{
			foreach (var edge in edges)
			{
				AddEdge(edge.Item1, edge.Item2);
			}
		}

		public IEnumerable<int> Neighbours(int node)
		{
			for (int i = 0; i < Size; i++)
			{
				if (neighbours[node, i]) yield return i;
			}
		}

		public int Size { get { return nodes; } }
	}
}

