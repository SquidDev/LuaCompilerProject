verbose(type(foo) == 'string')
local a, b
if verbose() then
	b = function()
		return a
	end
end
