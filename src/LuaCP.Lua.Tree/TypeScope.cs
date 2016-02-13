using System;
using LuaCP.Tree;
using System.Collections.Generic;

namespace LuaCP.Lua.Tree
{
	public class TypeScope : IScope
	{
		private readonly Dictionary<string, ValueType> variables = new Dictionary<string, ValueType>();
		private readonly TypeScope parent;

		public TypeScope()
		{
		}

		public TypeScope(TypeScope parent)
		{
			this.parent = parent;
		}

		public IScope CreateChild()
		{
			return new TypeScope(parent);
		}

		public bool TryGet(string name, out ValueType value)
		{
			return variables.TryGetValue(name, out value) || (parent != null && parent.TryGet(name, out value));
		}

		public void Declare(string name, ValueType value)
		{
			variables[name] = value;
		}
	}
}

