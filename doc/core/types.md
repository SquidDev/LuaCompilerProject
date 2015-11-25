# Types
LuaCP implements a gradual type system. The type system aims to include some level of usage information too. At this stage, this is by no means a formal definition.

## Usage information
The type system aims to store information about how a type is used and what effects it can have.
```fsharp
type Mutability =
	(* The consumer of this type can modify it *)
	| Mutable
	(* The consumer of this type cannot modify it, but it may be modified elsewhere *)
	| ReadOnly
	(* The variable will never change over the usage of this value*)
	| Constant
```

## ValueType
```fsharp
(*
	Represents a primitive type.
	Integer is implicitly convertible to Number
*)
type Primitive =
	| Number
	| Integer
	| String
	| Boolean

(* Lookup table of operations, generally will be set to functions *)
type Operations = {
	add : Type,
	subtract : Type,
	(* ... *)
}

type Type =
	(*
		A literal type can only be converted to from another identical literal.
		It can be converted to a primitive of the same type.
	*)
	| Literal of Literal

	(* See above definition of primitive *)
	| Primitive of Primitive

	(* The nil type cannot be converted to or from anything *)
	| Nil
	(* The base type: everything is convertable to it *)
	| Value
	(* Convertable to and from anything. *)
	| Any
	(*
		Union of possible types.
		To be converted to this the type must be convertable to every entry.
		To be convertable from, every type must be convertable to the target.
	*)
	| Union of Type[]

	(*
		An intersection of multiple types: must be convertable to all of the types.
		In the case of tables it must have every field in every table (a sort of inheritance).
		This can be used for function overloading.
	*)
	| Intersection of Type[]

	(*
		Marks both fields and operations.
		A type matching { integer : T?, meta __len : self -> int } is considered to
		be an array.
	*)
	| Table of { fields : (Type * Type)[], operations : Operations }

	(* Marks a callable function *)
	| Function of { args : TupleType, return : TupleType }
```

## Tuple Type
A tuple type is used to mark multiple values of varying length.

```fsharp
type TupleType =
	| Single of { types: Type[], var : Type }
	(* When a function can return sets of variables (such as `false`, error message or a file handle *)
	| Union of TupleType
```
