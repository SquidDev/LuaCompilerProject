using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace LuaCP.Collections
{
	public class IdentityComparer<T> : IEqualityComparer, IEqualityComparer<T>
	{
		private static volatile IdentityComparer<T> instance;

		public static  IdentityComparer<T> Instance
		{
			get { return instance ?? (instance = new IdentityComparer<T>()); }
		}

		private IdentityComparer()
		{
		}

		public bool Equals(T x, T y)
		{
			return Object.ReferenceEquals(x, y);
		}

		public int GetHashCode(T obj)
		{
			return RuntimeHelpers.GetHashCode(obj);
		}

		public int GetHashCode(object obj)
		{
			return RuntimeHelpers.GetHashCode(obj);
		}
	}
}

