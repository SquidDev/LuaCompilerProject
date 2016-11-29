local Scope = require "tacky.analysis.scope"
local errorPositions = require "tacky.parser".errorPositions

local function expectType(node, parent, type, name)
	if not node or node.tag ~= type then
		errorPositions(node or parent, "Expected " .. (name or type) .. ", got " .. (node and node.tag or "nothing"))
	end
end

local function expect(node, parent, name)
	if not node then
		errorPositions(parent, "Expected " .. name .. ", got nothing")
	end
end

local declaredSymbols = {
	-- Built in
	"lambda", "define", "define-macro", "define-native",
	"set!", "cond",
	"quote", "quasiquote", "unquote", "unquote-spicing",
}

local rootScope = Scope.child()
local builtins = {}
for i = 1, #declaredSymbols do
	local symbol = declaredSymbols[i]
	builtins[symbol] = rootScope:add(symbol, "builtin", nil)
end

local declaredVariables = { "nil", "true", "false" }
for i = 1, #declaredVariables do
	local defined = declaredVariables[i]
	rootScope:add(defined, "defined", nil)
end

local function tagMacro(macro, node, parent)
	if not node then
		return
	end

	node.parent = parent

	-- We've already tagged this so continue
	if not node.start and not node.macro then
		node.macro = macro
	end

	if node.tag == "list" then
		for i = 1, node.n do
			tagMacro(macro, node[i], node)
		end
	end
end

local resolveNode, resolveBlock, resolveQuote

function resolveQuote(node, scope, state, level)
	if level == 0 then
		return resolveNode(node, scope, state)
	end

	if node.tag == "string" or node.tag == "number" or node.tag == "symbol" then
		return node
	elseif node.tag == "list" then
		local first = node[1]
		if first and first.tag == "symbol" then
			if first.contents == "unquote" then
				node[2] = resolveQuote(node[2], scope, state, level - 1)
				return node
			elseif first.contents == "quasiquote" then
				node[2] = resolveQuote(node[2], scope, state, level + 1)
				return node
			end
		end

		for i = 1, #node do
			node[i] = resolveQuote(node[i], scope, state, level)
		end

		return node
	else
		error("Unknown tag " .. expr.tag)
	end
end

function resolveNode(node, scope, state)
	local kind = node.tag
	if kind == "number" or kind == "boolean" or kind == "string" then
		-- Do nothing: this is a constant term after all
		return node
	elseif kind == "symbol" then
		state:require(scope:get(node.contents))
		return node
	elseif kind == "list" then
		local first = node[1]
		if first and first.tag == "symbol" then
			local func = scope:get(first.contents)
			local funcState = state:require(func)

			if func == builtins["lambda"] then
				expectType(node[2], node, "list", "argument list")

				local childScope = scope:child()

				local args = node[2]

				for i = 1, #args do
					expectType(args[i], args, "symbol", "argument")
					args[i].var = childScope:add(args[i].contents, "arg", args[i])
				end

				resolveBlock(node, 3, childScope, state)
				return node
			elseif func == builtins["cond"] then
				for i = 2, #node do
					local case = node[i]
					expectType(case, node, "list", "case expression")
					expect(case[1], case, "condition")

					case[1] = resolveNode(case[1], scope, state)

					local childScope = scope:child()
					resolveBlock(case, 2, childScope, state)
				end

				return node
			elseif func == builtins["set!"] then
				expectType(node[2], node, "symbol")
				expect(node[3], node, "value")

				local var = scope:get(node[2].contents)
				state:require(var)
				if var.const then
					error("Cannot rebind constant " .. var.name)
				end

				node[3] = resolveNode(node[3], scope, state)
				return node
			elseif func == builtins["quote"] then
				expect(node[2], node, "value")
				return node
			elseif func == builtins["quasiquote"] then
				expect(node[2], node, "value")

				node[2] = resolveQuote(node[2], scope, state, 1)
				return node
			elseif func == builtins["unquote"] or func == builtins["unquote-splice"] then
				errorPositions(node[1] or node, "Unquote outside of quasiquote")
			elseif func == builtins["define"] then
				expectType(node[2], node, "symbol", "name")
				expect(node[3], node, "value")

				node.var = scope:add(node[2].contents, "defined", node)
				state:define(node.var)

				node[3] = resolveNode(node[3], scope, state)
				return node
			elseif func == builtins["define-macro"] then
				expectType(node[2], node, "symbol", "name")
				expect(node[3], node, "value")

				node.var = scope:add(node[2].contents, "macro", node)
				state:define(node.var)

				node[3] = resolveNode(node[3], scope, state)
				return node
			elseif func == builtins["define-native"] then
				expectType(node[2], node, "symbol", "name")

				node.var = scope:add(node[2].contents, "defined", node)
				state:define(node.var)
				return node
			elseif func.tag == "macro" then
				if not funcState then error("Macro is not defined correctly") end
				local replacement = funcState:get()(table.unpack(node, 2, #node))

				if replacement == nil then
					error("Macro " .. func.name .. " returned empty node")
				end

				tagMacro(funcState, replacement, node)
				return resolveNode(replacement, scope, state)
			elseif func.tag == "defined" or func.tag == "arg" then
				return resolveList(node, 1, scope, state)
			else
				error("Unknown kind " .. tostring(func.tag) .. " for variable " .. func.name)
			end
		else
			return resolveList(node, 1, scope, state)
		end
	else
		error("Unknown type " .. tostring(kind))
	end
end

function resolveList(list, start, scope, state)
	for i = start, #list do
		list[i] = resolveNode(list[i], scope, state)
	end

	return list
end

function resolveBlock(list, start, scope, state)
	for i = start, #list do
		list[i] = resolveNode(list[i], scope, state)
	end

	return list
end

return {
	createScope = function() return rootScope:child() end,
	resolveNode = resolveNode,
	resolveBlock = resolveBlock,
}
