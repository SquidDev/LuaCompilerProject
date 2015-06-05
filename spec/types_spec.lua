require'metalua.loader'

describe("Types #types #analysis", function()
	local ScopeCreator = require('luacp.analysis.ScopeCreator')

	describe("infering", function()
		it("Hiding", function()
			local node = require 'metalua.compiler'.new():src_to_ast([[
				-{extension("tua.parser.types", ...)}

				local a:auto
				local b:number
				local c:{number}
				local d:{string=number}
				local e:string|number
				local f:(number):void
				local f:Strict<Number>

				print(b + c[1])
			]], "scope.lua")

			local creator = ScopeCreator(node)
			creator:guess(node)

			print(require 'metalua.pprint'.tostring(node, {blacklist={lineinfo=true,scope=true}}))
		end)
	end)

end)
