local pprint = require "tacky.pprint"

local randCtr = 0
return {
	['print!'] = print,
	['pretty-print!'] = pprint.print,

	['dump-node!'] = function(x)
		print(pprint.tostring(x, pprint.nodeConfig))
	end,

	['get-idx'] = function(tbl, key)
		if type(key) == "table" and key.tag == "key" then
			key = key.contents:sub(2)
		end

		return rawget(tbl, key)
	end,
	['set-idx!'] = function(tbl, key, val)
		if type(key) == "table" and key.tag == "key" then
			key = key.contents:sub(2)
		end

		return rawset(tbl, key, val)
	end,

	['=='] = function(x, y) return x == y end,
	['~='] = function(x, y) return x ~= y end,
	['<'] = function(x, y) return x < y end,
	['<='] = function(x, y) return x <= y end,
	['>'] = function(x, y) return x > y end,
	['>='] = function(x, y) return x >= y end,

	['+'] = function(x, y) return x + y end,
	['-'] = function(x, y) return x - y end,
	['*'] = function(x, y) return x * y end,
	['/'] = function(x, y) return x / y end,
	['%'] = function(x, y) return x % y end,
	['^'] = function(x, y) return x ^ y end,

	['gensym'] = function()
		randCtr = randCtr + 1
		return { tag = "symbol", contents = ("r_%d"):format(randCtr) }
	end,

	['cdr'] = function(xs)
		if type(xs) ~= "table" then
			error("Expected list, got " .. type(xs), 2)
		elseif xs.tag ~= "list" then
			error("Expected list, got " .. xs.tag, 2)
		end
		return { tag = "list", n = xs.n - 1, table.unpack(xs, 2) }
	end,
	['invoke-dynamic'] = function(f, ...)
		local name = _ENV
		for x in f:gmatch('[^.]+') do
			name = name[x]
		end
		return name(...)
	end,
	['type'] = function(x)
		local ty = type(x)
		if ty == "table" then
			return x.tag or "table"
		else
			return ty
		end
	end,

	['type#'] = type,

	['empty-struct'] = function() return {} end,

	['error'] = error,
	['assert'] = assert,

	['number->string'] = tostring,
}
