-- Enable extension method
require 'metalua.loader'

describe("Extensions #compiler #extensions", function()
	describe("#comprehension", function()
		it("nested", function()
			local f = require 'metalua.compiler'.new():src_to_function([[
				-{ extension("comprehension", ...) }
				return { {{i, j} for i = 0, 4} for j = 1, 3, 2}
			]], "comprehension.lua")
			assert.are.same({
				{
					{0, 1},
					{1, 1},
					{2, 1},
					{3, 1},
					{4, 1}
				},
				{
					{0, 3},
					{1, 3},
					{2, 3},
					{3, 3},
					{4, 3}
				}
			}, f())
		end)

		it("chained", function()
			local f = require 'metalua.compiler'.new():src_to_function([[
				-{ extension("comprehension", ...) }
				return { {i, j} for i = 0, 4 for j = 1, 3, 2}
			]], "comprehension.lua")
			assert.are.same({
				{0, 1},
				{0, 3},
				{1, 1},
				{1, 3},
				{2, 1},
				{2, 3},
				{3, 1},
				{3, 3},
				{4, 1},
				{4, 3}
			}, f())
		end)

		it("varinclude", function()
			local f = require 'metalua.compiler'.new():src_to_function([[
				-{ extension("comprehension", ...) }
				local function x() return "bar", "baz" end
				return { "foo", x()..., "qux", x() }
			]], "comprehension.lua")
			assert.are.same({ "foo", "bar", "baz", "qux", "bar", "baz" }, f())
		end)

		it("customaccess", function()
			local f = require 'metalua.compiler'.new():src_to_function([[
				-{ extension("comprehension", ...) }
				local vars = { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10}
				return vars[1 ... 4, 6, 8 ... 10]
			]], "comprehension.lua")
			assert.are.same({ 1, 2, 3, 4, 6, 8, 9, 10 }, f())
		end)
	end)

	describe("#export", function()
		it("locals", function()
			local f = require 'metalua.compiler'.new():src_to_function([[
				-{ extension("export", ...) }
				local a = "value a"
				export a
				export c = "value c"

				export b = "value b"
			]], "export.lua")
			assert.are.same({
				a = "value a",
				b = "value b",
				c = "value c",
			}, f())
		end)

		it("function", function()
			local f = require 'metalua.compiler'.new():src_to_function([[
				-{ extension("export", ...) }
				export function thing()
					return "things!"
				end
			]], "export.lua")

			assert.are.equals("things!", f().thing())
		end)
	end)

	describe("#dollar", function()
		it("Method", function()
			local f = require 'metalua.compiler'.new():src_to_function([[
				-{stat:
					require 'metalua.extension.dollar'.register['test'] = function(...)
					return `Call { ... }
					end
				}
				-{ extension("dollar", ...) }

				return $test(function(...) return ... end, "HELLO")
			]], "opequals.lua")
			assert.are.equals("HELLO", f())
		end)

		it("Identifier", function()
			local f = require 'metalua.compiler'.new():src_to_function([[
				-{stat:
					require 'metalua.extension.dollar'.register['test'] = function(...)
						return `String { "Hello" }
					end
				}
				-{ extension("dollar", ...) }


				return $test
			]], "opequals.lua")
			assert.are.equals("Hello", f())
		end)
	end)

	describe("#infix", function()
		it("infix", function()
			local f = require 'metalua.compiler'.new():src_to_function([[
				-{ extension("infix", ...) }
				local function add(a, b) return a + b end

				return (2 ^ 2) `add` 3
			]], "infix.lua")
			assert.are.equals(7, f())
		end)
	end)

	describe("#loops", function()
		it("continue", function()
			local f = require 'metalua.compiler'.new():src_to_function([[
				-{ extension("loops", ...) }
				local accum = 0

				for i = 0, 100 do
					if i ^ 2 % 5 == 0 then
						continue
					end

					accum = accum + 1
				end

				return accum
			]], "loops.lua")
			assert.are.equals(80, f())
		end)

		it("nested for", function()
			local f = require 'metalua.compiler'.new():src_to_function([[
				-{ extension("loops", ...) }

				local result = {}
				for i = 0, 4 for j = 1, 3, 2 do
					result[#result + 1] = {i, j}
				end

				return result
			]], "loops.lua")
			assert.are.same({
				{0, 1},
				{0, 3},
				{1, 1},
				{1, 3},
				{2, 1},
				{2, 3},
				{3, 1},
				{3, 3},
				{4, 1},
				{4, 3}
			} , f())
		end)
	end)

	describe("#opequals", function()
		it("basic", function()
			local f = require 'metalua.compiler'.new():src_to_function([[
				-{ extension("opequals", ...) }
				local accum = 0

				accum += 2 -- 2
				accum *= 3 -- 6
				accum ^= 2 -- 36
				accum -= 6 -- 30
				accum /= 10 -- 3
				accum .= 3 -- 33

				return accum
			]], "opequals.lua")
			assert.are.equals("33", f())
		end)

		it("table", function()
			local f = require 'metalua.compiler'.new():src_to_function([[
				-{ extension("opequals", ...) }
				local accum = { a = 0 }

				accum.a += 2 -- 2
				accum.a *= 3 -- 6
				accum.a ^= 2 -- 36
				accum.a -= 6 -- 30
				accum.a /= 10 -- 3
				accum.a .= 3 -- 33

				return accum
			]], "opequals.lua")
			assert.are.same({ a = "33"}, f())
		end)

		it("nested", function()
			local f = require 'metalua.compiler'.new():src_to_function([[
				-{ extension("opequals", ...) }
				local counter, value = 0, { a = 0}
				local table = setmetatable({}, {
					 __index = function(self, name)
						counter += 1
						return value
					end,
				})

				table.a.a += 2 -- 2
				table.a.a *= 3 -- 6
				table.a.a ^= 2 -- 36
				table.a.a -= 6 -- 30
				table.a.a /= 10 -- 3
				table.a.a .= 3 -- 33

				return value.a, counter
			]], "opequals.lua")

			local value, counter = f()
			assert.are.equals("33", value)
			assert.are.equals(6, counter)
		end)
	end)
end)
