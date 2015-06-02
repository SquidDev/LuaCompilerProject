-------------------------------------------------------------------------------
-- Copyright (c) 2006-2014 Fabien Fleutot and others.
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

--- Compile-time metaprogramming features: splicing ASTs generated during compilation,
-- AST quasi-quoting helpers.

local gg = require 'metalua.compiler.grammar.generator'

return function(M)
	M.lexer:add({"+{", "-{"})

	local meta = { }
	local _meta = gg.future(meta)

	--- External splicing: compile an AST into a chunk, load and evaluate
	-- that chunk, and replace the chunk by its result (which must also be
	-- an AST).
	-- TODO: that's not part of the parser
	function meta.eval(ast)
		-- TODO: should there be one mlc per splice, or per parser instance?
		local mlc = require 'metalua.compiler'.new()
		local f = mlc:ast_to_function(ast, '=splice')
		local result = f(M) -- splices act on the current parser
		return result
	end

	--- Going from an AST to an AST representing that AST
	-- the only hash-part key being lifted is `"tag"`.
	-- Doesn't lift subtrees protected inside a `Splice{ ... }.
	-- e.g. change `Foo{ 123 } into
	-- `Table{ `Pair{ `String "tag", `String "foo" }, `Number 123 }
	local function lift(t)
		local cases = { }
		function cases.table(t)
			local mt = { tag = "Table" }
			if t.tag == "Splice" then
				assert(#t==1, "Invalid splice")
				local sp = t[1]
				return sp
			elseif t.tag then
				table.insert(mt, { tag="Pair", lift "tag", lift(t.tag) })
			end
			for _, v in ipairs (t) do
				table.insert(mt, lift(v))
			end
			return mt
		end
		function cases.number  (t) return { tag = "Number", t, quote = true } end
		function cases.string  (t) return { tag = "String", t, quote = true } end
		function cases.boolean (t) return { tag = t and "True" or "False", t, quote = true } end
		local f = cases [type(t)]
		if f then return f(t) else error ("Cannot quote an AST containing "..tostring(t)) end
	end
	meta.lift = lift

	--- when this variable is false, code inside [-{...}] is compiled and
	-- evaluated immediately. When it's true (supposedly when we're
	-- parsing data inside a quasiquote), [-{foo}] is replaced by
	-- [`Splice{foo}], which will be unpacked by [quote()].
	local in_a_quote = false

	--- Parse the inside of a "-{ ... }"
	function meta.splice_content(lx)
		local parser_name = "expr"
		if lx:is_keyword (lx:peek(2), ":") then
			local a = lx:next()
			lx:next() -- skip ":"
			assert (a.tag=="Id", "Invalid splice parser name")
			parser_name = a[1]
		end
		-- TODO FIXME running a new parser with the old lexer?!
		local parser = require 'metalua.compiler.parser'.new()
		local ast = parser [parser_name](lx)
		if in_a_quote then -- only prevent quotation in this subtree
			return { tag="Splice", ast }
		else -- convert in a block, eval, replace with result
			if parser_name == "expr" then
				ast = { { tag="Return", ast } }
			elseif parser_name == "stat"  then
				ast = { ast }
			elseif parser_name ~= "block" then
				error ("splice content must be an expr, stat or block")
			end

			return _meta.eval(ast)
		end
	end

	meta.splice = gg.sequence{ "-{", _meta.splice_content, "}", builder=unpack }

	-- Parse the inside of a "+{ ... }"
	function meta.quote_content(lx)
		local parser
		if lx:is_keyword(lx:peek(2), ":") then -- +{parser: content }
			local parser_name = M.id(lx)[1]
			parser = M[parser_name]
			lx:next() -- skip ":"
		else -- +{ content }
			parser = M.expr
		end

		local prev_iq = in_a_quote
		in_a_quote = true
		local content = parser (lx)
		local q_content = _meta.lift (content)
		in_a_quote = prev_iq
		return q_content
	end

	-- Apply to the expr class
	M.method_args:add({ "+{", _meta.quote_content, "}" })

	M.expr.primary:add({ "-{", _meta.splice_content, "}", builder = unpack })
	M.expr.primary:add({ "+{", _meta.quote_content, "}",  builder = unpack })

	M.expr.suffix:add({ "+{", _meta.quote_content, "}", builder = function (f, arg) return {tag="Call", f,  arg[1] } end })

	M.stat:add({ "-{", _meta.splice_content, "}", builder = unpack })

	M.opt_id:add( gg.sequence{
		"-{",
		function(lx)
			local v = _meta.splice_content(lx)
			if v.tag ~= "Id" and v.tag ~= "Splice" then
				gg.parse_error(lx, "Bad id splice")
			end
			return v
		end,
		"}",
		builder = unpack
	})

	M.string:add( gg.sequence{
		"-{",
		function(lx)
			local v = _meta.splice_content(lx)
			if v.tag ~= "String" and v.tag ~= "Splice" then
				gg.parse_error(lx, "Bad string splice")
			end
			return v
		end,
		"}",
		builder = unpack
	})

	return meta
end
