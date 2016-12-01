local displayInfo = false
local verbosity = 0

--- Applies an ANSI formatting code to a string
-- @tparam number col The formatting code to use
-- @tparam string str The string to format
-- @treturn string The formatted string
local function col(col, str)
	return "\27[" .. col .. "m" .. str .. "\27[0m"
end

local function printWarning(msg)
	print(col(33, "[WARN] " .. msg))
end

local function printError(msg)
	local loc = msg:find("\n")
	if loc then
		print(col(31, "[ERROR] " .. msg:sub(1, loc - 1)))
		print(msg:sub(loc + 1))
	else
		print(col(31, "[ERROR] " .. msg))
	end
end

local function printVerbose(...)
	if verbosity > 0 then
		print("[VERBOSE]", ...)
	end
end

local function printDebug(...)
	if verbosity > 1 then
		print("[DEBUG]", ...)
	end
end

--- Format a basic position
local function formatPosition(pos)
	return pos.line .. ":" .. pos.column
end

--- Format a position range
local function formatRange(pos)
	if pos.finish then
		return ("%s %s-%s"):format(pos.name, formatPosition(pos.start), formatPosition(pos.finish))
	else
		return ("%s %s"):format(pos.name, formatPosition(pos.start))
	end
end

--- Format a simple expression's position
local function formatNode(node)
	if node.range and node.contents then
		return ("%s (%q)"):format(formatRange(node.range), node.contents)
	elseif node.range then
		return formatRange(node.range)
	elseif node.macro then
		local macVar = node.macro.var
		return ("macro expansion of %s (%s)"):format(macVar.name, formatNode(node.macro.node))
	else
		return "?"
	end
end

--- Get the nearest source for a particular node, walking up
-- the tree until one is found
local function getSource(item)
	repeat
		if item.range then return item.range end
		item = item.parent
	until not item
end

local function putLines(range, ...)
	local entries = table.pack(...)

	if entries.n == 0 then
		error("Positions cannot be empty", 0)
	elseif entries.n % 2 ~= 0 then
		error("Positions must be a multiple of two, is " .. entries.n)
	end

	local previous = -1
	local maxLine = entries[#entries - 1].start.line
	local code = "\27[92m %" .. #tostring(maxLine) .. "s |\27[0m %s"

	for i = 1, entries.n, 2 do
		local position = entries[i]
		local message = entries[i + 1]

		if previous ~= -1 and (position.start.line - previous) > 2 then
			print(" \27[92m...\27[0m")
		end
		previous = position.start.line

		print(code:format(tostring(position.start.line), position.lines[position.start.line]))

		local pointer = (" "):rep(position.start.column - 1)
		if not range  then
			pointer = pointer .. "^"
		elseif position.finish and position.start.line == position.finish.line then
			pointer = pointer .. ("^"):rep(position.finish.column - position.start.column + 1)
		else
			pointer = pointer .. "^..."
		end

		print(code:format("", pointer .. " " .. message))
	end
end

local function putTrace(node)
	local previous = nil
	repeat
		local formatted = formatNode(node)
		if previous == nil then
			print(col(96, "  => " .. formatted))
		elseif previous ~= formatted then
			print("  in " .. formatted)
		end

		previous = formatted
		node = node.parent
	until not node
end

local function putInfo(...)
	if not displayInfo then return end

	local entries = table.pack(...)
	for i = 1, entries.n do
		print("  " .. entries[i])
	end
end

local function errorPositions(node, msg)
	printError(msg)
	putTrace(node)

	local source = getSource(node)
	if source then putLines(true, source, "") end

	error("An error occured", 0)
end

return {
	formatPosition = formatPosition,
	formatRange    = formatRange,
	formatNode     = formatNode,

	putLines     = putLines,
	putTrace     = putTrace,
	putInfo      = putInfo,

	printWarning   = printWarning,
	printError     = printError,
	printVerbose   = printVerbose,
	printDebug     = printDebug,

	errorPositions = errorPositions,

	setInfo        = function(x) displayInfo = x end,
	setVerbosity   = function(x) verbosity = x end,
}
