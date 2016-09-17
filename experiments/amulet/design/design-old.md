# Amulet
Another ML clone

## Type system
### Primitives
There are a series of "primitive" types. These make up all other types.

 - `Boolean`: Represents either `true` or `false`
 - `Integer`: Represents all integers.
 - `Real`: A floating point number.
 - `Unit`: The empty type.
 - `Nothing`: This type has no concrete value and so can be used to represent functions that never exit (they loop
   forever). This is based off Scala's [`Nothing`](http://stackoverflow.com/questions/13539822/whats-the-difference-between-unit-and-nothing) type.

There is also a `Value` type. This is not a primitive but a base type that everything can be assigned to.

> For interop with other languages it may be worth considering a `Dynamic` type. However this can probably be handled as
  a `Value` with a cast instead: ensuring type safty is not lost.

### Functions
The type system also contains functions, which map a value from one type to another: `a -> b`. Multiple arguments are
handled by returning another function (partial application): `a -> b -> c`. Functions also contain a list of properties:

 - `Impure`: Depends on mutable state, either in its arguments or upvalues. Consequently it may return different values
   each time it is called. A pure function can be called without side effects and will yield the same values each time
   as long as there is no mutation between each call.
 - `Mutates`: Mutates arguments or upvalues. This implies `Impure` (all `Mutates` functions are also `Impure`).

Properties propagate through code: any method that consumes a `Impure` function is also `Impure` itself. Properties can
be used as generic parameters, though they are inferred automatically.

### Tuples
These types can be combined together to form tuples. Tuples are composed of a fixed number of items. A complex number
could be written as a tuple of two real numbers (`real * real`). Tuples can also be passed as arguments to functions.

### Compound types
Whilst functions, primitives and tuples form the basis of the type system, the type system also contains records and
tagged unions.

A tagged union (also known as a discriminated union or ADT) is a type that can represent more than one type. Each type
is associated with a name or string key.

```ocaml
type Tree<'t> =
 | Leaf of 't
 | Branch of 't * Tree<'t> * Tree<'t>
```

Creating a union also creates type aliases in a sub module:
```ocaml
module Tree =
  type Leaf<'t> = 't
  type Branch<'t> = 't * Tree<'t> * Tree<'t>
```

Record types are an mapping of string keys to values.

```ocaml
type Person = { Name: string; Age : int }
```

Each pair is called a "member" and is composed of several elements:
 - Name of the field
 - Type of the field
 - Mutable
 - Access level: One of:
   - `Internal` (current namespace)
   - `Private` (only this record or associated methods)
   - `Public` (accessible to everone).

Records are not structurally typed: two identical record types are not equatable.

Unions can also contain records types.

### Traits/Interfaces/Type classes
These are used to describe a series of operations that can be applied to a type. For instance the `Hashable` trait
allows getting a hash code of an object:

```ocaml
trait Hashable of 't =
    hashCode : 't -> int
```

Traits can be implemented with the `impl` keyword:
```ocaml
impl Hashable for Tree<_> =
    member hashCode this = match this with ...
```
These traits can then be used as constraints on a generic type:
```ocaml
let hashObject<'t : Hashable>
```

### Special types
This documents some types which the compiler is aware of and have special significance:

#### `Lazy<'t>`
Not 100% sure about this. This represents a type thats calculation is deferred until needed. There is an implicit
conversion from any expression to a `Lazy<'t>` type. This allows defining "short circuiting" operators (such as `and`
or `or`) in Amulet:

```ocaml
let (or) left (right : lazy<'t>) = if left then left else right
printfn "%A" (a or calculate a)
(* Evaluates to *)
printfn "%A" (a or (fun () -> calculate a))
```

An alternative solution to this would be to allow macros.

#### `Task<'t>`
This represents a computation that requires waiting for external input (such as IO). If a function awaits on a
`Task<'t>` object, then it also must return a `Task<'t>`: asyncronous-ness propagates through your code (like
properties).

## Error model
Amulet works on the principal that their are two types of errors:

 - Environment errors: errors which occur due to the external environment: such as invalid user input, writing to
   read-only files, etc... These errors should be recoverable.
 - Programmer errors: bugs in the program due to lack of validation: out-of-bounds access, null pointers. These errors
   are generally unrecoverable.

