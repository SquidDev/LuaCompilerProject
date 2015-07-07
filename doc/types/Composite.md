# Composite types
Composite types are the most flexible structure in Tua and are the base for most types. They store a series of named properties, as well as additional meta processing info. Composite types can be defined in an `interface` block or with inline type definitions.

```lua
interface Foo
	foo:number, -- Like tables, `,` or `;` must be used to separate fields.
	bar:string,
end

local a:Foo = { foo = 1, bar = "Hello" }
local b:{foo:number, bar:string} = a
```

We will use the interface syntax when declaring types for clarity's sake.

## Fields
The syntax for declaring fields is not dissimilar to that of Lua tables, though the `[` and `]` symbols for keys are not needed. Identifier, String and Number keys are allowed

```lua
interface Foo
	foo:number,
	"bar":string, -- Useful for non-identifiers
	1:boolean,	-- Mix and match arrays and objects
end
```

This syntax allows you to define a mix of array and table style syntax.

```lua
interface Node
	startline:number.
	endline:number,
end

interface BinaryOperation extends Node
	1:string,
	2:Node,
	3:Node,
end

local a:BinaryOperation = { startline = 1, endline = 2, "+", left, right }
local b:Node = ...

print(a[1])
a[2] = b
print(a.startline)
```

## Functions
Functions can be stored within composite types in two ways. Functions normally operate as normal fields, but you can also create methods. Methods perform an action on the current object and cannot be overridden.

```lua
interface Fooable
	function fooify(),
	bar: (Fooable, number):number,
end

-- Look, more function type implying!
local a:Fooable = {
	fooify = function(x) return x end,
	bar = function(a, b) return b end,
}

a:fooify()
a.fooify(a) -- Error: Calling instance method without `:` operator
a.fooify = nil -- Error: Cannot set method 'fooify'

a:bar(2) -- Error: Calling field as instance method
a.bar(a, 2)
a.bar = nil
```

## Method Overloads
There are several types of overloads. The most common are those of different function signatures:

```lua
interface Foo
	function bar(a:number):number
	function bar(a:string):string
end

local a:Foo = {
	-- You must specify a method that takes the lowest common set of values - in this case (any):any.
	bar = function(a)
		if type(a) == "string" then
			return "Foo" .. a
		else
			return 2 ^ a
		end
	end
}
```

### Overloads on constants
Sometimes you may want to implement overloads based off a constant value. For instance `collectgarbage("collect")` is different to `collectgarbage("count")`. You can use String or Number literals to help define overrides.

```lua
interface Foo
	function bar(a:"Name"):String
	function bar(a:"Print"):void
end

local a:Foo
...

-- Different actions
local s:string = a:bar("Name")
a:bar("Print")

local b:string = "Print"
a:bar(b) -- Not valid - must be passed as literal
```

## Metamethods
Metamethods define a way of interacting with instances of the same or different types. Metamethods are not bound to a specific instance of the object but instead define what interactions the objects can do. At least one argument must the interfaces type - generally the first one

### Unary operators
- `index(T, key):any`: Get an undefined index
- `newindex(T, key, value):void`: Create a new value - this ensures the current value does not already exist.
- `call(T, ...:any):any`: Call this object with these arguments. By specifying arguments you can limit how the object can be called.
- `unm(T):any`: Unary minus (`-a`) operator
- `len(T):any`: Get the 'length' of the instance. **This does not work on non-native types and so produces a warning**.

### Binary operators
These take the form `add(T|any, T|any):any`, though a different type constraint can be used instead of `any`. You can also use overloads:

```lua
interface Foo
	meta add(lhs:Foo, rhs:String):String
	meta add(lhs:String, rhs:Foo):Foo

	meta add(lhs:Foo|Number, rhs:Foo|Number):Foo
end
```

The binary operators you can implement are:
 - `add`: `+`
 - `sub`: `-`
 - `mul`: `*`
 - `div`: `/`
 - `mod`: `%`
 - `pow`: `pow`

The presence of the binary operator is checked on the left and then right hand side. If the left side has the correct binary operator, but the function signature does not match, then an error will occur due to limitations with Lua's type system.

### Comparison methods
The comparison methods:
 - `eq`: `==`
 - `lt`: `<`
 - `le`: `<=`

Can be specified as metamethods, but both operands must be of the defining type - and they cannot be relied on 100% of the time. Tua attempts to find comparison issues but may not always find them.

## Subtypes
A composite type is a subtype of a composite type if all its memebers are compatible with the target's types.

```lua
local a:{a:number,b:string|number}
local b:{a:number} = a
local c:{b:number} = a -- Error: string is not assignable to number.
```

Fields must be compatible both ways, but members only need to be compatible one way:

```lua
local a:{
	function foo(a:string|number):string,
	bar:(a:string|number):string
}

-- This is valid as you can assign string to string|number
local b:{function foo(a:string):string} = a

-- Error: string|number is not assignable to string.
local c:{bar:(a:string):string} = a
```
