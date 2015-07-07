# Union
The union type represents a variable that is either `A` or `B`, written as `A | B`.

These follow some simple rules:
 - `A | B` is equivalent to `B | A`
 - `A | B` is equivalent to `A` is `B` is a subtype of `A`
 - `A | B | C` is equivalent to `A | (B | C)` and `(A | B) | C`

Effectively they are commutative and associative.

 - A union type `U` is the subtype of another type `T` if every type in `U` is a subtype of `T`
 - A type `T` is the subtype of a union `U` if `T` is a subtype of any type in `U`.

# Merging of Composite type
When merging composite types, fields and methods are merged:

```lua
interface A
  a:number,
  b:string,
  function c(a:number):string
  function d(a:number):string
  e:number,
end

interface B
  a:number,
  b:number,

  function c(a:string):number
  function d(a:number):number
end

interface A|B -- Not actually valid code.
  a:number,
  b:string|number, -- Types are merged. This is however read only.
  -- c is removed as the types are not convertible.
  function d(a:number):string|number -- Return types are merged
  -- e is removed as the types are not convertible.
end
```
