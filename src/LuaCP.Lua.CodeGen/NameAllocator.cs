using System;
using System.Collections.Generic;

namespace LuaCP.Lua.CodeGen
{
    public class NameAllocator
    {
        private int counter = -1;
        private readonly string prefix;

        public NameAllocator(string prefix)
        {
            this.prefix = prefix;
        }

        public string Next()
        {
            return prefix+ ++counter;
        }
    }

    public class NameAllocator<T>
    {
        private readonly Dictionary<T, String> lookup = new Dictionary<T, String>();
        private readonly NameAllocator allocator;

        public NameAllocator(string prefix)
        {
            allocator = new NameAllocator(prefix);
        }

        public string this [T key]
        {
            get
            {
                string name;
                if (lookup.TryGetValue(key, out name)) return name;

                name = allocator.Next();
                lookup.Add(key, name);
                return name;
            } 

            set
            {
                lookup.Add(key, value);
            }
        }
    }
}