Functions which cause environment errors (known as "exceptions") are marked with the `Errors` property. These can be caught with
the `try ... catch ...` expression.

Programmer errors (or "panics") can occur anywhere, but cannot be caught. However a separate coroutine can be created
if error recovery is needed.

## Language
### Identifiers
Identifiers can be in three forms:

 - Conventional form: `result`
 - Operator form: `(+)`. Wrapping something in parenthesis switches it between being an operator and a normal function:
 - Backtick form: ``` ``Allows any string literal`` ```.

### Expressions
Most expressions follow normal conventions. There are some key things to note

#### Function calls
Putting two expressions after each other is considered a function call. This is left associative.
```ocaml
f 2 3 (* Calls function "f" with argument "2", then calling the result of that with "3" *)
```

#### Lambdas
Lambdas are specified with the `fun` keyword, then an argument list followed by `->` and the function body:

```ocaml
(* Inferred types *)
let _ = fun x -> x * 2

(* Explicit types declaration *)
let _ = fun (x : int) : int -> x * 2
```
> It might be nice to use Haskell's lambda syntax: `\x -> x * 2` as it is less verbose. We shouldn't have both however.

To have a function that takes no arguments, you must specify a unit (`fun () -> something)`.
#### Object constructors
Arrays, literals and records have a custom syntax to allow instantiation. Different values are separated by a new line
or semicolon.

```ocaml
let array = [| 1; 2; 3 |]
let list = [ 1; 2; 3 ]
let record = { Name: "Hello"; Age : 23 }
let explicitRecord = { Person.Name: "Bill"; Age : 23 }
```

You can use the `new` keyword to create a record type from a constructor:

```ocaml
let person = new PersonItem("Bill", 23)
```

#### Tuples
Tuples are created when separating two items with a comma (for example `1, 2`). This forms a chain of values, rather
than a nested tree of tuples.

```ocaml
let _ : int * int * int = 1, 2, 3
let _ : int * (int * int ) = 1, (2, 3)
```

#### Handling errors
There are two possible ways to deal with this:
 - `try` expression: of the form `try <body> catch <handler`
 - `try` function: a function that takes a lambda and returns a union of `Success of 't | Failure of Exception`

### Statements
#### `let`
Let is used to define and declare a variable. There are two forms of the let statement:
 - Conventional variable assignment. This allows basic destructuring and mutable variables (`mutable`)
 - Function assignment. This allows multiple functions (`and` keyword) and recursive functions (`rec`)

##### Destructing assign
This is a very minimal form of pattern matching.
```ocaml
let _ = "Hello" (* Single variable *)
let a, b = 1, 2 (* Assign from tuple *)

let { Name = name; Age = _ } = aPerson (* The same as let name = aPerson.Name *).
```

##### Function assign
Function assignments are similar to lambda assignments. It is composed of the name, argument list, return type and then
function body.

```ocaml
(* Explicit form *)
let add (a : int) (b : int) : int = a + b

(* Inferred form *)
let add a b = a + b

(* Recursive form *)
let rec fact x = if x = 0 then 1 else x * fact (x - 1)

(* Mutually recursive *)
let rec doEven x = if x = 0 then 1 else doOdd (x - 1)
and doOdd = if x = 1 then 0 else doEven (x * 2)
```

When occurring in a module of type definition, you can use the `internal` or `private` specifier to give the variable
access modifiers:
```ocaml
let private a = "hello"
let internal add a b = a + b
```
In recursive definitions these modifiers apply to all functions defined.

For operators you can also specify associativity with `infixl` and `infixr`. This defaults to left:

```ocaml
let (+) a b = add a b (* Left associative *)
let infixr (+) a b = add a b (* Right associative *)
```

> There needs to be a way to specify precedence. We could use a syntax similar to Haskell's `infixr precedence`
  (such as `let infixr 10 (+)`). An alternative would be to use a [katahdin](https://github.com/chrisseaton/katahdin)
  style `precedence` operator(`precedence (+) = (-)`) to allow sorting operators instead of using explicit integers.

#### `use`
This defines a variable which will be disposed of when the scope is exited.

```ocaml
use foo = File.open "something.txt"
printfn "%s" foo.readAll()
(* Foo closed here *)
```

#### `module`
This can be used to define the module a piece of code lies in. It takes the form `module <name> = `. If it is the first
code line in a program, then the entire file is taken to be in the specified module. `module` can only appear in the top
level of the program or inside other `module` blocks: it cannot occur within types or functions.

```ocaml
module FizzBuzz

(* Is in the module FizzBuzz.Helpers *)
module Helpers =
    let isFizz x = x % 3 = 0
    let isBuzz x = x % 5 = 0
    let getString x =
        if isFizz x && isBuzz x then "Fizz buzz"
        elseif isFizz x then "Fizz"
        elseif isBuzz x then "Buzz"
        else string x

(* Is in the module FizzBuzz *)
let fizzBuzz last = range 0 last |> Seq.map Helpers.getString |> Seq.toList
```

> As modules represent a shared state, the shouldn't contain mutable variables, or variables of a mutable type.
  If you need a global state, it is recommended you pass around a `state` variable. However this is a bit of a pain for
  larger projects.

#### `open`
The `open` keyword allows importing a module into the current scope. `open` must be the first statements (or directly
after the initial `module` declaration). `open` allows importing modules as an alias or only importing specific values.

```ocaml
open FizzBuzz
printfn "%A" (fizzBuzz 10)
printfn "5 is %s" (Helpers.getString 5)

open FizzBuzz.Helpers with (getString)
printfn "3 is %s" (getString 5)

(* Alias functions or modules *)
open FizzBuzz as FB                              (* Use FB.fizzBuzz *)
open FizzBuzz.fizzBuzz as getFB                  (* Use getFB *)
open FizzBuzz.Helpers with (getString as getStr) (* Use getStr *)
```

If conflicting symbols are imported then there are several strategies for resolving the conflict:
 - Types and variables: the more recent type is imported
 - Modules: modules are merged. If another `Helpers` module is imported then you can use `Helpers` to access both
   modules.
 - Trait implementations: Neither are used. An error is reported if a trait is consumed.

#### `type`
Type is used to define types and type extensions. A type can either be of several forms:

 - Basic aliases: `type A = int` or `type EqMap<'t> = map<'t, 't>`
 - Unions: `type A = Number of int | String of string`
 - Records: `type A = { Name: string; Age: int }`
 - Records with methods: `type A() = ()`

##### Type extensions
Amulet does not actually have methods on classes: all method calls are implemented as "extension methods". This allows
calling a method using `this.MethodName` or `Module.MethodName this`.

```ocaml
type List<'t> with (* Constraints can be used, but not nested types, like normal type declarations *)
    member average this = this.foldl (+) 0 / (this.length)
let a = [1; 2; 3]
```

These member definitions are also placed within the module that they are defined.

> This may not be the nicest way of doing things: people may be confused why type definitions are masking other
  variables. An alternative strategy would be to create a module for that type name, the same way we do for classes,
  so you use `List.average` instead.

##### Classes
Classes are implemented as records with type extensions and an explicit constructor.

```ocaml
type PersonItem(name : string, age : int) =
    member Name this = name
    member Age this = age
```
is implemented under the hood as
```ocaml
type PersonItem = { internal name : string; internal age : int }
module PersonItem =
      type PersonItem with
            member Name this = this.name
            member Age this = this.age
```
Note that the function definitions are placed within their own module: allowing `PersonItem.Age`.

##### Properties
Properties are a way of implementing mutable state on types. They are composed of a getter and setter.
```ocaml
type AgablePerson = { internal name : string; internal mutable age : int }
type AgablePerson with
    member Age with
        get this = this.age
        set this age = this.age <- age
```

In classes, there is an implicit property syntax:

```ocaml
type AgablePerson(name : string, age : int) =
    member Name = name
    member Age = age with get, set
```

These members can be accessed with `<Name>` and set with `set<Name`>. Property syntax is just sugar for functions which
accept one or two arguments.

> The above syntax is ambiguous and so should be improved.

#### `trait` and `impl`
`trait` is used to define type classes, while `impl` is used to define an implementation of a type class.

```ocaml
trait Hashable of 't =
    hash : 't -> int
impl Hashable for Tree<_> =
    member hash this = match this with ...
```

Traits implicitly create a module with all trait methods defined, allowing `Hashable.hash object`.

> If a trait requires a constraint (such as `Comparable` requiring `Equatable`) should the required trait be resolved at
  use time or definition time?
