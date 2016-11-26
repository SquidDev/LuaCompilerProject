local function createLookup(t)
	for i = 1, #t do t[t[i]] = true end
	return t
end


local kwrds = createLookup { "and", "break", "do", "else", "elseif", "end", "for", "function", "if", "in", "local", "not", "or", "repeat", "return", "then", "until", "while" }
local function escape(name, args)
	if name == "..." and not args then
		return "_args"
	elseif kwrds[name] then
		return "_e" .. name
	elseif name:match("^%w[_%w%d]*$") then
		-- We explicitly forbid leading _ as that is used for compiler internals
		return name
	else
		return "_e" .. name:gsub("([^_%w%d])", function(x) return "_" .. x:byte() .. "_" end)
	end
end

local compileBlock, compileExpression, compileQuote

function compileQuote(node, builder, level)
	if level == 0 then
		return compileExpression(node, builder)
	end

	local append = builder.add
	if node.tag == "string" or node.tag == "number" then
		append('{tag = "' .. node.tag .. '", ' .. node.contents .. '}')
	elseif node.tag == "symbol" then
		append('{tag = "symbol", contents = ' .. ("%q"):format(node.contents):gsub("\n", "\\n") .. '}')
	elseif node.tag == "list" then
		local first = node[1]
		if first and first.tag == "symbol" then
			if first.contents == "unquote" then
				return compileQuote(node[2], builder, level - 1)
			elseif first.contents == "quasiquote" then
				return compileQuote(node[2], builder, level + 1)
			end
		end

		append('{tag = "list"') -- , n = ' .. #node
		for i = 1, #node do
			append(", ")
			compileQuote(node[i], builder, level)
		end
		append('}')
	else
		error("Unknown tag " .. expr.tag)
	end
end

function compileExpression(expr, builder, retStmt)
	local append = builder.add

	if expr.tag == "string" or expr.tag == "number" or expr.tag == "symbol" then
		if retStmt == "" then retStmt = "local _ = " end
		if retStmt then append(retStmt) end
		local contents = expr.contents
		if expr.tag == "symbol" then contents = escape(contents) end
		append(contents)
	elseif expr.tag == "list" then
		local head = expr[1]
		if head and head.tag == "symbol" then
			local name = head.contents
			if name == "lambda" then
				if retStmt then append(retStmt) end

				append("(function(")
				local args = expr[2]
				for i = 1, #args do
					if i > 1 then append(", ") end
					append(escape(args[i].contents, true))
				end
				append(")")

				builder.indent() builder.line()

				if args[#args].contents == "..." then
					append("local _args = table.pack(...)")
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

				for i = 2, #expr do
					local item = expr[i]
					append("if ")
					compileExpression(item[1], builder)
					append(" then")

					builder.indent() builder.line()
					compileBlock(item, builder, 2, retStmt)
					builder.unindent()

					append("else")
				end

				builder.indent() builder.line()
				append("error('unmatched item')")
				builder.unindent() builder.line()
				append("end")
				builder.line()

				if forceClosure then
					builder.unindent()
					append("end)()")
				end
			elseif name == "set!" then
				compileExpression(expr[3], builder, escape(expr[2].contents) .. " = ")
				if retStmt and retStmt ~= "" then
					builder.line()
					append(retStmt)
					append("nil")
				end
			elseif name == "define" or name == "define-macro" then
				compileExpression(expr[3], builder, "local " .. escape(expr[2].contents) .. " = ")
			elseif name == "define-native" then
				append(("local %s = _ENV[%q]"):format(escape(expr[2].contents), expr[2].contents))
				builder.line()
			elseif name == "quote" then
				if retStmt then
					append(retStmt)
					compileQuote(expr[2], builder, 1)
				end
			elseif name == "quasiquote" then
				if retStmt then
					append(retStmt)
					compileQuote(expr[2], builder, 1)
				end
			elseif name == "unquote" then
				error("Unquote outside of quasiquote")
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
		elseif head and head.tag == "list" and head[1].tag == "symbol" and head[1].contents == "lambda" then
			-- ((lambda (args) body) values)
			append("do")
			builder.indent() builder.line()

			append("local ")

			local args = head[2]
			if #args > 0 then
				for i = 1, #args do
					if i > 1 then append(", ") end
					append(escape(args[i].contents))
				end
			else
				append("_")
			end

			append(" = ")

			-- TODO: Varargs
			if #expr > 1 then
				for i = 2, #expr do
					if i > 2 then append(", ") end
					compileExpression(expr[i], builder)
				end
			else
				append("nil")
			end

			builder.line()

			compileBlock(head, builder, 3, retStmt)

			builder.unindent()
			append("end")
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
	block = compileBlock,
	expression = compileExpression,
}
