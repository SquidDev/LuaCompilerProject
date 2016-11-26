local pprint = require "tacky.pprint"

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

	['gen-sym'] = function()
		return { tag = "symbol", contents = ("r_%08x"):format(math.random(0, 16^8)) }
	end,
}
