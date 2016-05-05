using System;

namespace LuaCP.Collections
{
	public sealed class IdentRef<T> : IComparable, IComparable<IdentRef<T>>
	{
		public T Value;

		public IdentRef(T value)
		{
			Value = value;
		}

		public int CompareTo(IdentRef<T> other)
		{
			if (this == other) return 0;

			int thisCode = GetHashCode();
			int otherCode = other.GetHashCode();

			if (thisCode < otherCode) return -1;
			if (thisCode > otherCode) return 1;

			// Not really sure what to do here.
			return 1;
		}

		public int CompareTo(object obj)
		{
			return CompareTo((IdentRef<T>)obj);
		}
	}
}

