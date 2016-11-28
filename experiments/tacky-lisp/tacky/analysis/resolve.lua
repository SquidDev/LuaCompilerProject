local Scope = require "tacky.analysis.scope"
local errorPositions = require "tacky.parser".errorPositions

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

local resolveNode, resolveBlock
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
				local childScope = scope:child()

				local args = node[2]
				for i = 1, #args do
					if args[i].tag ~= "symbol" then
						errorPositions(args[i], "Expected symbol, got something " .. args[i].tag)
					end
					args[i].var = childScope:add(args[i].contents, "arg", args[i])
				end

				resolveBlock(node, 3, childScope, state)
				return node
			elseif func == builtins["cond"] then
				for i = 2, #node do
					local case = node[i]
					case[1] = resolveNode(case[1], scope, state)

					local childScope = scope:child()
					resolveBlock(case, 2, childScope, state)
				end

				return node
			elseif func == builtins["set!"] then
				local var = scope:get(node[2].contents)
				state:require(var)
				if var.const then
					error("Cannot rebind constant " .. var.name)
				end

				node[3] = resolveNode(node[3], scope, state)
				return node
			elseif func == builtins["quote"] or func == builtins["unquote"] or func == builtins["quasiquote"] then
				-- Do nothing as we're quoting
				-- TODO: quasiquote
				return node
			elseif func == builtins["define"] then
				if node[2] == nil or node[2].tag ~= "symbol" then
					errorPositions(node[2] or node, "Expected symbol, got " .. (node[2] and node[2].tag or "nothing"), 2)
				end

				node.var = scope:add(node[2].contents, "defined", node)
				state:define(node.var)

				node[3] = resolveNode(node[3], scope, state)
				return node
			elseif func == builtins["define-macro"] then
				if node[2] == nil or node[2].tag ~= "symbol" then
					errorPositions(node[2] or node, "Expected symbol, got " .. (node[2] and node[2].tag or "nothing"))
				end

				node.var = scope:add(node[2].contents, "macro", node)
				state:define(node.var)

				node[3] = resolveNode(node[3], scope, state)
				return node
			elseif func == builtins["define-native"] then
				node.var = scope:add(node[2].contents, "defined", node)
				state:define(node.var)
				return node
			elseif func.tag == "macro" then
				if not funcState then error("Macro is not defined correctly") end
				local replacement = funcState:get()(table.unpack(node, 2, #node))

				if replacement == nil then
					error("Macro " .. func.name .. " returned empty node")
				end

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
