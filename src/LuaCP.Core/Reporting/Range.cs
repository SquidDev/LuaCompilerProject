using System;

namespace LuaCP.Reporting
{
	public class Range : IEquatable<Range>
	{
		private readonly Position start;
		private readonly Position end;
		private readonly string source;

		public Range(string source, Position start, Position end)
		{
			this.source = String.IsNullOrEmpty(source) ? "<stdin>" : source;
			this.start = start;
			this.end = end;
		}

		public Position Start { get { return start; } }

		public Position End { get { return end; } }

		public string Source { get { return source; } }

		public override string ToString()
		{
			return source + ": " + start + "-" + end;
		}

		public override int GetHashCode()
		{
			int hash = source.GetHashCode();
			hash = hash * 31 + start.GetHashCode();
			hash = hash * 31 + end.GetHashCode();
			return hash;
		}

		public override bool Equals(object obj)
		{
			if (obj is Range) return Equals((Range)obj);
			return false;
		}

		public bool Equals(Range other)
		{
			return source == other.source && start.Equals(other.start) && end.Equals(other.end);
		}
	}
}

