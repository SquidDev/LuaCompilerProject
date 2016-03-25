using LuaCP.Collections;
using System.Collections.Generic;
using System;
using LuaCP.IR.Components;

namespace LuaCP.Tree
{
	public interface IScope
	{
		IScope CreateChild();

		IScope CreateFunctionChild(Function func);
	}

	public class ScopeDictionary : TypeDictionary<BlockBuilder, IScope>, IScope
	{
		public ScopeDictionary(BlockBuilder instance)
			: base(instance)
		{
		}

		public ScopeDictionary CreateChild()
		{
			ScopeDictionary child = new ScopeDictionary(Instance);
			foreach (KeyValuePair<Type, IScope> item in Items)
			{
				child.Items.Add(item.Key, item.Value.CreateChild());
			}

			return child;
		}

		public ScopeDictionary CreateFunctionChild(Function func)
		{
			ScopeDictionary child = new ScopeDictionary(Instance);
			foreach (KeyValuePair<Type, IScope> item in Items)
			{
				child.Items.Add(item.Key, item.Value.CreateFunctionChild(func));
			}

			return child;
		}

		public T Get<T>() where T : IScope
		{
			return Get<T>(x =>
			{
				throw new Exception("Cannot create instance");
			});
		}

		public T GetCreate<T>() where T : IScope, new()
		{
			return Get<T>(x => new T());
		}

		IScope IScope.CreateFunctionChild(Function func)
		{
			return CreateFunctionChild(func);
		}

		IScope IScope.CreateChild()
		{
			return CreateChild();
		}
	}
}

