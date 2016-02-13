using System;

namespace LuaCP.Reporting
{
	public class Range : IEquatable<Range>
	{
		public readonly Position Start;
		public readonly Position End;
		public readonly string Source;

		public Range(string source, Position start, Position end)
		{
			Source = String.IsNullOrEmpty(source) ? "<stdin>" : source;
			Start = start;
			End = end;
		}

		public override string ToString()
		{
			return Source + ": " + Start + "-" + End;
		}

		public override int GetHashCode()
		{
			int hash = Source.GetHashCode();
			hash = hash * 31 + Start.GetHashCode();
			hash = hash * 31 + End.GetHashCode();
			return hash;
		}

		public override bool Equals(object obj)
		{
			var range = obj as Range;
			return range != null && Equals(range);
		}

		public bool Equals(Range other)
		{
			return Source == other.Source && Start == other.Start && End == other.End;
		}

		public static bool operator==(Range left, Range right)
		{
			return left.Equals(right);
		}

		public static bool operator!=(Range left, Range right)
		{
			return !left.Equals(right);
		}
	}
}

