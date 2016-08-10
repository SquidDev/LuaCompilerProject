local parser = require "tacky.parser"
local resolve = require "tacky.analysis.resolve"
local macros = require "tacky.analysis.macros"
local backend = require "tacky.backend.init"
local writer = require "tacky.backend.writer"

local pprint = require "tacky.pprint"

local empty = { blacklist = { node = true, parent = true, children = true, start = true, finish = true }, dups = false }
local default = { blacklist = { node = true, start = true, finish = true }, dups = false }
local function dump(item, cfg)
	print(pprint.tostring(item, cfg or default))
end

local function asLisp(item)
	local tag = item.tag
	if tag == "string" or tag == "number" or tag == "symbol" then
		return item.contents
	elseif tag == "list" then
		local out = {}
		for i = 1, #item do
			out[i] = asLisp(item[i])
		end
		return "(" .. table.concat(out, " ") .. ")"
	else
		error("Unsuported type " .. tag)
	end
end

local lexed = parser.lex
[[
(define-macro if (lambda (c t b) `(cond (,c ,t) (true ,b))))
(if true true false)
]]

local parsed = parser.parse(lexed)
local scope = resolve(parsed)

local required = macros.gatherRequired(scope)
dump(required)

local compiled = {}
local environment = {}
while #required > 0 do
	local possible, names = macros.gatherPossible(required, compiled, scope)
	if #possible == 0 and #required > 0 then
		error("Cannot resolve for" .. table.concat(required))
	end

	for i = 1, #possible do
		print(names[i], asLisp(possible[i]))
	end

	print(backend.lisp.block(possible))

	local builder = writer()
	backend.lua.backend.block(possible, builder, 1, "")
	builder.add("return {")
	for i = 1, #names do
		builder.add(("[%q] = %s, "):format(names[i], backend.lua.backend.escape(names[i])))
	end
	builder.add("}")

	local str = builder.toString()
	print(str)
	local fun, msg = loadstring(str, "=compile{" .. table.concat(names, ", ") .. "}")
	if not fun then error(msg, 0) end

	setfenv(fun, environment)

	for k, v in pairs(fun()) do
		environment[k] = v
	end

	dump(environment["if"]("foo", "bar", "baz"))

end
