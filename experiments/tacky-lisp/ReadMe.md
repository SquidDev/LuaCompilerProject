# Tacky Lisp
A hacked together Lisp varient which compiles to Lua.

 - Features more complex data types. Whilst this removes Lisp's simplicity it
   makes the language faster.

## Roadmap

 - Immediate:
   - Support for `quasi-quote`/`unquote` in resolver
   - Don't break on recursive definitions
   - Don't break on unbound variables

 - Short term:
   - Allow defining variables after their usage on the top level.
   - Make parser much more stable and general error reporting
     - Include macro in error location (if relevant)
   - Begin work on standard library
   - All symbols are statically bound

 - Medium term:
   - Pattern maching
   - Port compiler to tacky-Lisp
   - Documentation generation

 - Long term:
   - `unquote` in top level to "escape" compiler?
   - Typing
