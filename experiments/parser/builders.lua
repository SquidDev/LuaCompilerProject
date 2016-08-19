--- Core parsing items

local function char(c)
	return function(input, index)
		if input == nil then
			return "'"..c.."'" -- ugly hack used to print items
		end

		return input:byte(index) == c:byte()
	end
end

local function range(r)
	return function(input, index)
		if input == nil then
			return "["..r:sub(1, 1)..'-'..r:sub(2, 2).."]" -- ugly hack used to print items
		end

		local i = input:byte(index)
		return i ~= nil and i >= r:byte(1) and i <= r:byte(2)
	end
end

local function class(c)
	return function(input, index)
		if input == nil then
			return "["..c.."]" -- ugly hack used to print items
		end

		return index <= input:len() and c:find(input:sub(index, index), 1, true) ~= nil
	end
end

return {
	char  = char,
	range = range,
	class =  class,
}
