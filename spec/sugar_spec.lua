describe("Syntax sugar #compiler #extensions", function()
	it("Lambdas", function()
		local f = require 'metalua.compiler'.new():src_to_function([[
			return |x| x ^ 2
		]], "sugar.lua")
		assert.are.equal(9, f()(3))
	end)

	it("Backtick", function()
		local f = require 'metalua.compiler'.new():src_to_function([[
			return `Local { { `Id {"Thing"}}, { `String { "World"}}}
		]], "sugar.lua")
		assert.are.same({
			tag = "Local",
			{ {
				tag = "Id",
				"Thing"
			} },
			{ {
				tag = "String",
				"World"
			} }
		}, f())
	end)

	it("Custom identifier", function()
		local f = require 'metalua.compiler'.new():src_to_function([[
			local -{ `Id { "Thing" }} = "Foo"
			return Thing
		]], "sugar.lua")
		assert.are.equal("Foo", f())
	end)

	it("Custom identifier", function()
		local f = require 'metalua.compiler'.new():src_to_function([[
			local function a(thing) return "Foo " .. thing end
			return a -{ `String {"Bar"} }
		]], "sugar.lua")
		assert.are.equal("Foo Bar", f())
	end)
end)
