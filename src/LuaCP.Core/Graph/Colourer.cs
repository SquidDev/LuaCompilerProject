using System;
using System.Collections.Generic;
using System.Linq;
using LuaCP.Collections;

namespace LuaCP.Graph
{
	public class Colourer
	{
		private enum Result
		{
			Solved,
			Unsolved,
			Busted,
		}

		private readonly HashSet<int>[] possibilities;
		private readonly Graph graph;
		private readonly int colours;

		public Colourer(Graph graph, int colours)
		{
			this.graph = graph;
			this.colours = colours;
			this.possibilities = new HashSet<int>[graph.Size];

			var values = new HashSet<int>(Enumerable.Range(0, colours));
			for (int i = 0; i < possibilities.Length; i++)
			{
				possibilities[i] = values;
			}
		}

		public Colourer(Graph graph, int colours, HashSet<int>[] possibilities)
		{
			this.graph = graph;
			this.colours = colours;
			this.possibilities = possibilities;
		}

		public Colourer SetColour(int node, int colour)
		{
			var newPossibilities = (HashSet<int>[])this.possibilities.Clone();
			newPossibilities[node] = new HashSet<int>() { colour };
			return new Colourer(graph, colours, newPossibilities);
		}

		private Result Status
		{
			get
			{
				if (possibilities.Any(p => p.IsEmpty())) return Result.Busted;
				if (possibilities.All(p => p.Count() == 1)) return Result.Solved;
				return Result.Unsolved;
			}
		}

		private Colourer Reduce()
		{
			var newPossibilities = (HashSet<int>[])this.possibilities.Clone();
			// The query answers the question “What colour possibilities should I remove?”
			var reductions = Enumerable
				.Range(0, newPossibilities.Length)
				.Where(node => newPossibilities[node].Count() == 1)
				.SelectMany(node =>
			{
				var colour = newPossibilities[node].Single();
				return graph.Neighbours(node)
						.Where(neighbour => newPossibilities[neighbour].Contains(colour))
						.Select(neighbour => new { neighbour , colour });
			});
			bool progress = false;

			while (true)
			{
				var list = reductions.ToList();
				if (list.Count == 0) break;
				progress = true;
				foreach (var reduction in list)
				{
					HashSet<int> replacement = new HashSet<int>(newPossibilities[reduction.neighbour]);
					replacement.Remove(reduction.colour);
					newPossibilities[reduction.neighbour] = replacement;
				}
				// Doing so might have created a new node that has a single possibility,
				// which we can then use to make further reductions. Keep looping until
				// there are no more reductions to be made.
			}
			return progress ? new Colourer(graph, colours, newPossibilities) : null;
		}

		public IEnumerable<int> Solve()
		{
			switch (Status)
			{
				case Result.Solved:
					// Base case: we are already solved or busted.
					return this.possibilities.Select(x => x.Single());
				case Result.Busted:
					return null;
				default:
					{
						// Easy inductive case: do simple reductions and then solve again.
						var reduced = Reduce();
						if (reduced != null) return reduced.Solve();
						// Difficult inductive case: there were no simple reductions.
						// Make a hypothesis about the colouring of a node and see if 
						// that introduces a contradiction or a solution.
						int node = Array.FindIndex(this.possibilities, p => p.Count() > 1);
						var solutions =
							from colour in this.possibilities[node]
							let solution = this.SetColour(node, colour).Solve()
							where solution != null
							select solution;
						return solutions.FirstOrDefault();
					}
			}
		}

		public static void Run()
		{
			Graph graph = new Graph(11);
			Action<char, char> addEdge = (a, b) => graph.AddEdge(a - 'A', b - 'A');
			addEdge('A', 'B');
			addEdge('A', 'H');

			addEdge('D', 'B');
			addEdge('D', 'C');
			addEdge('D', 'I');
			addEdge('D', 'K');

			addEdge('E', 'K');
			addEdge('E', 'F');

			addEdge('G', 'F');
			addEdge('G', 'H');
			addEdge('G', 'K');

			addEdge('I', 'H');
			addEdge('I', 'J');

			addEdge('J', 'H');
			addEdge('J', 'K');

			addEdge('K', 'H');

			var sasolver = new Colourer(graph, 14);
			var solution = sasolver.Solve();
			int i = 0;
			foreach (var colour in solution)
			{
				Console.WriteLine(Char.ToString((char)(i + 'A')) + ": " + colour);
				i++;
			}
		}
	}
}

