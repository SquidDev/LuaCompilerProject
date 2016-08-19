local recogniser = require 'recogniser'
local builders = require 'builders'
local buildItems, diagnose = recogniser.buildItems, recogniser.diagnose
-------------------
-- Hello, world! --
-------------------
local print_S = require 'print'
local start = 'Expr'
local Grammar = {
	Number = {
		{ name = 'Number', builders.range('09') },
		{ name = 'Number', builders.range('09'), 'Number' },
	},
	Term = {
		{ name = 'Term', 'Number' },
		{ name = 'Term', 'Term', builders.class('+-'), 'Number' },
	},
	Product = {
		{ name = 'Product', 'Term' },
		{ name = 'Product', 'Product', builders.class('*/'), 'Term' },
	},
	Expr = {
		{ name = 'Expr', 'Product' },
		{ name = 'Expr', builders.char('('), 'Product', builders.char(')') },
	}
}

while true do
	io.write('> ')
	io.flush()
	local input = io.read('*l')
	local S = buildItems(Grammar, input, start)
	io.write('Input: ', input, '\n') -- print the input
	print_S(S, Grammar)              -- print all the internal state
	diagnose(S, Grammar, input, start)      -- tell if the input is OK or not
end
