# Misc types
All Tua types are instances of these core types. This document discusses their behavior and how they interact.

## Any
The Any type (written `Any` or `any`) can be cast from any value and to any value. It can be indexed, called and have any operation applied to it.

```lua
local var:any = "hello"
local thing = var.thing -- thing is an instance of :any
print(var ^ 2) -- Also any

-- You get the idea...
```

## Nil
The Nil type can be converted to any value but cannot be used in any operation. It cannot be called, indexed or operated on, nor can it be used in an index.

```lua
local var:string = nil -- Nil can be cast to anything
local foo:nil = nil -- Invalid: You cannot explicitly define a variable as `nil`

local bar:auto = {}
bar[nil] = "something" -- Cannot index with nil
```

## Strict

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
