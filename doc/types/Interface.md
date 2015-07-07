# Interfaces in greater depth
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

Though in the case of Objects, you can leave out the braces:

```lua
interface Animal
	name:string,
	age:number
end
```
