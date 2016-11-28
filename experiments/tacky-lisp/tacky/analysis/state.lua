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

		--- The variable this node is defined as
		var = nil,

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
		local state = assert(self.variables[var], "Variable's State is nil: it probably hasn't finished parsing: " .. var.name)
		self.required[state] = true
		return state
	end
end

function State:define(var)
	if self.stage ~= "parsed" then
		error("Cannot add definition when in stage " .. self.stage, 2)
	end

	if var.scope ~= self.scope then return end

	if self.var then
		error("Cannot redeclare variable, already have: " .. self.var.name, 2)
	end

	self.var = var
	self.variables[var] = self
end

function State:built(node)
	if not node then error("node cannot be nil", 2) end

	if self.stage ~= "parsed" then
		error("Cannot transition from " .. self.stage .. " to built", 2)
	end

	self.stage = "built"
	self.node = node

	if node.var ~= self.var then
		error("Variables are different", 2)
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

	local required = {}
	local requiredQueue = {}
	local queue = { self }

	while #queue > 0 do
		local state = table.remove(queue, 1)
		if not required[state] then
			required[state] = true

			for inner, _ in pairs(state.required) do
				queue[#queue + 1] = inner
			end
		end

		-- Sure, it'll be on the queue a lot but it isn't too bad.
		requiredQueue[#requiredQueue + 1] = state
	end

	-- Instead we should scan for all nodes which haven't been built
	-- and request that they are finished.
	-- And then we execute all non-executed nodes.
	for i = #requiredQueue, 1, -1 do
		local state = requiredQueue[i]
		if state.stage ~= "built" then
			coroutine.yield({
				tag = "build",
				state = state,
			})
		end

		if state.stage ~= "executed" then
			coroutine.yield({
				tag = "execute",
				state = state,
			})
		end
	end

	return self.value
end

return State
