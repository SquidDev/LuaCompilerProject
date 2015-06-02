# Primative Extensions
## Lambdas/anonymous functions (Built in)
Lua lets you use anonymous functions. However, when programming in a functional style, where there are a lot of short anonymous functions simply returning an expression, the default syntax becomes cumbersome. Metalua being
functional-styel friendly, it offers a terser idiom: `function(arg1, arg2, argn) return some expr end` can be written: `|arg1,arg2,argn| some exp`.

Notice that this notation is currying-friendly, i.e. one can easily write functions that return functions: `function(x) return function(y) return x+y end end` is simply written `|x||y| x+y`.

Lua functions can return several values, but it appeared that supporting multiple return values in metalua’s short lambda notation caused more harm than good. If you need multiple returns, use the traditional long syntax.
Finally, it’s perfectly legal to define a parameterless function, as in `|| 42`. This makes a convenient way to pass values around in a lazy way.

## Algebraic Data Types (Built in)
This adds a simple shorthand for `{tag = "Id", "foo"}`: `` `Id { "foo" }``. You can also write simple expressions inline: `` `Id "foo"`` or parameterless tags: `` `Nil``

## List comprehensions (`comprehension`)
This adds a Python style syntax for list comprehensions:

```lua
local foo = {x^2 for x = 0, 10}
-- {0, 1, 4, 9, 16, 25, 36, 49, 64, 81, 100}
```

It, like Python, supports `if` statements and nesting:

```lua
local foo = {x^2 for x = 0, 10 if x % 2 == 0}
-- {0, 4, 16, 36, 64, 100}

local bar = {{y*x for y = 1, x}  for x = 1, 3}
--[[
{
	{1},
	{2, 4},
	{3, 6, 9}
}
]]
```

## Dollar macros (`dollar`)
Dollar macros offer a simple method of creating macros to use:

```lua
-{block:
	require 'metalua.extension.dollar'.register['foo'] = function(...)
		return `Call { `Id "print", ...}
	end
	extension("dollar", ...)
}

$foo(1, 2, 3) -- Replaced with `print(1, 2, 3)`
```

You can also assign ADTs to be registed:

```lua
-{block:
	require 'metalua.extension.dollar'.register['foo'] = `Call { `Id "print", `String "Something"}
	end
	extension("dollar", ...)
}

$foo

local a = $foo
```

## Export (`export`)
Export simply allows you to mark objects to be returned from a function (or file). This is aimed for modules.

```lua
-- Inline local & export
export a, b = "Hello", "World"

-- Separate declarations
local c = "Foo"
export c

export function d()
	return "bar"
end

-- return { a = a, b = b, c = c, d = d}
```

This only works in the root - it does not work in statements such as `if` or `do` to prevent undefined variable errors.

## Custom infix operators (`infix`)
In many cases, people would like to extend syntax simply to create infix binary operators. Haskell offers a nice compromize to satisfy this need without causing any mess, and metalua incorporated it: when a function is put between backquotes, it becomes infix. for instance, let’s consider the plus function `plus=|x,y|x+y`; this function can be called the classic way, as in `plus (20, 22)`; but if you want to use it in an infix context, you can also write `20 ‘plus‘ 22`.

## Custom loops and `continue` (`loops`)
This adds a series of inline loop structures.

```lua
for i = 0, 10 if i % 2 == 0 do
	print(i)
end
-- 0, 2, 3, 6, 8

for i = 0, 4 for j = 1, 3, 2 do
	print(i, j)
end

--[[
for i = 0, 4 do
	for j = 1, 3, 2 do
		print(i, j)
	end
end
]]
```

You can also mix and match `while`, `until` and `for`.

## Operator Assign (`opequals`)
This adds the much needed feature to Lua to use operator assigns:

```lua
local a = 1
a += 2
-- a = 3

a .= "Hello"
-- a = 3Hello
```

This also works for indexes - and will not have any adverse effects on your code:
```lua
local t = { x = 0 }
local function get()
	print("Called")
	return t
end

get().x += 1
-- "Called" is only printed once
```
