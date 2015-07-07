# Function
The Function type is simply a callable entity. The Function type literal is composed of an argument list followed by the return value. It can also be written as a generic type.

```lua
local a:(number, number):number = function(a, b) return a + b end -- Note, types on the right are implied
local b:Function<Number, Number, Number> = a
```

Function return values are the only time when the `void` type can be used.

## Varargs
Varargs are represented like any typed parameter.

```lua
function check(check:string, ...:string):boolean
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

## Multiple returns
Variable return types handle multiple return values, these are similar to function arguments but have no associated name. To specify a variable number of return values, you can use the `...:type` syntax.

```lua
local function randomData():[number, string]
	return 4, "Hello"
end

local a:auto, b:auto = randomData() -- a:number and b:string. Automatically expands

local function fib():[string, ...:number]
	local result:{number} = {}
	local a:number, b:number = 1, 1
	while a < 100 do
		table.insert(result, a)
		a, b = b, a + b
	end

	return "Numbers!", unpack(result)
end

local function concat(a:number, b:string):string
	return a .. b
end

concat(randomData())
concat((randomData())) -- As the variable is wrapped in brackets, the variable is limited to one.
concat(randomData(), "things")
```

## Subtypes
Function compatibility is complicated.

### Argument types
If every argument in the target is assignable to the original then the function can be converted.
```lua
local a:(a:string|number):string

local b:(a:string):string = a
local c:(a:number):string = a

local d:(a:boolean):string = a -- Error: boolean is not assignable to string|number
```

With a variable number of arguments, things get more complicated. If the functions are compatible but probably invalid then a warning is issued.
```lua
local a:(a:string, ...:string):string
local b:(a:string, b:string, ...:string) = a -- This issues a warning but is compatible
local c:(...:string):string = a -- Error: (a:string, ...:string) is not assignable to (...:string).
-- This is because you can technically call c with 0 arguments, but shouldn't be able to call a with 0 arguments.
```

When optional arguments are implemented, this will get more complicated.

### Return types
If the return type in the original is assignable to the target then it can be cast.
```lua
local a:():string|number
local b:():string|number|boolean = a
local c:():string = a -- Error: string|number is not assignable to string.
```
