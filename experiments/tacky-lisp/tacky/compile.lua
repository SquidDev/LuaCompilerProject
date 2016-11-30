local backend = require "tacky.backend.init"
local resolve = require "tacky.analysis.resolve"
local State = require "tacky.analysis.state"
local writer = require "tacky.backend.writer"

return function(parsed, global, env, scope, loader, debugEnabled)
	local function debugPrint(...)
		if debugEnabled then print(...) end
	end

	local queue = {}
	local out = {}
	local states = {}

	for i = 1, #parsed do
		local state = State.create(env, scope)
		states[i] = state
		queue[i] = {
			tag  = "init",
			node =  parsed[i],

			-- Global state for every action
			_idx   = i,
			_co    = coroutine.create(resolve.resolveNode),
			_state = state,
		}
	end

	local function resume(action, ...)
		local status, result = coroutine.resume(action._co, ...)

		if not status then
			error(result .. "\n" .. debug.traceback(action._co), 0)
		elseif coroutine.status(action._co) == "dead" then
			debugPrint("  Finished: " .. #queue .. " remaining")
			-- We have successfully built the node.
			action._state:built(result)
			out[action._idx] = result
		else
			-- Store the state and coroutine data and requeue for later
			result._idx   = action._idx
			result._co    = action._co
			result._state = action._state

			-- And requeue node
			queue[#queue + 1] = result
		end
	end

	while #queue > 0 do
		local head = table.remove(queue, 1)

		debugPrint(head.tag .. " for " .. head._state.stage)

		if head.tag == "init" then
			-- Start the parser with the initial data
			resume(head, head.node, scope, head._state)
		elseif head.tag == "define" then
			-- We're waiting for a variable to be defined.
			-- If it exists then resume, otherwise requeue.

			if scope.variables[head.name] then
				resume(head, scope.variables[head.name])
			else
				debugPrint("  Awaiting definition of " .. head.name)
				queue[#queue + 1] = head

				io.read("*l")
			end
		elseif head.tag == "build" then
			if head.state.stage ~= "parsed" then
				resume(head)
			else
				debugPrint("  Awaiting building of node (" .. (head.state.var and head.state.var.name or "?") .. ")")
			end
		elseif head.tag == "execute" then
			if head.state.stage ~= "executed" then
				local state = head.state
				local node = assert(state.node, "State is in " .. state.stage .. " instead")
				local var = assert(state.var, "State has no variable")

				local builder = writer()
				backend.lua.backend.expression(node, builder, "")
				builder.line()
				builder.add("return " .. backend.lua.backend.escapeVar(var))

				local str = builder.toString()
				local fun, msg = load(str, "=compile{" .. var.name .. "}", "t", global)
				if not fun then error(msg .. ":" .. str, 0) end

				local result = fun()
				state:executed(result)
				global[backend.lua.backend.escapeVar(var)] = result
			end

			resume(head)
		elseif head.tag == "import" then
			local module = loader(head.module)

			for _, state in pairs(module) do
				if state.var then
					scope:import(head.module, state.var)
				end
			end
			resume(head)
		else
			error("Unknown tag " .. head.tag)
		end
	end

	return out, states
end
