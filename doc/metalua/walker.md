# The tree walker
When you write advanced macros, or when you’re analyzing some code to check for some property, you often need to design a function that walks through an arbitrary piece of code, and does complex stuff on it. Such a function is
called a code walker. Code walkers can be used for some punctual adjustments, e.g. changing a function’s return statements into something else, or checking that a loop performs no break, up to pretty advanced transformations,
such as CPS transformation (a way to encode full continuations into a language taht doesn’t support them natively; see Paul Graham’s On Lisp for an accessible description of how it works), lazy semantics... Anyway, code walkers are tricky to write, can involve a lot of boilerplate code, and are generally brittle. To ease things as much as possible, Metalua comes with a walk library, which intends to accelerate code walker implementation. Since code walking is intrinsically tricky, the lib won’t magically make it trivial, but at least it will save you a lot of time and code, when compared to writing all walkers from scratch. Moreover, other people who took the time to learn the walker generator’s API will enter into your code much faster.


## Principles
Code walking is about traversing a tree, first from root to leaves, then from leaves back to the root. This tree is not uniform: some nodes are expressions, some others statements, some others blocks; and each of these node kinds is subdivided in several sub-cases (addition, numeric for loop...). The basic code walker just goes from root to leaves and back to root without doing anything. Then it’s up to you to plug some action callbacks in that walker, so that it does interesting things for you.

Any method starting with `traverse` is a generic method to visit a particular group of elements (statements, expressions, etc...), any method starting with `visit` is a node specific visitor. Almost all methods in the visitor accept the current node as the first argument and successive parents as the next arguments - though most of the time you can discard them.

## Examples
The codewalker is defined using the `class` extension to enable easier overriding. Lets build the implest walker possible:

```lua
local Walker = require 'metalua.treequery.walker'

class MyCustomWalker extends Walker
end

MyCustomWalker():guess(node)
```

The `:guess(<node>)` method chooses the best starting point based off a node and visits from it. This is the best entry point into a node.

Lets find something that removes assert expressions.
```lua
class MyCustomWalker extends Walker
	function visitCall(node, ...)
		match node[1] with
			case `Id "assert" then return `Nil
			case _ then
		end

		return self.super.visitCall(self, node, ...)
	end
end
```
You may note we don't remove statements. This is because returning `nil` is currently broken. Sorry.

## `traverse*` methods
The `Walker` class provides some traverse functions depending on the type.

- `traverseStatement`: This is called on all statement nodes.
- `traverseExpression`: This is called for all expression nodes.
- `traverseBlock`: This is used to visit every child node in a list of statements.
- `traverseDeclaration`: This is used when visiting declarations of variables, through the counter in for loops or  in `local x = y` and `local function x() end` statements.

The `Walker` class also provides scope tracking through the `pushScope` and `popScope` functions. These do nothing but can be overridden in a parent class.

## Error handling
There are three major error functions:

 - `unknown`: Called when there is not a visitor associated with this node type.
 -  `unexpected`: Called when this tag type was unexpected (wanted expression and got statement).
 -  `malformed`: Called when a node has a tag and a list was expected.
