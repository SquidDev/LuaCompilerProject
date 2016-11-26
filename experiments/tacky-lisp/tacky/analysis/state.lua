local State = {}
State.__index = State

function State.create(variables, scope)
	if not variables then error("variables cannot be nil", 2) end
	if not scope then error("scope cannot be nil", 2) end

	local state = setmetatable({
		--- The scope this top level definition lives under
		scope = scope,

		--- Variable to state mapping
		variables = variables,

		--- List of all required variables
		required = {},

		--- The current stage we are in.
		-- Transitions from parsed -> built -> executed
		stage = "parsed",

		--- The final node for this entry. This is set when building
		-- has finished.
		node = nil,

		-- The actual value of this node. This is set when this function
		-- is executed.
		value = nil,
	}, State)

	return state
end

function State:require(var)
	if self.stage ~= "parsed" then
		error("Cannot add requirement when in stage " .. self.stage, 2)
	end

	if var.scope == self.scope then
		self.required[var] = true
		return assert(self.variables[var], "Variable's State is nil: it probably hasn't finished parsing: " .. var.name)
	end
end

function State:built(node)
	if not node then error("node cannot be nil", 2) end

	if self.stage ~= "parsed" then
		error("Cannot transition from " .. self.stage .. " to built", 2)
	end

	self.stage = "built"
	self.node = node

	if node.var then
		self.variables[node.var] = self
	end
end

function State:executed(value)
	if self.stage ~= "built" then
		error("Cannot transition from " .. self.stage .. " to executed", 2)
	end

	self.stage = "executed"
	self.value = value
end

function State:get()
	if self.stage == "executed" then
		return self.value
	end

	if self.stage ~= "built" then
		coroutine.yield({
			tag = "build",
			state = self,
		})
	end

	-- Note: this could still stack-overflow.
	-- Instead we should scan for all nodes which haven't been built
	-- and request that they are finished.
	-- And then we execute all non-executed nodes.
	for var, _ in pairs(self.required) do
		self.variables[var]:get()
	end

	-- We haven't built this yet so wait til it has been compiled
	coroutine.yield({
		tag = "execute",
		state = self,
	})

	return self.value
end

return State
