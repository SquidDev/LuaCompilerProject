#!/usr/bin/env lua
-------------------------------------------------------------------------------
-- Copyright (c) 2006-2013 Fabien Fleutot and others.
--
-- All rights reserved.
--
-- This program and the accompanying materials are made available
-- under the terms of the Eclipse Public License v1.0 which
-- accompanies this distribution, and is available at
-- http://www.eclipse.org/legal/epl-v10.html
--
-- This program and the accompanying materials are also made available
-- under the terms of the MIT public license which accompanies this
-- distribution, and is available at http://www.lua.org/license.html
--
-- Contributors:
--	Fabien Fleutot - API and implementation
--
-------------------------------------------------------------------------------

-- strict.lua
-- checks uses of undeclared global variables
-- All global variables must be 'declared' through a regular assignment
-- (even assigning nil will do) in a main chunk before being used
-- anywhere or assigned to inside a function.
-- distributed under the Lua license: http://www.lua.org/license.html

local error, rawset, rawget = error, rawset, rawget

-- Main file for the metalua executable
require 'metalua.loader' -- load *.mlua files
require 'metalua.compiler.globals' -- metalua-aware loadstring, dofile etc.

local pp = require 'metalua.pprint'
local mlc = require 'metalua.compiler'

local options = require "argparse" "metalua"
options:description [[
Compile and/or execute metalua programs. Parameters passed to the
compiler should be prefixed with an option flag, hinting what must be
done with them: take tham as file names to compile, as library names
to load, as parameters passed to the running program... When option
flags are absent, metalua tries to adopt a "Do What I Mean" approach:

- if no code (no library, no literal expression and no file) is
	specified, the first flag-less parameter is taken as a file name to
	load.

- if no code and no parameter is passed, an interactive loop is
	started.

- if a target file is specified with --bytecode or --source, the program is not
	executed by default, unless a --run flag forces it to. Conversely,
	if no --output target is specified, the code is run unless ++run
	forbids it.
]]

options:flag "--print-lineinfo" "-A" :description "Print the resulting AST and associated lineinfo"
options:flag "--print-ast"      "-a" :description "Print the resulting AST"
options:flag "--print-src"      "-S" :description "Print the resulting AST"
options:flag "--verbose"        "-v" :description "Print verbose output"
options:flag "--run"            "-x" :description "Run the result"

options:option "--file"        "-f" :description "Compile this file"    :count "*"
options:option "--library"     "-l" :description "Require this library" :count "*"
options:option "--literal"     "-e" :description "Compile this literal" :count "*"
options:option "--source"      "-s" :description "Save the source to this file"
options:option "--bytecode"    "-b" :description "Save the bytecode to this file"
options:option   "--shebang"        :description "Add a shebang and chmod the output files" :default "/usr/bin/env lua5.1"  :defmode "arg"
options:option "--interactive" "-i" :description "Launch the REPL"
options:option "--arg"              :description "Arg to parse when running" :count "*"

options:argument "file" :description "Compile this file" :args "*"

local function parser(...)
	local cfg = options:parse({...})
	cfg.chunks = {}
	for _, t in ipairs({ "library", "literal", "file" }) do
		for _, item in ipairs(cfg[t]) do
			table.insert(cfg.chunks, {tag = t, item})
		end
		cfg[t] = nil
	end

	return cfg
end

