local print = require "tacky.pprint".print

local function gatherRequired(scope)
	local visited = scope:requiredMacros()
	local queue = {}
	for macro in pairs(visited) do queue[#queue + 1] = macro end

	local i = 0

	while i < #queue do
		i = i + 1

		local name = queue[i]
		visited[name] = true

		local macro = scope.variables[name]
		if macro then
			for required in pairs(macro.node.scope.requiredVars) do
				if not visited[required] then
					visited[required] = true
					queue[#queue + 1] = required
				end
			end
		end
	end

	return queue
end

local function gatherPossible(items, compiled, scope)
	local out, outNames = {}, {}
	for i = #items, 1, -1 do
		local name = items[i]
		local node = scope.variables[name].node

		local success = true
		for required in pairs(node.scope.requiredVars) do
			if not compiled[required] then
				success = false
				break
			end
		end

		if success then
			out[#out + 1] = node
			outNames[#outNames + 1] = name
			compiled[name] = true
			table.remove(items, i)
		end
	end

	for i=1, math.floor(#out / 2) do
		out[i], out[#out - i + 1] = out[#out - i + 1], out[i]
		outNames[i], outNames[#outNames - i + 1] = outNames[#outNames - i + 1], outNames[i]
	end

	return out, outNames
end

return {
	gatherRequired = gatherRequired,
	gatherPossible = gatherPossible,
}
