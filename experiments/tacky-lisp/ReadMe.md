# Tacky Lisp
A hacked together Lisp varient which compiles to Lua.

 - Features more complex data types. Whilst this removes Lisp's simplicity it
   makes the language faster.

## Roadmap

 - Immediate:
   - Don't break on unbound variables

 - Short term:
   - Allow defining variables after their usage on the top level.
   - Begin work on standard library
   - Statically bind symbols: hygenic macros
   - Namespaces
   - Optimisations
     - Strip unused symbols

 - Medium term:
   - Pattern maching
   - Port compiler to tacky-lisp
   - Documentation generation
   - Optimisations
     - Constant propagation
     - Function inlining
     - Inline Lua operators

 - Long term:
   - `unquote` in top level to "escape" compiler?
   - Typing
   - Optimisations
     - Convert tail recursive functions to loops
