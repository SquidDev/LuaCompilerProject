describe("Meta levels #compiler #extensions", function()
	it("Quote", function()
		local f = require 'metalua.compiler'.new():src_to_function([[
			return +{stat: local Thing = "World"}
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

		it("Two way quote", function()
			local f = require 'metalua.compiler'.new():src_to_function([[
				local Thing = `String {"World"}
				return +{stat: local Thing = -{Thing}}
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

end)
