local pprint = require "tacky.pprint"

local randCtr = 0
return {
	['print!'] = print,
	['pretty-print!'] = pprint.print,

	['dump-node!'] = function(x)
		print(pprint.tostring(x, pprint.nodeConfig))
	end,

	['get-idx'] = rawget,
	['set-idx!'] = rawset,

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

	["and"] = function(x, y) return x and y end,
	["or"] = function(x, y) return x or y end,

	['gensym'] = function()
		randCtr = randCtr + 1
		return { tag = "symbol", contents = ("r_%d"):format(randCtr) }
	end,

	['cdr'] = function(xs)
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
}
