local Scope = {}
Scope.__index = Scope

function Scope.child(parent)
	local child = setmetatable({
		--- The parent scope.
		parent = parent,

		--- List of all child scopes
		children = {},

		--- Lookup of named variables.
		variables = {},
	}, Scope)

	if parent then
		parent.children[#parent.children + 1] = child
	end

	return child
end

Scope.empty = Scope.child(nil)

function Scope:get(name)
	local element = self

	while element do
		local var = element.variables[name]
		if var then return var end

		element = element.parent
	end

	return nil
end

function Scope:add(name, kind, node)
	if name == nil then error("name is nil", 2) end

	local previous = self.variables[name]
	if previous then
		error("Previous declaration of " .. name)
	end

	self.variables[name] = {
		tag = kind,
		name = name,
		start = self.start, finish = self.finish,
		const = kind == "define" or kind == "define-macro",
		node = node,
	}
end

return Scope
