local function execute()
	local a = verbose()
	local b = c and { d = e }
	local f
	local function g()
		return f
	end

	return g()
end

assertEquals(nil, execute())
