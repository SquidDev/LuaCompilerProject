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

		--- All used variables in this scope.
		requiredVars = {},

		--- All variables which are called in this scope.
		-- This is used to locate macros
		requiredCalled = {},
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

function Scope:requireVar(name)
	if name == nil then error("name is nil", 2) end

	local var = self:get(name)
	if not var then error("Unknown variable " .. name) end

	self.requiredVars[name] = true

	return var
end

function Scope:requireCalled(name)
	if name == nil then error("name is nil", 2) end

	local var = self:get(name)
	if not var then error("Unknown variable " .. name) end

	self.requiredVars[name] = true
	self.requiredCalled[name] = true

	return var
end

function Scope:requiredMacros()
	local requiredMacros = {}
	for name, _ in pairs(self.requiredCalled) do
		if self:get(name).tag == "define-macro" then
			requiredMacros[name] = true
		end
	end

	return requiredMacros
end

function Scope:pop()
	local parent = self.parent
	if not parent then return end

	for name, _ in pairs(self.requiredVars) do
		if not self.variables[name] then
			parent.requiredVars[name] = true
		end
	end

	for name, _ in pairs(self.requiredCalled) do
		if not self.variables[name] then
			parent.requiredCalled[name] = true
		end
	end
end

return Scope
