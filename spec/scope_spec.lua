require'metalua.loader'

describe("Scopes #scope #analysis", function()
	local ScopeCreator = require('metalua.analysis.ScopeCreator')

	it("Reference count", function()
		local creator = ScopeCreator()
		creator:guess(require 'metalua.compiler'.new():src_to_ast([[
			local a = "HELLO"

			print(a)
			a = a + 1

			local b = a
		]], "scope.lua"))

		-- TODO: Setting doesn't count as a usage
		assert.are.equals(4, creator.scope:getVariable("a").metainfo.references)
		assert.are.equals(0, creator.scope:getVariable("b").metainfo.references)
		assert.are.equals(1, creator.scope.parent:getVariable("print").metainfo.references)
	end)

	it("Nested scopes", function()
		local creator = ScopeCreator()
		creator:guess(require 'metalua.compiler'.new():src_to_ast([[
			local a = "HELLO"
			print(a)

			do
				local b = a
				b = b + 1
			end
		]], "scope.lua"))

		assert.are.equals(2, creator.scope:getVariable("a").metainfo.references)
		assert.are.equals(0, creator.scope:getVariable("b").metainfo.references)
		assert.are.equals(2, creator.scope.children[1]:getVariable("b").metainfo.references)
		assert.are.equals(1, creator.scope.parent:getVariable("print").metainfo.references)
	end)

	it("Hiding", function()
		local creator = ScopeCreator()
		creator:guess(require 'metalua.compiler'.new():src_to_ast([[
			local a = "HELLO"
			a = a + 1

			local a = "Another"
			a = 2
		]], "scope.lua"))

		assert.are.equals(2, creator.scope.variables[1].metainfo.references)
		assert.are.equals(1, creator.scope.variables[2].metainfo.references)
	end)
end)
