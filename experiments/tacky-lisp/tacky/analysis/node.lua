local Node = {}
Node.__index = Node

function Node.wrap(owner, parent, scope)
	local node = setmetatable({
		--- The owning node.
		node = owner,

		--- The scope this node is in
		scope = scope,

		--- All used variables in this node and child nodes.
		requiredVars = {},

		--- All variables which are called in this scope.
		-- This is used to locate macros
		requiredCalled = {},
	}, Node)

	owner.scope = scope
	owner.node = node

	return node
end

function Node:require(name)
	if name == nil then error("name is nil", 2) end

	local var = self:get(name)
	if not var then error("Unknown variable " .. name) end

	self.requiredVars[name] = true

	return var
end

function Node:requireCalled(name)
	if name == nil then error("name is nil", 2) end

	local var = self:get(name)
	if not var then error("Unknown variable " .. name) end

	self.requiredVars[name] = true
	self.requiredCalled[name] = true

	return var
end

function Node:requiredMacros()
	local requiredMacros = {}
	for name, _ in pairs(self.requiredCalled) do
		if self:get(name).tag == "define-macro" then
			requiredMacros[name] = true
		end
	end

	return requiredMacros
end

function Node:pushParent(parent)
	if not parent then return end

	local scope = self.scope

	for name, _ in pairs(self.requiredVars) do
		if not scope.variables[name] then
			parent.requiredVars[name] = true
		end
	end

	for name, _ in pairs(self.requiredCalled) do
		if not scope.variables[name] then
			parent.requiredCalled[name] = true
		end
	end
end

return Node
