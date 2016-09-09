local Scope = require "tacky.analysis.scope"
local Node = require "tacky.analysis.node"

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
	local node = Node(node, parent, scope)
	node.visited = false

	local kind = node.tag
	if kind == "number" or kind == "boolean" then
		return
	elseif kind == "symbol" then
		scope:require(node.contents)
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

				local success = resolveScope(node, 3, childScope)
				childScope:pushParent()

				if not success then return false end
			elseif contents == "cond" then
				local success = true
				for i = 2, #node do
					local case = node[i]
					resolveNode(case[1], scope)

					local childScope = scope:child()
					if not resolveScope(case, 2, childScope) then
						success = false
					end

					childScope:pushParent()
				end

				if not success then return false end
			elseif contents == "set!" then
				local var = scope:requireVar(node[2].contents)
				if var.const then
					error("Cannot rebind constant " .. var.name)
				end

				if not resolveNode(node[3], scope) then
					return false
				end
			elseif contents == "quote" or contents == "unquote" or contents == "quasiquote" then -- TODO: quasiquote
				-- Do nothing as we're quoting
			elseif contents == "define" or contents == "define-macro" then
				if not resolveNode(node[3], scope) then
					return false
				end
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
	local out = {}
	for i = start, #list do
		out[i - start + 1] = resolveNode(list[i], scope)
	end

	return out
end

return function(node)
	local scope = rootScope:child()
	resolveRoot(node, scope)
	return scope
end
