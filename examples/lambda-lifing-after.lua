local function doAdd(a, b) return a + b end
local function add(a)
	return doAdd(a, 2), doAdd(a, 3)
end
return add(4), add(8)
