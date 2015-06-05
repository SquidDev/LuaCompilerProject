# Internal representation of types
There are several type definitions used internally. There are three 'intermittant ones':

- `` `NamedType { "<name>" }`` This is used for type identifiers such as `number`, `String`.
- `` `GenericType { `NamedType, [type arguments] }`` This is created from generics such as `Array<Number>`.
- `` `InferedType`` Used in the case of `auto`.
- `` `UnknownType`` Used when no type is specified. This is generally replaced with `any` but in some cases may vary

`` `Type { [properties]... }``. is resolved from a reference of `` `NamedType``. It is also directly created from Objects.

## Very basic types
```lua
local a:number -- `NamedType "number"
local b:auto   -- `InferType
local c        -- `UnknownType

-- The `a` parameter is `UnknownType { } and will be replaced with `NamedType "number"
local d:(number):number = function(a) return a end
```

## Generics
```lua
local a:{number}        -- `GenericType { `NamedType "Array", `NamedType "number" }
local b:{string=number} -- `GenericType { `NamedType "Map", `NamedType "string", `NamedType "number" }
local c:string|number   -- `GenericType { `NamedType "Union", `NamedType "string", `NamedType "number" }
local d:Strict<Number>  -- `GenericType { `NamedType "Strict", `NamedType "Number" }
```

## Functions
Functions are represented as generics, all but the last type arguments are the argument, the last being the return value.

```lua
local a:(string):number -- `GenericType { `NamedType "Function", `NamedType "string", `NamedType "number" }
```

Functions also have several additional types as covered more widely in TypeDefinitions

```lua
local a:(...:string):[string, ...:number]
--[[
`NamedType { "Function",
	`GenericType { `NamedType "Dots", `NamedType "String" },
	`GenericType { `NamedType "Tuple", `NamedType "String", `GenericType { `NamedType "Dots", `NamedType "String" } }.
}
]]
```

## Objects
Objects are represented through the `` `Type`` construct:

```lua
local a:{name:string, age:number}
--[[
	`Type {
		`Property { `String "name", `NamedType "string" },
		`Property { `String "age", `NamedType "number" },
	}
]]
```

Interfaces are represented the same way, though with an additional `name = ` field:

```lua
interface Person -- `Type { name = "Person", ...	}
	name:string, age:number
end

-- `NamedType "Person"
local a:Person
```

### Metamethods
Methods and Metamethods  are represented by the `` `Method { "<name> ", [function arguments]... }`` and `` `MetaMethod { "<name> ", [function arguments]... }`` respectively.

```lua
interface Person
	name:string,
	age:number,
	function sayHello():void,
	meta function add(Person, Person):Person,
end
--[[
	`Type { name = "Person",
		`Property { `String "name", `NamedType "string" },
		`Property { `String "age", `NamedType "number" },
		`Method { "sayHello", `NamedType "void" },
		`MetaMethod { "add", `NamedType "Person", `NamedType "Person", `NamedType "Person" },
	}
]]
```

## Inheritance
Supertypes of an interface are instances of `` `Extends { <type> }`:

```lua
interface Foo end -- `Type { name = "Foo" }

interface Bar extends Foo end
--[[
	`Type { name = "Bar",
		`Extends { `NamedType "Foo" },
	}
]]
```

## Generics
Generics are a key part of defining types. To use them, they are passed as arguments to `` `NamedType``. Generic properties are represented with: `` `Generic { "<name>", [superclasses] ...}``.

```lua
interface Foo end -- `Type { name = "Foo" }

interface Bar<T extends Foo> end
--[[
	`Type { name = "Bar",
		`Generic { "T", `NamedType "Foo" },
	}
]]
```

## Type properties
All types can hold a series of additional properties:x:
- `name:string` The friendly name of the type
- `scope:TypeScope` The scope this type was defined in.
