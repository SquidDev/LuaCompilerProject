local function add(a)
	local function doAdd(b) return a + b end

	return doAdd(2), doAdd(3)
end
return add(4), add(8)
