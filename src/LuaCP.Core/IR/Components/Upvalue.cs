using System;
using System.Collections.Generic;
using System.Linq;
using LuaCP.IR.Instructions;
using LuaCP.IR.User;

namespace LuaCP.IR.Components
{
	public class Upvalue : IValue
	{
		private readonly CountingSet<IUser<IValue>> users = new CountingSet<IUser<IValue>>();
		private readonly bool closed;
		private readonly Function function;
		private readonly ValidDictionary<Upvalue> meta;

		public Upvalue(Function function, bool closed)
		{
			(closed ? function.closedUpvalues : function.openUpvalues).Add(this);
			this.closed = closed;
			this.function = function;
			this.meta = new ValidDictionary<Upvalue>(this);
		}

		public ValueKind Kind { get { return Closed ? ValueKind.Value : ValueKind.Reference; } }

		public CountingSet<IUser<IValue>> Users { get { return users; } }

		public bool Closed { get { return closed; } }

		public Function Function { get { return function; } }
        
		public ValidDictionary<Upvalue> Meta { get { return meta; } }
        
		public IEnumerable<IValue> KnownValues
		{
			get
			{
				int index = (closed ? function.closedUpvalues : function.openUpvalues).IndexOf(this);
				return function.Users
                    .OfType<ClosureNew>()
                    .Select(x => (closed ? x.ClosedUpvalues : x.OpenUpvalues)[index]);
			}
		}

		public void Remove()
		{
			if (users.UniqueCount > 0) throw new InvalidOperationException("Cannot remove upvalue as it is used");
			(closed ? function.closedUpvalues : function.openUpvalues).Remove(this);
		}
	}
}
