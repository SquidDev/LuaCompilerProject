using System.Collections.Generic;

namespace LuaCP.Graph
{
	/// <summary>
	/// A node in a CFG
	/// </summary>
	public interface IGraphNode<T>
        where T : class, IGraphNode<T>
	{
		T ImmediateDominator { get; set; }

		HashSet<T> DominatorTreeChildren { get; }

		HashSet<T> DominanceFrontier { get; }

		IEnumerable<T> Next { get; }

		IEnumerable<T> Previous { get; }
	}
}

