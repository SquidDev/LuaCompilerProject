# Built in types
Most built in types are composite types. This document covers those types.

## Number
The Number type (or `number`) represents a 64-bit double precision floating point.

```lua
strict interface Number
	meta function add(lhs:string|number, rhs:string|number):number,
	meta function sub(lhs:string|number, rhs:string|number):number,
	meta function mul(lhs:string|number, rhs:string|number):number,
	meta function div(lhs:string|number, rhs:string|number):number,
	meta function mod(lhs:string|number, rhs:string|number):number,
	meta function pow(lhs:string|number, rhs:string|number):number,

	meta function eq(lhs:number, rhs:number):boolean,
	meta function lt(lhs:number, rhs:number):boolean,
	meta function le(lhs:number, rhs:number):boolean,

	meta function unm(self:number):number,
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
	function byte(first:number = 1):string,
	function byte(first:number, last:number):[...:string],
	function find(pattern:string, start:number = 1, plain:number = false):[number, number],
	function format(...:any):string,
	function gmatch(pattern:string):():[...:any],
	function gsub(pattern:string, replace:(...:string):string|string):[string, number],
	function gsub(pattern:string, replace:(...:string):string|string, limit:number):[string, number],
	function len():number,
	function lower():string,
	function match(pattern:string, first:number = 1):():[...:any],
	function rep(count:number):string,
	function reverse():string,
	function sub(first:number):string,
	function sub(first:number, last:number):string,
	function upper():string,

	meta function concat(lhs:string|number, rhs:string|number):string,

	meta function eq(lhs:string, rhs:string):boolean,
	meta function lt(lhs:string, rhs:string):boolean,
	meta function le(lhs:string, rhs:string):boolean,

	meta function unm(self:number):number,
	meta function len(self:number):number,
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
