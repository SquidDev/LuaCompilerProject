local errorPositions = require "tacky.logger".errorPositions
local builtins = require "tacky.analysis.resolve".declaredVars

local function createLookup(t)
	for i = 1, #t do t[t[i]] = true end
	return t
end

local kwrds = createLookup {
	"and", "break", "do", "else", "elseif", "end", "false", "for", "function",
	"if", "in", "local", "nil", "not", "or", "repeat", "return", "then", "true",
	"until", "while",
}
local function escape(name)
	if kwrds[name] then
		return "_e" .. name
	elseif name:match("^%w[_%w%d]*$") then
		-- We explicitly forbid leading _ as that is used for compiler internals
		return name
	else
		return "_e" .. name:gsub("([^_%w%d])", function(x) return "_" .. x:byte() .. "_" end)
	end
end

local varLookup = {}
local ctrLookup = {}

local function escapeVar(var, args)
	if builtins[var] then return var.name end

	if var.isVariadic and args then
		return "..."
	end

	local v = escape(var.name)

	local id = varLookup[var]
	if not id then
		id = (ctrLookup[var.name] or 0) + 1
		ctrLookup[var.name] = id
		varLookup[var] = id
	end

	return v .. id
end

local compileBlock, compileExpression, compileQuote

function compileQuote(node, builder, level)
	if level == 0 then
		return compileExpression(node, builder)
	end

	local append = builder.add
	if node.tag == "string" or node.tag == "number" then
		if level then
			append('{tag = "' .. node.tag .. '", contents = ' .. node.contents .. '}')
		else
			append(node.contents)
		end
	elseif node.tag == "symbol" or node.tag == "key" then
		append('{tag = "' .. node.tag .. '", contents = ' .. ("%q"):format(node.contents):gsub("\n", "\\n") .. '}')
	elseif node.tag == "list" then
		local first = node[1]
		if first and first.tag == "symbol" then
			if first.contents == "unquote" or first.contents == "unquote-splice" then
				return compileQuote(node[2], builder, level and level - 1)
			elseif first.contents == "quasiquote" then
				return compileQuote(node[2], builder, level and level + 1)
			end
		end

		local containsUnsplice = false
		for i = 1, #node do
			local sub = node[i]
			if sub.tag == "list" and sub[1] and sub[1].contents == "unquote-splice" then
				containsUnsplice = true
				break
			end
		end

		if containsUnsplice then
			append('(function()')
			builder.line()
			builder.indent()

			append('local _offset = 0')
			builder.line()

			append('local _result = {tag = "list"}')
			builder.line()

			append('local _temp')
			builder.line()

			local offset = 0
			for i = 1, #node do
				local sub = node[i]
				if sub.tag == "list" and sub[1] and sub[1].contents == "unquote-splice" then
					-- Every unquote-splice subtracts one from the offset position
					offset = offset + 1

					append("_temp = ")
					compileQuote(sub[2], builder, level - 1)
					builder.line()

					append('for _c = 1, _temp.n do _result[' .. (i - offset) .. ' + _c + _offset] = _temp[_c] end')
					builder.line()
					append('_offset = _offset + _temp.n')
					builder.line()
				else
					append("_result[" .. (i - offset)  .. " + _offset] = ")
					compileQuote(sub, builder, level)
					builder.line()
				end
			end

			append('_result.n = _offset + ' .. (#node - offset))
			builder.line()

			append('return _result')
			builder.line()

			builder.unindent()
			append("end)()")
		else
			append('{tag = "list", n = ' .. #node)
			for i = 1, #node do
				append(", ")
				compileQuote(node[i], builder, level)
			end
			append('}')
		end
	else
		error("Unknown tag " .. expr.tag)
	end
end

function compileExpression(expr, builder, retStmt)
	local append = builder.add

	if expr.tag == "string" or expr.tag == "number" or expr.tag == "symbol" or expr.tag == "key" then
		if retStmt == "" then retStmt = "local _ = " end
		if retStmt then append(retStmt) end
		local contents
		if expr.tag == "symbol" then
			contents = escapeVar(expr.var)
		elseif expr.tag == "key" then
			-- TODO: Should we write this as a table instead?
			-- ATM this is kinda a kludge.
			contents = ("%q"):format(expr.contents:sub(2))
		else
			contents = tostring(expr.contents)
		end
		append(contents)
	elseif expr.tag == "list" then
		local head = expr[1]
		if head and head.tag == "symbol" then
			local name = head.contents
			if name == "lambda" then
				if retStmt == "" then retStmt = "local _ = " end
				if retStmt then append(retStmt) end

				append("(function(")
				local args = expr[2]
				for i = 1, #args do
					if i > 1 then append(", ") end
					append(escapeVar(args[i].var, true))
				end
				append(")")

				builder.indent() builder.line()

				if #args > 0 and args[#args].var.isVariadic then
					local argsVar = escapeVar(args[#args].var)
					append('local ' .. argsVar .. ' = table.pack(...) ' .. argsVar .. '.tag = "list"')
					builder.line()
				end

				compileBlock(expr, builder, 3, "return ")

				builder.unindent()
				append("end)")
			elseif name == "cond" then
				local forceClosure = not retStmt

				if forceClosure then
					append("(function()")
					retStmt = "return "
					builder.indent()
					builder.line()
				end

				local hadFinal = false
				local ends = 1
				for i = 2, #expr do
					local item = expr[i]
					local cond = item[1]

					local isFinal = cond.tag == "symbol" and cond.contents == "true"

					if not isFinal then
						if cond.tag == "list" and cond[1].contents == "cond" then
							if i > 2 then builder.indent() builder.line() end
							append("local _temp")
							builder.line()

							compileExpression(item[1], builder, "_temp = ")
							builder.line()

							append("if _temp then")

							if i > 2 then ends = ends + 1 end
						else
							append("if ")

							compileExpression(item[1], builder)
							append(" then")
						end
					elseif i == 2 then
						append("do")
					end

					builder.indent() builder.line()
					compileBlock(item, builder, 2, retStmt)
					builder.unindent()

					if isFinal then
						hadFinal = true
						break
					else
						append("else")
					end
				end

				if not hadFinal then
					builder.indent() builder.line()
					append("error('unmatched item')")
					builder.unindent() builder.line()
				end

				for i = 1, ends do
					append("end")
					if i < ends then builder.unindent() builder.line() end
				end

				if forceClosure then
					builder.unindent() builder.line()
					append("end)()")
				end
			elseif name == "set!" then
				compileExpression(expr[3], builder, escapeVar(expr[2].var) .. " = ")
				if retStmt and retStmt ~= "" then
					builder.line()
					append(retStmt)
					append("nil")
				end
			elseif name == "define" or name == "define-macro" then
				compileExpression(expr[3], builder, escapeVar(expr.defVar) .. " = ")
			elseif name == "define-native" then
				append(("%s = _libs[%q]"):format(escapeVar(expr.defVar), expr[2].contents))
			elseif name == "quote" then
				if retStmt == "" then retStmt = "local _ = " end
				if retStmt then append(retStmt) end

				compileQuote(expr[2], builder, nil)
			elseif name == "quasiquote" then
				if retStmt == "" then retStmt = "local _ = " end
				if retStmt then append(retStmt) end

				compileQuote(expr[2], builder, 1)
			elseif name == "unquote" then
				errorPositions(expr[1] or expr, "unquote outside of quasiquote")
			elseif name == "unquote-splice" then
				errorPositions(expr[1] or expr, "unquote-splice outside of quasiquote")
			elseif name == "import" then
				if not retStmt then
					append("nil")
				elseif retStmt ~= "" then
					append(retStmt)
					append("nil")
				end
			else
				if retStmt then append(retStmt) end
				compileExpression(expr[1], builder)
				append("(")
				for i = 2, #expr do
					if i > 2 then append(", ") end
					compileExpression(expr[i], builder)
				end
				append(")")
			end
		elseif head and head.tag == "list" and head[1].tag == "symbol" and head[1].contents == "lambda" and retStmt then
			-- ((lambda (args) body) values)
			local args = head[2]
			if #args > 0 and #expr > 1 then
				append("local ")

				if #args > 0 then
					for i = 1, #args do
						if i > 1 then append(", ") end
						append(escapeVar(args[i].var))
					end
				else
					append("_")
				end

				append(" = ")

				local varargs = #args > 0 and args[#args].var.isVariadic
				for i = 2, varargs and #args or #expr do
					if i > 2 then append(", ") end
					if expr[i] then
						compileExpression(expr[i], builder)
					else
						append("nil")
					end
				end

				if varargs then
					if #args > 1 then append(", ") end
					append("{ tag = 'list', n = " .. (#expr - #args + 1))
					for i = #args + 1, #expr do
						append(", ")
						compileExpression(expr[i], builder)
					end
					append(" }")
				end

				builder.line()
			end

			compileBlock(head, builder, 3, retStmt)
		else
			if retStmt then append(retStmt) end
			compileExpression(expr[1], builder)
			append("(")
			for i = 2, #expr do
				if i > 2 then append(", ") end
				compileExpression(expr[i], builder)
			end
			append(")")
		end
	else
		error("Unknown tag " .. expr.tag)
	end
end

function compileBlock(exprs, builder, start, retStmt)
	for i = start, #exprs do
		local ret
		if i == #exprs then
			ret = retStmt
		else
			ret = ""
		end

		compileExpression(exprs[i], builder, ret)
		builder.line()
	end
end


return {
	escape = escape,
	escapeVar = escapeVar,
	block = compileBlock,
	expression = compileExpression,
}
