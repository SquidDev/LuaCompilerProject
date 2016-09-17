# Amulet - Overview

Amulet is another functional-esque language.

## Aims
 - Functional language, though not purely functional
 - Immutable by default
 - Mutability is allowed, though should ideally be hidden from the public API. For instance
   you can mutate an object when creating it but not afterwards.
 - Complex type system supporting type classes
 - No mutable global state
 - Trace which methods mutate objects or depend on mutable state

> **To consider:** Dependent/refined types. The issue here is that this requires creating a
  theorum/proof solver. Personally I feel it would be better to include assertions in the type
  signature but enforce them at runtime, unless they can be proved false.

> **To consider:** How can we track mutability? How do we know if an argument is mutated, or if its
  upvalues are? Should mutable objects require an explicit `mut` to make them mutable (or the
  use the converse `const`)?

> **To consider:** Similar to the above statement: how can we track global mutable state and
  forbid it?