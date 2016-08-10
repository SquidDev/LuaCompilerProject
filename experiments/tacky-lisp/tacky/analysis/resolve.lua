local Scope = require "tacky.analysis.scope"

local declaredSymbols = {
	"nil", "true", "false",
	-- Built in
	"lambda", "define", "define-macro", "set!", "cond",
	"quote", "quasiquote", "unquote", "unquote-spicing",
}

local rootScope = Scope.child()
for i = 1, #declaredSymbols do
	rootScope:add(declaredSymbols[i], "define", nil)
end

local resolveNode, resolveScope
function resolveNode(node, scope)
	node.pending = nil

	local kind = node.tag
	if kind == "number" or kind == "boolean" then
		return
	elseif kind == "symbol" then
		scope:requireVar(node.contents)
	elseif kind == "list" then
		local first = node[1]
		if first and first.tag == "symbol" then
			local contents = first.contents
			if contents == "lambda" then
				local childScope = scope:child()

				local args = node[2]
				for i = 1, #args do
					childScope:add(args[i].contents, "arg", args[i])
				end

				resolveScope(node, 3, childScope)

				childScope:pop()
			elseif contents == "cond" then
				for i = 2, #node do
					local case = node[i]
					resolveNode(case[1], scope)

					local childScope = scope:child()
					resolveScope(case, 2, childScope)
					childScope:pop()
				end
			elseif contents == "set!" then
				local var = scope:requireVar(node[2].contents)
				if var.const then
					error("Cannot rebind constant " .. var.name)
				end

				resolveNode(node[3], scope)
			elseif contents == "quote" or contents == "unquote" or contents == "quasiquote" then -- TODO: quasiquote
				-- Do nothing as we're quoting
			elseif contents == "define" or contents == "define-macro" then
				resolveNode(node[3], scope)
			else
				local var = scope:requireCalled(first.contents)
				if var.macros then
					node.pending = true
					node.scope = scope
					return
				end

				for i = 1, #node do resolveNode(node[i], scope) end
			end
		else
			for i = 1, #node do resolveNode(node[i], scope) end
		end
	else
		error("Unknown type " .. tostring(kind))
	end
end

function resolveScope(list, start, scope)
	-- Predeclare all variables
	for i = start, #list do
		local node = list[i]
		-- require "tacky.pprint".print(i, node)
		-- print(node.kind, node.contents)
		if node.tag == "list" then
			local first = node[1]
			-- print(first.tag, first.contents)
			if first and first.tag == "symbol" and (first.contents == "define" or first.contents == "define-macro") then
				scope:add(node[2].contents, first.contents, node[3])
			end
		end
	end

	-- Resolve all variables
	for i = start, #list do
		resolveNode(list[i], scope)
	end
end

local function resolveRoot(list, scope)
	-- Predeclare all variables
	for i = 1, #list do
		local node = list[i]
		-- require "tacky.pprint".print(i, node)
		-- print(node.kind, node.contents)
		if node.tag == "list" then
			local first = node[1]
			-- print(first.tag, first.contents)
			if first and first.tag == "symbol" and (first.contents == "define" or first.contents == "define-macro") then
				scope:add(node[2].contents, first.contents, node)
			end
		end
	end

	-- Resolve all variables
	for i = 1, #list do
		local childScope = scope:child()
		resolveNode(list[i], childScope)
		childScope:pop()
		list[i].scope = childScope
	end
end

return function(node)
	local scope = rootScope:child()
	resolveRoot(node, scope)
	return scope
end
