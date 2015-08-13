require'metalua.loader'

describe("Types #types #analysis", function()
	local ScopeCreator = require('luacp.analysis.ScopeCreator')
	local TypeScopeCreator = require('tua.analysis.TypeScopeCreator')

	describe("infering", function()
		it("Hiding", function()
			local node = require 'metalua.compiler'.new():src_to_ast([[
				-{extension("tua.parser.types", ...)}

				local a:auto
				local b:number
				local c:{number}
				local d:{string=number}
				local e:string|number
				local f:(number)->void
				local g:Strict<Number>
				local h:number?

				print(2 + 2)
			]], "scope.lua")

			-- ScopeCreator():guess(node)
			-- TypeScopeCreator():guess(node)

			print(require 'metalua.pprint'.tostring(node, {blacklist={lineinfo=true,scope=true}, with_name = true}))
		end)
	end)
end)
