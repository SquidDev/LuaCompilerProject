local pprint = require "tacky.pprint"

local nativeType = type

return {
	['print!'] = print,
	['pretty-print!'] = pprint.print,

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

	['gensym'] = function()
		return { tag = "symbol", contents = ("r_%08x"):format(math.random(0, 16^8)) }
	end,

	['cdr'] = function(xs)
		return { tag = "list", n = xs.n - 1, table.unpack(xs, 2) }
	end,

	['type'] = function(x)
		local ty = nativeType(x)
		if ty == "table" then
			return ty.tag or "table"
		else
			return ty
		end
	end,
}
