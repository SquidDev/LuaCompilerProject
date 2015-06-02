--- Simple module to expect an argument type
-- Also uses checks the getmetatable.__type field

local type = type
local function getType(val, expected)
	local t = type(val)
	local ot
	if t == "table" then
		local mt = getmetatable(val)
		if mt then
			ot = t
			t = mt.__type or t
		end
	end

	return t, ot
end

return function(val, expected, name)
	if expected == "?" or (val == nil and expected:sub(1, 1) == '?') then
		return
	end

	local t, ot = getType(val)
	for one in expected:gmatch("[^|?]+") do
		if one == t or one == ot then return true end
	end

	local message = string.format("Expected %s, got %s", expected, t)
	if name then message = message .. " for argument " .. name end
	error(message, 3)
end
