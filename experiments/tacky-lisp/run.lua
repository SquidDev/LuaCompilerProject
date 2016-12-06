local backend = require "tacky.backend.init"
local compile = require "tacky.compile"
local logger = require "tacky.logger"
local parser = require "tacky.parser"
local pprint = require "tacky.pprint"
local resolve = require "tacky.analysis.resolve"

local paths = { "?", "tacky/lib/?" }
local inputs, output, verbosity, run, prelude = {}, "out", 0, false, "tacky/lib/prelude"

local args = table.pack(...)
local i = 1
while i <= args.n do
	local arg = args[i]
	if arg == "--output" or arg == "-o" then
		i = i + 1
		output = args[i] or error("Expected output after " .. arg, 0)
	elseif arg == "-v" then
		verbosity = verbosity + 1
		logger.setVerbosity(verbosity)
	elseif arg == "--info" or arg == "-i" then
		logger.setInfo(true)
	elseif arg == "--run" or arg == "-r" then
		run = true
	elseif arg == "--include" or arg == "-i" then
		i = i + 1
		local path = args[i] or error("Expected directory after " .. arg, 0)

		path = path:gsub("\\", "/")
		if not path:find("?") then
			if path:sub(#path, #path) == "/" then
				path = path .. "?"
			else
				path = path .. "/?"
			end
		end
		paths[#paths + 1] = path
	elseif arg == "--prelude" or arg == "-p" then
		i = i + 1
		prelude = args[i] or error("Expected prelude after " .. arg, 0)
	elseif arg:sub(1, 1) == "-" then
		error("Unknown option " .. arg, 0)
	else
		inputs[#inputs + 1] = arg
	end

	i = i + 1
end

if #inputs == 0 then error("No inputs specified", 0) end

logger.printVerbose("Using path: " .. table.concat(paths, ":"))

local libEnv = {}
local libs = {}
local libCache = {}
local global = setmetatable({ _libs = libEnv }, {__index = _ENV})

local rootScope = resolve.createScope()
rootScope.isRoot = true
local env = {}
local out = {}

local function libLoader(name, scope, resolve)
	if name:sub(-5) == ".lisp" then
		name = name:sub(1, -6)
	end

	local current = libCache[name]
	if current == true then
		error("Loop: already loading " .. name, 2)
	elseif current ~= nil then
		return true, current
	end

	logger.printVerbose("Loading " .. name)

	libCache[name] = true

	local lib = { name = name }

	local path, handle
	if resolve == false then
		path = name

		handle = io.open(path .. ".lisp", "r")
	else
		for i = 1, #paths do
			path = paths[i]:gsub("%?", name)

			handle = io.open(path .. ".lisp", "r")
			if handle then
				break
			end
		end
	end

	if not handle then return false, "Cannot find " .. name end

	lib.lisp = handle:read("*a")
	lib.path = path
	handle:close()

	if not current then
		for i = 1, #libs do
			local tempLib = libs[i]
			if tempLib.path == path then
				logger.printVerbose("Reusing " .. tempLib.name .. " for " .. name)
				local current = libCache[tempLib.name]
				libCache[name] = current
				return true, current
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

	if not scope then
		scope = rootScope:child()
		scope.isRoot = true
	end
	local compiled, state = compile(parsed, global, env, scope, libLoader)

	libs[#libs + 1] = lib
	libCache[name] = state
	for i = 1, #compiled do
		out[#out + 1] = compiled[i]
	end

	logger.printVerbose("Loaded " .. name)

	return true, state
end

assert(libLoader(prelude, rootScope, false))

for i = 1, #inputs do
	assert(libLoader(inputs[i]))
end

local compiledLua = backend.lua.block(out, 1)
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

-- Predeclare all variables
-- TEMP HACK to prevent "too many local variable" errors.
--[[
handle:write("local ")
local first = true
for var, _ in pairs(env) do
	if first then
		first = false
	else
		handle:write(", ")
	end

	handle:write(backend.lua.backend.escapeVar(var))
end
]]
handle:write("\n")
handle:write(compiledLua)
handle:close()

local compiledLisp = backend.lisp.block(out, 1)
local handle = io.open(output .. ".lisp", "w")

handle:write(compiledLisp)
handle:close()

if run then
	dofile(output .. ".lua")
end
