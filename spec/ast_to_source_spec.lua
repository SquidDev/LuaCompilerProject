describe("ast to source #compiler #source", function()
	local function doubleBack(src)
		local compiler = require 'metalua.compiler'.new()
		return compiler:ast_to_src(compiler:src_to_ast(src))
	end

	describe("correct brackets", function()
		describe("call", function()
			it("on functions", function()
				local result = doubleBack([[
					return function() return "H" end()
				]])

				assert.are.equals("H", loadstring(result)())
			end)

			it("on strings", function()
				local result = doubleBack([[
					return "World".sub("Hello", 1,1)
				]])

				assert.are.equals("H", loadstring(result)())
			end)

			it("on tables", function()
				local result = doubleBack([[
					return {a = function(...) return ... end}.a("H")
				]])

				assert.are.equals("H", loadstring(result)())
			end)
		end)

		describe("invoke", function()
			it("on strings", function()
				local result = doubleBack([[
					return "HELLO":sub(1,1)
				]])

				assert.are.equals("H", loadstring(result)())
			end)

			it("on tables", function()
				local result = doubleBack([[
					return {a = function(self, ...) return ... end}:a("H")
				]])

				assert.are.equals("H", loadstring(result)())
			end)
		end)
	end)

	describe("semicolons", function()
		it("chained functions", function()
			local result = doubleBack([[
				local function a() return a end
				a(); ("HELLO"):sub(1, 1)
			]])

			assert(result:match(";"), "Contains semicolon")
			assert(loadstring(result), "Is valid")
		end)

		it("assignment functions", function()
			local result = doubleBack([[
				local a = (1);
				("HELLO"):sub(1, 1)
			]])

			assert(result:match(";"), "Contains semicolon")
			assert(loadstring(result), "Is valid")
		end)
	end)

	describe("function names", function()
		it("invalid identifier", function()
			local result = doubleBack([[
				a['b.c'].d = function() end
			]])

			assert(loadstring(result), "Is valid")
		end)
	end)

end)
