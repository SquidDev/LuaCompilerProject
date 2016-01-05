using System;
using System.Collections.Generic;
using System.Collections;

namespace LuaCP.Collections
{
	public class SingletonEnumerator<T>: IEnumerator<T>
	{
		private readonly T value;
		private int index;

		public SingletonEnumerator(T value)
		{
			this.value = value;
			index = -1;
		}

		public bool MoveNext()
		{
			index++;
			return index < 1;
		}

		public void Reset()
		{
			index = -1;
		}

		object IEnumerator.Current { get { return Current; } }

		public void Dispose()
		{
		}

		public T Current
		{
			get
			{
				if (index != 0) throw new InvalidOperationException("Enumeration has either not started or has already finished.");
				return value;
			}
		}
	}
}

