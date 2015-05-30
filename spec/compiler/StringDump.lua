-- Tests that `string.dump` works on Java functions

local function testing()
	verbose("HELLO")
end

verbose(string.dump(testing))
