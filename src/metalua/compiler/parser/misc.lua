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
--	 Fabien Fleutot - API and implementation
--
-------------------------------------------------------------------------------

--- Summary: metalua parser, miscellaneous utility functions.
--
-- Exported API:
-- * [mlp.fget()]
-- * [mlp.id()]
-- * [mlp.opt_id()]
-- * [mlp.id_list()]
-- * [mlp.string()]
-- * [mlp.opt_string()]
-- * [mlp.id2string()]

local gg = require 'metalua.compiler.grammar.generator'

return function(M)
	local _M = gg.future(M)

	-- Try to read an identifier return [false] if no id is found.
	M.opt_id = gg.multisequence {
		default = function(lx)
			local a = lx:peek()
			if a.tag == "Id" then
				return lx:next()
			else
				return false
			end
		end
	}

	-- Mandatory reading of an id: causes an error if it can't read one.
	M.id = gg.multisequence {
		name = "identifier",
		function(lx) return _M.opt_id(lx) or gg.parse_error(lx, "Identifier expected") end,
	}

	M.declaration = gg.multisequence {
		name = "declaration",
		default = _M.id,
	}

	-- Common helper function
	M.id_list = gg.list { primary = _M.id, separators = "," }

	M.declaration_list = gg.list { primary = _M.declaration, separators = ','}

	-- Converts an identifier into a string. Hopefully one day it'll handle
	-- splices gracefully, but that proves quite tricky.
	function M.id2string(id)
		if id.tag == "Id" then
			id.tag = "String";
			return id
		elseif id.tag == "Splice" then
			error ("id2string on splice not implemented")
			-- Evaluating id[1] will produce `Id{ xxx },
			-- and we want it to produce `String{ xxx }.
			-- The following is the plain notation of:
			-- +{ `String{ `Index{ `Splice{ -{id[1]} }, `Number 1 } } }
			return { tag="String",  { tag="Index", { tag="Splice", id[1] }, { tag="Number", 1 } } }
		else
			error ("Identifier expected: "..tostring(id, 'nohash'))
		end
	end

	-- Read a string, possibly spliced, or return an error if it can't
	-- TODO: Remove splice
	M.string = gg.multisequence {
		default = function(lx)
			if lx:peek().tag == "String" then
				return lx:next()
			else
				error "String expected"
			end
		end
	}

	-- Try to read a string, or return false if it can't.
	-- Uses M.string
	function M.opt_string (lx)
		local m, result = pcall(_M.string, lx)
		return m and result
	end

	-- Chunk reader: block + Eof
	function M.skip_initial_sharp_comment (lx)
		-- Dirty hack: I'm happily fondling lexer's private parts
		-- FIXME: redundant with lexer:newstream()
		lx:sync()
		local i = lx.src:match("^#.-\n()", lx.i)
		if i then
			lx.i = i
			lx.column_offset = i
			lx.line = lx.line and lx.line + 1 or 1
		end
	end

	local function chunk (lx)
		if lx:peek().tag == 'Eof' then
			return { } -- handle empty files
		else
			M.skip_initial_sharp_comment (lx)
			local chunk = M.block (lx)
			if lx:peek().tag ~= "Eof" then
				gg.parse_error(lx, "End-of-file expected")
			end
			return chunk
		end
	end

	-- chunk is wrapped in a sequence so that it has a "transformer" field.
	M.chunk = gg.sequence { chunk, builder = unpack }
end
