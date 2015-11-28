using System;

namespace LuaCP.Reporting
{
	public struct Position : IComparable<Position>, IEquatable<Position>
	{
		private readonly int line;
		private readonly int column;
		private readonly int offset;

		public Position(int line, int column, int offset)
		{
			this.line = line;
			this.column = column;
			this.offset = offset;
		}

		public int Line { get { return line; } }

		public int Column { get { return column; } }

		public int Offset { get { return offset; } }

		public override string ToString()
		{
			return Line + ":" + Column;
		}

		public override int GetHashCode()
		{
			return offset;
		}

		public override bool Equals(object obj)
		{
			if (obj is Position) return Equals((Position)obj);
			return false;
		}

		public int CompareTo(Position other)
		{
			return offset.CompareTo(other.offset);
		}

		public bool Equals(Position other)
		{
			return offset == other.offset;
		}

		public bool Equals(ref Position other)
		{
			return offset == other.offset;
		}
	}
}

