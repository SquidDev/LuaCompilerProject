# Lua Compiler Project [![Build Status](https://travis-ci.org/SquidDev/LuaCompilerProject.svg?branch=vnext)](https://travis-ci.org/SquidDev/LuaCompilerProject)
The Lua Compiler Project (LuaCP) is an attempt to create a language agnostic approach to processing Lua code.

Wait: language agnostic? LuaCP aims to allow writing in Lua, Moonscript and Tua - a series of extensions of the basic Lua syntax. There will also be an API to add custom language frontends.

All languages will compile to the LuaCP IR. This is an gradual typed IR: types can be specified or not. Type inference can then occur on the IR. This allows custom AST nodes to be specified without the analysis tools having to know what effect they have on control flow. LuaCP will also do extensive analysis of the IR, allowing optimisations from constant folding to function inlining or automatic table/global value caching.

The aim is to take this type information and generate optimised bytecode for a series of platforms: from Lua bytecode to .Net or the JVM (and maybe LLVM). LuaCP will initially target Lua 5.3, with options to generate Lua 5.1 and 5.2 bytecode.