local function main(...)
	local cfg = parser(...)

	-- Print messages if in verbose mode
	local function verbose (fmt, ...)
		if cfg.verbose then
			pp.printf("[ "..fmt.." ]", ...)
		end
	end

	if cfg.verbose then
		verbose("raw options: %s", cfg)
	end

	-- If nothing to do, run REPL loop
	if not next(cfg.chunks) and not cfg.interactive then
		verbose "Nothing to compile nor run, force interactive loop"
		cfg.interactive=true
	end


	-- Run if asked to, or if no --source/--bytecode has been given and we have an input file
	-- if cfg.run==false it's been *forced* to false, don't override.
	if next(cfg.chunks) and not cfg.run and not cfg.source and not cfg.bytecode then
		verbose("No output file specified; I'll run the program")
		cfg.run = true
	end


	-- Get ASTs from sources
	local code = { }
	local lastFile
	for i, x in ipairs(cfg.chunks) do
		local compiler = mlc.new()
		local tag, val = x.tag, x[1]
		verbose("Compiling %s", x)
		local st, ast
		if tag=='library' then
			-- Its a library - just require it
			ast = {
				tag='Call',
				{ tag='Id', "require" },
				{ tag='String', val },
			}
		elseif tag=='literal' then
			ast = compiler:src_to_ast(val)
		elseif tag=='file' then
			ast = compiler:srcfile_to_ast(val)
			-- Isolate each file in a separate fenv
			ast = {
				tag='Call',
				{ tag='Function', { { tag='Dots'} }, ast },
				{ tag='Dots' },
				source = '@' .. val,
			}
			code.source = '@'..val

			lastFile = i
		else
			error("Bad option " .. tag)
		end

		ast.origin = x
		table.insert(code, ast)
	end

	-- The last file returns the whole chunk's result
	if lastFile then
		-- transform	+{ (function(...) -{ast} end)(...) }
		-- into	+{ return (function(...) -{ast} end)(...) }
		local prv_ast = code[lastFile]
		local new_ast = { tag='Return', prv_ast }
		new_ast.source, new_ast.origin = prv_ast.source, prv_ast.origin
		prv_ast.source, prv_ast.origin = nil, nil
		code[lastFile] = new_ast
	end

	-- Further uses of compiler won't involve AST transformations:
	-- they can share the same instance.
	-- TODO: reuse last instance if possible.
	local compiler = mlc.new()

	-- AST printing
	if cfg.print_ast or cfg.print_lineinfo then
		verbose "Resulting AST:"
		local ppConfig = {hide_hash = true}
		if cfg['print-ast-lineinfo'] then ppConfig.hide_hash = false end
		for _, x in ipairs(code) do
			pp.printf("--- AST From %s: ---", x.source)
			print(pp.tostring(x, ppConfig))
		end
	end

	-- Source printing
	if cfg.print_src then
		verbose "Resulting sources:"
		for _, x in ipairs(code) do
			print(compiler:ast_to_src(x))
		end
	end

	local bytecode = compiler:ast_to_bytecode(code)

	-- Insert #!... command
	local shebang = cfg.shebang
	if shebang then
		verbose("Adding shebang directive %q", shebang)
		if not shebang:match'^#!' then shebang = '#!' .. shebang end
		if not shebang:match'\n$' then shebang = shebang .. '\n' end
	end

	-- Save to file
	if cfg.bytecode then
		verbose ("Saving to file %q", cfg.bytecode)
		local file, err_msg = io.open(cfg.bytecode, 'wb')
		if not file then error("can't open bytecode file: "..err_msg) end
		if shebang then file:write(shebang) end
		file:write(bytecode)
		file:close()
		if shebang and os.getenv "OS" ~= "Windows_NT" then
			pcall(os.execute, 'chmod a+x "'..cfg.bytecode..'"')
		end
	end

	if cfg.source then
		verbose("Saving src to file %q", cfg.source)
		local file, err_msg = io.open(cfg.source, 'w')
		if not file then error("can't open source file: "..err_msg) end

		if shebang then file:write(shebang) end
		file:write(compiler:ast_to_src(code))
		file:close()

		if shebang and os.getenv "OS" ~= "Windows_NT" then
			pcall(os.execute, 'chmod a+x "'..cfg.source..'"')
		end
	end

	-- Run compiled code
	if cfg.run then
		verbose "Running"
		local f = compiler:bytecode_to_function(bytecode)
		bytecode = nil
		-- FIXME: isolate execution in a ring
		-- FIXME: check for failures
		local function print_traceback (errmsg)
			return errmsg .. '\n' .. debug.traceback ('',2) .. '\n'
		end
		local st, msg = xpcall(function() f(unpack(cfg.arg)) end, print_traceback)
		if not st then
			error(msg, 0)
		end
	end

	-- Run REPL loop
	if cfg.interactive then
		verbose "Starting REPL loop"
		require 'metalua.repl'.run(cfg)
	end

	verbose "Done"

end

return main(...)
