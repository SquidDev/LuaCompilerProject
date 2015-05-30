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

--- Non-Lua syntax extensions

local gg = require 'metalua.compiler.grammar.generator'

return function(M)
	local ext = {}
	local _M = gg.future(M)

	--- Algebraic Datatypes
	local function adt(lx)
		local node = _M.id(lx)
		local tagval = node[1]
		-- tagkey = `Pair{ `String "key", `String{ -{tagval} } }
		local tagkey = { tag="Pair", {tag="String", "tag"}, {tag="String", tagval} }
		if lx:peek().tag == "String" or lx:peek().tag == "Number" then
			-- TODO support boolean litterals
			return { tag="Table", tagkey, lx:next() }
		elseif lx:is_keyword (lx:peek(), "{") then
			local x = M.table.table (lx)
			table.insert (x, 1, tagkey)
			return x
		else
			return { tag="Table", tagkey }
		end
	end

	ext.adt = gg.sequence{ "`", adt, builder = unpack }

	M.expr.primary:add(ext.adt)

	--- Anonymous lambda
	ext.lambda_expr = gg.sequence {
		"|", _M.func_params_content, "|", _M.expr,
		builder = function (x)
			local li = x[2].lineinfo
			return {
				tag="Function", x[1],
				{ {tag="Return", x[2], lineinfo=li }, lineinfo=li }
			}
		end
	}

	M.expr.primary:add(ext.lambda_expr)

	return ext
end
