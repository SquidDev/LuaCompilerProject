using System;
using System.Collections.Generic;
using System.Linq;

namespace LuaCP.IR.User
{
	public interface IUsable<T>
	{
		CountingSet<IUser<T>> Users { get; }
	}

	public interface IUser<T>
	{
		IEnumerable<T> GetUses();
		
		void Replace(T original, T replace);
	}
	
	public static class UserExtensions
	{
		public static T Replace<T>(IUser<T> user, T original, T replace)
			where T : IUsable<T>
		{
			if (replace == null) throw new ArgumentNullException("replace");
			if (original != null) original.Users.Decrement(user);
			replace.Users.Increment(user);
			
			return replace;
		}
		
		public static void Increment<T>(this IEnumerable<T> items, IUser<T> user)
			where T : IUsable<T>
		{
			foreach (T item in items)
			{
				item.Users.Increment(user);
			}
		}
		
		public static void Decrement<T>(this IEnumerable<T> items, IUser<T> user)
			where T : IUsable<T>
		{
			foreach (T item in items)
			{
				item.Users.Decrement(user);
			}
		}
		
		public static void ReplaceWith<T>(this T original, T replace)
			where T : IUsable<T>
		{
			foreach (IUser<T> item in original.Users.ToList<IUser<T>>())
			{
				item.Replace(original, replace);
			}
		}
	}
}
