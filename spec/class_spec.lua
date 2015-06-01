-- Enable extension method
require 'metalua.loader'

describe("Classes #extension #class", function()
	it("instance methods", function()
		local f = require 'metalua.compiler'.new():src_to_function([[
			-{ extension("class", ...) }
			local class A
				a = "thing"
				b = {}
			end

			local a = A()
			a.b[1] = "HELLO"

			return A().b
		]], "class.lua")
		assert.are.same({}, f())
	end)

	it("static methods", function()
		local f = require 'metalua.compiler'.new():src_to_function([[
			-{ extension("class", ...) }
			local class A
				a = "foo"
				b = {}

				static a = "bar"
			end

			return A.a, A().a
		]], "class.lua")
		local a, b = f()
		assert.are.equals("foo", b)
		assert.are.equals("bar", a)
	end)

	it("inheritance", function()
		local f = require 'metalua.compiler'.new():src_to_function([[
			-{ extension("class", ...) }
			local class A
				function foo()
					return "foo"
				end

				function bar()
					return "bar"
				end
			end

			local class B extends A
				function bar()
					return self:foo() .. self.super.bar(self)
				end
			end

			return B():bar()
		]], "class.lua")
		assert.are.equals("foobar", f())
	end)

	it("local access", function()
		local f = require 'metalua.compiler'.new():src_to_function([[
			-{ extension("class", ...) }
			local class A
				static function newA()
					return A()
				end

				function bar()
					return "bar"
				end
			end

			return A:newA():bar()
		]], "class.lua")
		assert.are.equals("bar", f())
	end)
end)
