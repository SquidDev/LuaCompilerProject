local backend = require "tacky.backend.init"
local compile = require "tacky.compile"
local parser = require "tacky.parser"
local pprint = require "tacky.pprint"
local resolve = require "tacky.analysis.resolve"

local inputs, output, debug = {}, "out", false

local args = table.pack(...)
local i = 1
while i <= args.n do
	local arg = args[i]
	if arg == "--output" or arg == "-o" then
		i = i + 1
		output = args[i] or error("Expected output after " .. arg, 0)
	elseif arg == "--debug" or arg == "-d" then
		debug = true
	else
		inputs[#inputs + 1] = arg
	end

	i = i + 1
end

if #inputs == 0 then error("No inputs specified", 0) end

local libs = {}
local function loadFile(name)
	local lib = { name = name, path = name .. ".lisp" }

	local handle = assert(io.open(name .. ".lisp", "r"))
	lib.lisp = handle:read("*a")
	handle:close()

	local handle = io.open(name .. ".lua", "r")
	if handle then
		lib.native = handle:read("*a")
		handle:close()
	end

	libs[#libs + 1] = lib
end

loadFile("tacky/lib/prelude")

for i = 1, #inputs do loadFile(inputs[i]) end

local libEnv = {}
local global = setmetatable({ _libs = libEnv }, {__index = _ENV})

for i = 1, #libs do
	local lib = libs[i]
	if lib.native then
		local fun, msg = load(lib.native, "@" .. lib.name, "t", _ENV)
		if not fun then error(msg, 0) end

		for k, v in pairs(fun()) do
			libEnv[k] = v
		end
	end
end

local scope = resolve.createScope()
local env = {}

local out = {}

for i = 1, #libs do
	local lib = libs[i]

	local lexed = parser.lex(lib.lisp, lib.path)
	local parsed = parser.parse(lexed, lib.lisp)

	local compiled = compile(parsed, global, env, scope, debug)

	for i = 1, #compiled do
		out[#out + 1] = compiled[i]
	end

	if debug then
		for k, v in pairs(env) do
			print(("%20s => %s"):format(k.name, v.stage))
		end
	end
end

local result = backend.lua.block(out, 1)
local handle = io.open(output .. ".lua", "w")

handle:write("local _libs = {}\n")
for i = 1, #libs do
	local native = libs[i].native
	if native then
		handle:write("local _temp = (function()\n")
		handle:write(native)
		handle:write("end)() \nfor k, v in pairs(_temp) do _libs[k] = v end\n")
	end
end

handle:write(result)
handle:close()


local result = backend.lisp.block(out, 1)
local handle = io.open(output .. ".lisp", "w")

handle:write(result)
handle:close()
