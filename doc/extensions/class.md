# Class Library
LuaCP provides a simple class syntax:

```lua
local class A
	bar = {}
	function foo()
		return self.bar
	end
end

local class B extends A
	function addBar(bar)
		table.insert(self.bar, bar)
	end

	function foo()
		return {"Super methods are bad", unpack(self.super.foo(self))}
	end

	static things = "Static properties"
	static function getThings()
		-- There is no self variable so you have to use the class name
		return B.things
	end
end

local thing = B()
thing:addBar("Hello")
print(things:foo())

print(B.getThings())
```

Yeah, it is horrible. I'm working on it.
