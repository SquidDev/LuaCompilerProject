local backend = require "tacky.backend.init"
local resolve = require "tacky.analysis.resolve"
local State = require "tacky.analysis.state"
local writer = require "tacky.backend.writer"

return function(parsed, global, env, scope)
	local queue = {}
	local out = {}

	for i = 1, #parsed do
		queue[i] = {
			tag  = "init",
			node =  parsed[i],

			-- Global state for every action
			_idx   = i,
			_co    = coroutine.create(resolve.resolveNode),
			_state = State.create(env, scope),
		}
	end

	local function resume(action, ...)
		local status, result = coroutine.resume(action._co, ...)

		if not status then
			error(result .. "\n" .. debug.traceback(action._co))
		elseif coroutine.status(action._co) == "dead" then
			print("  Finished: " .. #queue .. " remaining")
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

		print(head.tag .. " for " .. head._state.stage)

		if head.tag == "init" then
			-- Start the parser with the initial data
			resume(head, head.node, scope, head._state)
		elseif head.tag == "define" then
			-- We're waiting for a variable to be defined.
			-- If it exists then resume, otherwise requeue.

			if scope.variables[head.name] then
				resume(head, scope.variables[head.name])
			else
				print("  Awaiting definition of " .. head.name)
				queue[#queue + 1] = head

				io.read("*l")
			end
		elseif head.tag == "build" then
			if head.state.stage ~= "parsed" then
				resume(head)
			else
				print("  Awaiting building of node")
				queue[#queue + 1] = head

				io.read("*l")
			end
		elseif head.tag == "execute" then
			if head.state.stage ~= "executed" then
				local state = head.state
				local node = assert(state.node, "State is in " .. state.stage .. " instead")
				local var = assert(node.var, "State has no variable")

				local builder = writer()
				backend.lua.backend.expression(node, builder, "")
				builder.add("return " .. backend.lua.backend.escape(var.name))

				local str = builder.toString()
				local fun, msg = load(str, "=compile{" .. var.name .. "}", "t", global)
				if not fun then error(msg, 0) end

				local result = fun()
				state:executed(result)
				global[backend.lua.backend.escape(var.name)] = result
			end

			resume(head)
		else
			error("Unknown tag " .. head.tag)
		end
	end

	return out
end
