using System;
using System.Collections.Generic;
using LuaCP.Collections;
using System.Linq;

namespace LuaCP.Graph
{
	/// <summary>
	/// A basic undirected graph
	/// </summary>
	public sealed class UndirectedGraph
	{
		private readonly bool[,] neighbours;
		private readonly int nodes;

		public UndirectedGraph(int nodes)
		{
			this.nodes = nodes;
			this.neighbours = new bool[nodes, nodes];
		}

		public void AddEdge(int left, int right)
		{
			neighbours[left, right] = true;
			neighbours[right, left] = true;
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

		public int NeighbourCount(int node)
		{
			int count = 0;
			for (int i = 0; i < Size; i++)
			{
				if (neighbours[node, i]) count++;
			}
			return count;
		}

		public int Size { get { return nodes; } }

		public ColourerResult Colour(EqualityMap<int> eq = null)
		{
			int[] mappings = (-1).Repeat(Size);

			int[] nodes = Enumerable.Range(0, Size)
				.OrderByDescending(x => NeighbourCount(x))
				.ToArray();

			int colour = 0;
			int remaining = nodes.Length;
			while (remaining > 0)
			{
				foreach (int node in nodes)
				{
					if (mappings[node] == -1 && Neighbours(node).All(x => mappings[x] != colour))
					{
						mappings[node] = colour;
						remaining--;

						if (eq != null)
						{
							foreach (int other in eq.GetEqual(node))
							{
								if (mappings[other] == -1 && Neighbours(other).All(x => mappings[x] != colour))
								{
									mappings[other] = colour;
									remaining--;
								}
							}
						}
					}
				}
				colour++;
			}

			return new ColourerResult(colour, mappings);
		}
	}

	public struct ColourerResult
	{
		/// <summary>
		/// The number of colours used
		/// </summary>
		public readonly int ColourCount;

		/// <summary>
		/// The colour for each node
		/// </summary>
		public readonly int[] Colours;

		public ColourerResult(int colourCount, int[] colours)
		{
			ColourCount = colourCount;
			Colours = colours;
		}
	}
}

