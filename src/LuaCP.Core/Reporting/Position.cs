using System;

namespace LuaCP.Reporting
{
	public struct Position : IComparable<Position>, IEquatable<Position>
	{
		public readonly int Line;
		public readonly int Column;
		public readonly int Offset;

		public Position(int line, int column, int offset)
		{
			Line = line;
			Column = column;
			Offset = offset;
		}

		public override string ToString()
		{
			return Line + ":" + Column;
		}

		public override int GetHashCode()
		{
			return Offset;
		}

		public override bool Equals(object obj)
		{
			if (obj is Position) return Equals((Position)obj);
			return false;
		}

		public int CompareTo(Position other)
		{
			return Offset.CompareTo(other.Offset);
		}

		public bool Equals(Position other)
		{
			return Offset == other.Offset;
		}

		public bool Equals(ref Position other)
		{
			return Offset == other.Offset;
		}

		public static bool operator==(Position left, Position right)
		{
			return left.Offset == right.Offset;
		}

		public static bool operator!=(Position left, Position right)
		{
			return left.Offset != right.Offset;
		}
	}
}

