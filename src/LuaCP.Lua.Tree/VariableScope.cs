using System;
using System.Collections.Generic;
using LuaCP.IR;
using LuaCP.IR.Components;

namespace LuaCP.Tree
{
	public 	interface IVariableScope
	{
		bool Defined(string name);
		bool TryGet(string name, out IValue value);
		void Declare(string name, IValue value);
	}

	public sealed class VariableScope : IVariableScope
	{
		public const string GlobalTable = "_ENV";

		private readonly Dictionary<string, IValue> variables = new Dictionary<string, IValue>();
		private readonly IVariableScope parent;
        
		public VariableScope()
		{
		}
		public VariableScope(IVariableScope parent)
		{
			this.parent = parent;
		}
        
		public bool Defined(string name)
		{
			return variables.ContainsKey(name) || (parent != null && parent.Defined(name));
		}

		public bool TryGet(string name, out IValue value)
		{
			return variables.TryGetValue(name, out value) || (parent != null && parent.TryGet(name, out value));
		}

		public void Declare(string name, IValue value)
		{
			variables[name] = value;
		}
        
		public IValue Globals
		{
			get
			{
				IValue value;
				if (TryGet(GlobalTable, out value)) return value;
				throw new KeyNotFoundException("Cannot find global table");
			} 
		}
	}
	
	public sealed class FunctionVariableScope : IVariableScope
	{
		private readonly IVariableScope parent;
		private readonly Dictionary<string, IValue> variables = new Dictionary<string, IValue>();
		private readonly FunctionBuilder function;
		
		public FunctionVariableScope(IVariableScope parent, FunctionBuilder function)
		{
			this.parent = parent;
			this.function = function;
		}

		public bool Defined(string name)
		{
			return parent.Defined(name);
		}
		
		public bool TryGet(string name, out IValue value)
		{
			if (variables.TryGetValue(name, out value)) return true;
			
			IValue parentVariable;
			if (parent.TryGet(name, out parentVariable))
			{
				Upvalue upvalue = new Upvalue(function.Function, false);
				value = upvalue;
				function.Upvalues.Add(parentVariable);
				variables.Add(name, upvalue);
				
				return true;
			}
			
			return false;
		}
		
		public void Declare(string name, IValue value)
		{
			throw new InvalidOperationException("Cannot declare in function scope");
		}
	}
}
