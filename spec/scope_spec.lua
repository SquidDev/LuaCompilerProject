require'metalua.loader'

describe("Scopes #scopes", function()
	local ScopeCreator = require('metalua.analysis.ScopeCreator')

	local creator = ScopeCreator()
	creator:guess(require 'metalua.compiler'.new():src_to_ast([[
		local a = "HELLO"

		print(a)

		local b = a
	]], "scope.lua"))

	assert.are.equals(2, creator.scope:getVariable("a").metainfo.references)
	assert.are.equals(0, creator.scope:getVariable("b").metainfo.references)
	assert.are.equals(1, creator.scope.parent:getVariable("print").metainfo.references)

end)
