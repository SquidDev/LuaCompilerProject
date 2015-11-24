# Intermediate Representation
LuaCP uses a SSA based IR. Code is stored in functions (prototypes), with one function being marked as the entry point of the entire program or module. Functions are composed of arguments, upvalues and a series of blocks, with one also being marked as an entry point. Blocks could be considered as 'mini-functions', but the phi-node or argument is marked at the beginning of the block rather than the call site.

```ocaml
type Value = { type : ValueType }
type Tuple = { type : TupleType }
type Reference = { type : ValueType, mutability : Mutability }

(* A literal is the most basic value *)
type Literal : Value =
	| String of string
	| Number of float
	| Integer of int
	| Boolean of bool
	| Nil of nil

(* Instructions can be values, or not *)
type Instruction =
	(* Binary operators. *)
    | Add of { left : Value, right : Value} : Value
    | Subtract of { left : Value, right : Value} : Value
    (* ... *)

    (* Unary operators. These are Values. *)
    | UnaryMinus of { operand : Value }
    | Length of { operand : Value }
    (* ... *)

    (*
	    Table operations:
		The isArray flag marks an optimised getter/setter for tables
		which guarantees the key is an integer and within the bounds.
	*)
	(*
		Create a new table
		We store the *additional* length of the array and hash parts
	    so we can optimise creation when creating a table and then
	    setting the keys later.
	*)
    | TableNew of {
	    (* Additional length of hash/array. *)
	    arrayLength : int, hashLength : int,
	    (* Contents of hash / array *)
	    array : Value[], hash : (Value * Value)[]
	} : Value
	(* Accessor for tables. *)
	| TableGet of { table : Value, key : Value, isArray : bool } : Value
	(* Set a key in the table *)
	| TableSet of { table : Value, key : Value, value : Value, isArray : bool }

	(*
		Control flow operators
		One of these instructions must appear at the end of every
		block.
	*)
	| Return of Tuple
	| Branch of Block
	| BranchConditional of { condition : Value, true : Block, false : Block }
	(*
		Chooses a value based off another.
		This exists so we don't have to create new blocks for trivial operations
	*)
	| ValueConditional of  { condition : Value, true : Block, false : Block } : Value

	(* Tuple operations *)
	(* Create a new tuple from a list of values and a varargs *)
	| TupleCreate of { items : Value[], remainder : Tuple } : Tuple
	(* Get an element at an offset *)
	| ElementGet of { tuple : Tuple, offset : int } : Value
	(* Get the remainder of a tuple *)
	| RemainderGet of { tuple : Tuple, offset : int } : Tuple

	(* Call a function with arguments *)
	| Call of { func : Value, args : Tuple } : Tuple

	(*
		A reference is a mutable variable. We attempt to convert this
		to a phi node, but cannot when closures and mutable variables are involved.
	*)
	| ReferenceNew of { value : Value } : Reference
	| ReferenceGet of { reference : Reference} : Value
	| ReferenceSet of { reference : Reference, value : Value}

	(* Create a new closure with mutable and immutable upvalues *)
	| ClosureNew of { proto : Function, mutable : Reference[], immutable : Value[] } : Value

(* A phi node represents which value is taken from which block *)
type Phi = map<Block, Value>

type Block = {
	phis : Phi[],
	instructions: Instruction[],
}

type Upvalue = Reference | Value
type Argument = Tuple | Value

type Function = {
	immutable : Upvalue[],
	mutable : Upvalue[],
	arguments : Argument[],
	code : Block[],
	entry : Block,
}
```
