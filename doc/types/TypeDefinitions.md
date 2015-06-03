# Types
## Number
The Number type (or `number`) represents a 64-bit double precision floating point.

```lua
strict interface Number
  static meta function add(lhs:String|Number, rhs:String|Number):Number
	static meta function sub(lhs:String|Number, rhs:String|Number):Number
	static meta function mul(lhs:String|Number, rhs:String|Number):Number
	static meta function div(lhs:String|Number, rhs:String|Number):Number
	static meta function mod(lhs:String|Number, rhs:String|Number):Number
	static meta function pow(lhs:String|Number, rhs:String|Number):Number

	meta function eq(lhs:number, rhs:number):boolean
	meta function lt(lhs:number, rhs:number):boolean
	meta function le(lhs:number, rhs:number):boolean

	meta function unm(self:number):number
end
```

## Boolean
The Boolean type (or `boolean` or `bool`) is always the product of the `==`, `<` and `<=` operations (and so also of `~=`, `>` and `>=`). It is also the result of `not` operation.

```lua
strict interface Boolean
end
```

## String
The String type (or `string`) represents a series of characters - not necessarily a printable ASCII sequence.

```lua
strict interface String
	function byte(first:Number = 1):String
	function byte(first:Number, last:Number):[...:String]
	function find(pattern:String, start:Number = 1, plain:Number = false):[Number, Number]
	function format(...:any):String
	function gmatch(pattern:String):():[...:any]
	function gsub(pattern:String, replace:(...:String):String|String):[String, Number]
	function gsub(pattern:String, replace:(...:String):String|String, limit:Number):[String, Number]
	function len():Number
	function lower():String
	function match(pattern:String, first:Number = 1):():[...:any]
	function rep(count:Number):String
	function reverse():String
	function sub(first:Number):String
	function sub(first:Number, last:Number):String
	function upper():String

	static meta function concat(lhs:string|number, rhs:string|number):string

	meta function eq(lhs:string, rhs:string):boolean
	meta function lt(lhs:string, rhs:string):boolean
	meta function le(lhs:string, rhs:string):boolean

	meta function unm(self:number):number
	meta function len(self:number):number
end
```

## Function
The Function type is simply a callable entity. It doesn't have an explicit representation due to its complex natures. The Function type literal is composed of an argument list followed by the return value.

```lua
local a:(number, number):number = function(a, b) return a + b end -- Note, types on the right are implied
```

Function return values are the only time when the `void` type can be used.

### Varargs
Varargs are represented like any typed parameter. By default they are all any, but can be limited to any type.

```lua
function a(check:string, ...:string):boolean
	for _, v in ipairs({...}) do
	  if v == check then return true end
	end
	return false
end

check() -- Not valid (missing arguments)
check("Test") -- Valid as none are required
check("Test", "Foo") -- Valid as argument passed is string
check("Test", 123) -- Not valid (wrong arguments)
```

### Var return
Variable return types handle multiple return values, these are similar to function arguments but have no associated name:

```lua
function nextFoo():[number, string]
	return 4, "Hello"
end

local a:auto, b:auto = nextFoo() -- a:number and b:string

function allFoos():[string, ...:number] -- TODO: Maybe use string... instead.
	local result:{number} = {}
	local a:number, b:number = 1, 1
	while a < 100 do
		table.insert(result, a)
		a, b = b, a + b
	end

	return "Numbers!", unpack(result)
end
```

## Arrays
Arrays are tables that store a list of objects. They are 1 indexed like normal Lua tables.

```lua
strict interface Array<T>
	meta function index(n:number):T
	meta function newindex(n:number, value:T):void
	meta function len():number
end

-- Array type literal
local a:{number} = { 1, 2, 3 }

-- Array generic
local b:Array<Number> = a
```

## Maps
Maps represent a key -> value store. This is a basic Lua table.

```lua
strict interface Map<TKey, TValue>
	meta function index(key:TKey):TValue
	meta function newindex(key:TKey, value:TValue):void
end

-- Map type literal
local a:{string=number} = { foo = 1, bar = 2 }

-- Map generic
local b:Map<String, Number> = a
```

## Objects
Objects are the most flexible structure in Tua and are the base for all types. They store a series of named properties, as well as additional meta processing info. Objects can be defined in an `interface` block or inline in type definitions.

```lua
interface Foo
    foo:number, -- Like tables, `,` or `;` must be used to separate fields.
		bar:string,
end

local a:Foo = { foo = 1, bar = "Hello" }
local b:{foo:number, bar:string} = a
```

We will use the interface syntax when declaring types for clarity's sake.

### Basic fields
The syntax for declaring fields is not dissimilar to that of Lua tables, though the `[` and `]` symbols for keys are not needed. Identifier, String and Number keys are allowed

```lua
interface Foo
	foo:number,
	"bar":string, -- Useful for non-identifiers
	1:boolean,    -- Mix and match arrays and objects
end
```

### Methods
There are two types of Object methods: instance bound and field methods. Bound methods operate on the current object, field methods are simply fields with Function type.

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

a:fooify() -- Valid
a.fooify(a) -- Error: Calling instance method without `:` operator

a:bar(2) -- Warning: Calling field as instance method
a.bar(a, b) -- Valid
```


### Method Overloads
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

#### Overrides on values
Sometimes you may want to implement overrides on values. For instance `collectgarbage("collect")` is different to `collectgarbage("count")`. You can use String or Number literals to help define overrides.

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

### Metamethods
Metamethods define a way of interacting with instances of the same or different types. Metamethods are not bound to a specific instance of the object but instead define what interactions the objects can do. At least one argument must the interfaces type - generally the first one

#### Methods that work on the current instance
- `index(T, key):any`: Get an undefined index
- `newindex(T, key, value):void`: Create a new value - this ensures the current value does not already exist.
- `call(T, ...:any):any`: Call this object with these arguments. By specifying arguments you can limit how the object can be called.
- `unm(T):any`: Unary minus (`-a`) operator
- `len(T):any`: Get the 'length' of the instance. **This does not work on non-native types and so produces a warning**.

#### Binary operators
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

#### Comparison methods
The comparison methods:
 - `eq`: `==`
 - `lt`: `<`
 - `le`: `<=`

Can be specified as metamethods, but both operands must be of the defining type - and they cannot be relied on 100% of the time. Tua attempts to find comparison issues but may not always find them.

## Other types
### Strict

Represents a type that structuarly identical variables cannot be converted to. The `strict` modifier on interfaces is the easiest way to achieve this.
```lua
interface Strict<T> extends T
end

strict interface A end
interface B end

local a:A, b:B = ...

a = b -- Error: Cannot convert to strict type
b = a -- Valid: B is not strict
```

### Union
Represents a type that can be either value.
```lua
interface Union<T, U> extends T, U
end

local a:string|number = "Hello"    -- Using syntax sugar
local b:Union<String, Number> = 23 -- Equivilent
```


## Interfaces in greater depth
Interfaces describe a structure. In their simplest form they act as an alias for a type:

```lua
interface Fooifier
	(number, number):number
end

local a:Fooifier = function(a, b) return a + b end
```

This means you can create an interface representing an Object:

```lua
interface Animal
	{
		name:string,
		age:number
	}
end
```

Though in the case of objects, you can leave out the braces:

```lua
interface Animal
	name:string,
	age:number
end
```
