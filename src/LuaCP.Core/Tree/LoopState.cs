namespace LuaCP.Tree
{
	/// <summary>
	/// The state of the current loop.
	/// </summary>
	public sealed class LoopState
	{
		/// <summary>
		/// Where the test occurs
		/// (to skip to for a "continue" statement)
		/// </summary>
		public readonly BlockBuilder Test;

		/// <summary>
		/// The exit point of the current loop
		/// (to skip to for a "break" statement)
		/// </summary>
		public readonly BlockBuilder End;

		public LoopState(BlockBuilder test, BlockBuilder end)
		{
			Test = test;
			End = end;
		}
	}
}

