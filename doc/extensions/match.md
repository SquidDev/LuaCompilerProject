# Extension `switch`: structural pattern matching
Pattern matching is an extremely pleasant and powerful way to handle treelike structures, such as ASTs. Unsurprisingly, it’s a feature found in most ML-inspired languages, which excel at compilation-related tasks. There is a pattern matching extension for metalua, which is extremely useful for most meta-programming purposes.

## Purpose
First, to clear a common misconception: structural pattern matching has absolutely nothing to do with regular expresssions matching on strings: it works on arbitrary structured data. When manipulating trees, you want to check whether they have a certain structure (e.g. a `` `Local`` node with as first child a list of variables whose tags are `` `Id`` ); when you’ve found a data that matches a certain pattern, you want to name the interesting sub-parts of it, so that you can manipulate them easily; finally, most of the time, you have a series of different possible patterns, and you want to apply the one that matches a given data. These are the needs addressed by pattern matching: it lets you give a list of (`pattern -> code to execute if match`) associations, selects the first matching pattern, and executes the corresponding code. Patterns both describe the expected structures and bind local variables to interesting parts of the data. Those variables’ scope is obviously the code to execute upon matching success.

## Switch statement A `switch` statement has the form:
```lua
switch <some_value> do
	case <pattern_1> then <block_1>
  case <pattern_2> then <block_2>
	...
	case <pattern_n> then <block_n>
end
```
There can be multiple case statements for one block - if an match the block is executed
```lua
switch <some_value> do
	case <pattern_1>
	case <pattern_1_bis > then
		<block_1>
	...
end
```

When the match statement is executed, the first pattern which matches `<some value>` is selected, the corresponding block is executed, and all other patterns and blocks are ignored. If no pattern matches, an error "mismatch" is raised. However, we’ll see it’s easy to add a catch-all pattern at the end of the match, when we want it to be failproof.

## Patterns definition
### Atomic litterals
Syntactically, a pattern is mostly identical to the values it matches: numbers, booleans and strings, when used as patterns, match identical values.
```lua
switch x do
	case 1 then print ’x is one’
	case 2 then print ’x is two’
end
```

### Tables
Tables as patterns match tables with the same number of array-part elements, if each pattern field matches the corresponding value field. For instance, `{1, 2, 3}` as a pattern matches `{1, 2, 3}`but also matches value `{1, 2, 3, foo=4}`. However pattern `{1, 2, 3, foo=4}` doesn’t match value `{1, 2, 3}`: there can be extra hash-part fields in the value, not in the pattern.

Notice that field `tag` is a regular hash-part field, therefore `{1, 2, 3}` matches `` `Foo{1, 2, 3}`` (but not the other way around). Of course, table patterns can be nested. The table keys must currently be integers or strings. It’s not difficult to add more, but the need hasn’t yet emerged.

If you want to match tables of arbitrary array-part size, you can add a ”...” as the pattern’s final element. For instance, pattern `{1, 2, ...}` will match all table with at least two array-part elements whose two first elements are 1 and 2.

## Identifiers
The other fundamental kind of patterns are identifiers: they match everything, and bind whatever they match to themselves. For instance, pattern `1, 2, x` will match value `1, 2, 3`, and in the corresponding block, local variable `x` will be set to 3. By mixing tables and identifiers, we can already do interesting things, such as getting the identifiers list out of a local statement, as mentionned above:
```lua
switch stat do
	 case ‘Local{ identifiers, values } then
		table.foreach(identifiers, |x| print(x[1])
... -- other cases
end
```

When a variable appears several times in a single pattern, all the elements they match must be equal, in the sense of the ”==” operator. Fore instance, pattern `{x, x}` will match value `{ 1, 1 }`, but not `{ 1, 2 }`. Both values would be matched by pattern `{ x, y }`, though. A special identifier is `_`, which doesn’t bind its content. Even if it appears more than once in the pattern, metched value parts aren’t required to be equal. The pattern `_` is therefore the simplest catch-all one, and a match statement with a `case _ then` final statement will never throw
a ”mismatch” error.

## Guards
Some tests can’t be performed by pattern matching. For these cases, the pattern can be followed by an ”if” keyword, followed by a condition.
```lua
switch x do
	case n if n%2 == 0 then print ’odd’
	case _ then print ’even’
end
```

Notice that the identifiers bound by the pattern are available in the guard condition. Moreover, the guard can apply to several patterns:
```lua
switch x do
	case n
	case {n} if n%2 == 0 then print ’odd’
	case _ then print ’even’
end
```
### Multi-match
If you want to match several values, let’s say ’a’ and ’b’, there’s an easy way:
```lua
switch {a,b} do
	case {pattern_for_a, pattern_for_b} then block
	...
end
```
However, it introduces quite a lot of useless tables and checks. Since this kind of multiple matches are fairly common, they are supported natively:
```lua
switch a, b do
	case pattern_for_a, pattern_for_b then block
	...
end
```
This will save some useless tests and computation, and the compiler will complain if the number of patterns doesn’t match the number of values.

### String regular expressions
There is a way to use Lua’s regular exressions with match, through the division operator `/`: the left operand is expected to be a literal string, interpreted as a regular expression. The variables it captures are stored in a table, which is matched as a value against the right-hand-side operand. For instance, the following case succeeds when foo is a string composed of 3 words separated by spaces. In case of success, these words are bound to variables `w1`, `w2` and `w3` in the executed block:
```lua
switch foo do
	case "ˆ(%w+) +(%w+) +(%w+)$"/{ w1, w2, w3 } then
		do_stuff (w1, w2, w3)
end
```
