# Tua
Tua is a programming language which adds optional static type
checking to the Lua programming language.

Programs can be written in Tua, typechecked by the Tua compiler,
then compiled into Lua and run anywhere Lua scripts are used.
The type checking features in Tua are powerful, but optional,
allowing programmers to mix dynamic and static programming
styles as they wish. Tua aims to be backwards compatible with
existing Lua code.

## Basic syntax

### Declaring dynamically typed local variables
```lua
local a = 1
local b = true
local c = "hello"
local d = { "hello", "world" }
local e = { hello = "world" }
local function f(x, y)
	return x + y
end
```

### Declaring statically typed local variables using explicit types:
```lua
local a:number = 1
local b:boolean = true
local c:string = "hello"
local d:{string} = { "hello", "world" } -- Arrays
local e:{string=string} = { hello = "world" } -- Objects

-- Functions can also specify types
local function f(x:number, y:number):number
	return x + y
end

-- Used to represent no returns
local function g(x:number, y:number):void
	print(x + y)
end

-- Used to represent multiple returns
local function h(x:number, y:number):[number, number]
	print(x + y)
end
```

### Inferring types and `any`
```lua
local a:auto = 1
local b:auto = true
local c:auto = "hello"
local d:{auto} = { "hello", "world" } -- Of course, you can just do d:auto
local e:{auto=auto} = {hello = "world"} -- And here too!

-- We don't infer types on functions however we can infer the arguments and
-- return values if passed like this (or as an argument to a function, etc...)
local f:(number, number):number = function(a, b)
	return a + b
end

-- Sometimes you might want to ignore this so you can specify custom types or
-- use the `any` type:
local f:(number, number):number = function(a:any, b:any)
	return a + b
end

-- This also allows you to create generic tables:
local f:{any} = {1, "Hello"}
```

## Errors
It is worth noting that these errors are not 'fatal'. Compilation will
still happen so they can be ignored - it just defines a series of
best practices.
```lua
local a:auto = 1
a = "HELLO" -- cannot assign string to number
a[1] -- Cannot index number
a() -- Cannot call number
a[1] = "Thing" -- Cannot index number

local b:{number} = { "hello" }
b[1] = 2 -- Cannot assign string to number

local function e(x:number, y:number):number
	return x + y
end

e(1) -- Cannot call e with 1 argument
e(1, "2") -- Attempt to parse string as number in argument 2
local a:string = e(1, 1) -- Cannot assign number to string
```

### Comparing functions
Functions can be passed about like any other type, following these rules.
 - Any finite arg function can be converted to a vararg (`...`) function.
 - Functions can be converted to ones returning less values: `:[number, number]` => `:number` => `:void`
 - Functions can be converted to ones taking more arguments: `()` => `(number)` => `(number, string)`

## Extra 'structures'
### Interfaces
Interfaces are based of TypeScript's way of handing them. Instead of explicit inheritance, interfaces say: "You should have these members".

```lua
interface Person has
	name:string
	age:number
end

local fred:Person = {
	name = "Fred",
	age = 20
} -- We know it has the fields, so we can say it is an instance of Person

interface Genius has -- Or you can do `interface Genius extends Person has`
	name:string
	age:number
	iq:number
end

local bert:Genius = {
	name = "Fred",
	age = 20,
	iq = 200,
}

-- This is totally fine as a `Genius` has the same fields as a `Person`
local normal:Person = bert

-- Interfaces can be implicit:
local function say_hello(named:{name:string}):void
	print("Hello" .. named.name)
end
```

### `alias`
Aliases simply resolve a more complicated type to a simple to remember name:
```lua
alias operator:(number, number):number
local add:operator = function(x, y)
	return x + y
end

-- All aliases resolve to their equivilents:
alias binary_operator:(number, number):number
local different:binary_operator = add
```

### `tiny`
Tiny has a similar syntax to `alias`, but two identical `tiny`s do not resolve to each other.
```lua
tiny operator:(number, number):number
local add:operator = function(x, y)
	return x + y
end

-- Choosing a random number isn't the same as a binary operator
tiny random_between:(number, number):number

-- So this produces an error
local different:random_between = add

-- You can however do this as `tiny` instances resolve to
-- and from their base type
local temp:(number, number):number = operator
different = operator
```

This is most useful if you want to have two fields which represent different concepts but have the same base data type. For instance you could have `age:number` and `iq:number` `tiny`s and you couldn't pass a `iq` to an `age` field by mistake.
