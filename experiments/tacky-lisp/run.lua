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

local libEnv = {}
local libs = {}
local libCache = {}
local global = setmetatable({ _libs = libEnv }, {__index = _ENV})

local scope = resolve.createScope()
local env = {}
local out = {}

local paths = { "?", "tacky/lib/?" }

local function libLoader(name)
	local current = libCache[name]
	if current == true then
		error("Loop: already loading " .. name, 2)
	elseif current ~= nil then
		return current
	end

	if debug then
		print("Loading " .. name)
	end

	libCache[name] = true

	local lib = { name = name }

	local path
	for i = 1, #paths do
		path = paths[i]:gsub("%?", name)

		local handle = io.open(path .. ".lisp", "r")
		if handle then
			lib.lisp = handle:read("*a")
			lib.path = path
			handle:close()
			break
		end
	end

	if not path then error("Cannot find " .. name) end

	if not current then
		for i = 1, #libs do
			local tempLib = libs[i]
			if tempLib.path == path then
				if debug then
					print("Reusing " .. tempLib.name .. " for " .. name)
				end
				local current = libCache[tempLib.name]
				libCache[name] = current
				return current
			end
		end
	end

	local handle = io.open(path .. ".lua", "r")
	if handle then
		lib.native = handle:read("*a")
		handle:close()

		local fun, msg = load(lib.native, "@" .. lib.name, "t", _ENV)
		if not fun then error(msg, 0) end

		for k, v in pairs(fun()) do
			-- TODO: Make name specific for each library
			libEnv[k] = v
		end
	end

	local lexed = parser.lex(lib.lisp, lib.path)
	local parsed = parser.parse(lexed, lib.lisp)

	local compiled, state = compile(parsed, global, env, scope, libLoader, debug)

	libs[#libs + 1] = lib
	libCache[name] = state
	for i = 1, #compiled do
		out[#out + 1] = compiled[i]
	end

	if debug then
		for k, v in pairs(env) do
			print(("%20s => %s"):format(k.name, v.stage))
		end
	end

	if debug then
		print("Loaded " .. name)
	end

	return state
end

libLoader("tacky/lib/prelude")

for i = 1, #inputs do
	libLoader(inputs[i])
end

local result = backend.lua.block(out, 1)
local handle = io.open(output .. ".lua", "w")

handle:write("local _libs = {}\n")
for i = 1, #libs do
	local native = libs[i].native
	if native then
		handle:write("local _temp = (function()\n")

		-- Indent the libraries to make them look prettier
		for line in native:gmatch("[^\n]*") do
			if line == "" then
				handle:write("\n")
			else
				handle:write("\t")
				handle:write(line)
				handle:write("\n")
			end
		end
		handle:write("end)() \nfor k, v in pairs(_temp) do _libs[k] = v end\n")
	end
end

handle:write(result)
handle:close()


local result = backend.lisp.block(out, 1)
local handle = io.open(output .. ".lisp", "w")

handle:write(result)
handle:close()
