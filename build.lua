#!/usr/bin/env lua5.1
package.path = './src/?.lua;./src/?/?.lua;./src/?/init.lua;' .. package.path

local listPackages
do
	local yieldTree
	if pcall(require, 'lfs') then
		yieldTree = function(dir, extensions, filter)
			for entry in lfs.dir(dir) do
				if not filter[entry] then
					entry = dir .. "/" .. entry
					local attr = lfs.attributes(entry)
					if attr.mode == "directory" then
						yieldTree(entry, extensions, filter)
					elseif attr.mode == 'file' then
						local ext = entry:match("%.([^.]+)$")
						if not extensions or extensions[ext] then
							coroutine.yield(entry)
						end
					end
				end
			end
		end
	else
		yieldTree = function(dir, extensions, filter)
			local handle = io.popen('find "'..dir..'" -type f')
			for entry in handle:lines() do
				local continue = true
				for flt, _ in pairs(filter) do
					if entry:find('/' .. flt .. '/', 1, true) then
						continue = false
						break
					end
				end

				if continue then
					local ext = entry:match("%.([^.]+)$")
					if not extensions or extensions[ext] then
						coroutine.yield(entry)
					end
				end
			end
			handle:close()
		end

		lfs = {
			currentdir = function()
				local handle = io.popen('cd')
				local data = handle:read("*l")
				handle:close()
				return data
			end,
			mkdir = function(directory)
				io.popen('mkdir -p "' .. directory .. '"'):close()
			end,
		}
	end

	local function list(dir, extensions, filter)
		assert(dir and dir ~= "", "directory parameter is missing or empty")

		if string.sub(dir, -1) == "/" then
			dir = string.sub(dir, 1, -2)
		end
		filter = filter or {}
		filter['.'] = true
		filter['..'] = true
		filter['.git'] = true
		filter['tua'] = true
		filter['luacp'] = true

		return coroutine.wrap(function() yieldTree(dir, extensions, filter) end)
	end

	listPackages = function(dir)
		if string.sub(dir, -1) == "/" then
			dir = string.sub(dir, 1, -2)
		end

		local packages, names = {}, {}

		for file in list(dir, {lua = true, mlua = true}, {doc = true, build = true, spec = true}) do
			local name = file:sub(#dir + 2):gsub('%.[^.]+$', ''):gsub('/', '.'):gsub("%.init", "")
			packages[name] = file
			table.insert(names, name)
		end
		table.sort(names, function(a,b) return a > b end)

		return function(table, id)
			local id, value = next(table, id)
			return id, value, packages[value]
		end, names
	end
end

local options = require "argparse" "build.mlua"
options:flag "--source"     "-s" :description "Output source file"
options:flag "--bytecode"   "-b" :description "Print the resulting AST"
options:flag "--individual" "-i" :description "Source files for individual files"

local args = options:parse({...})
local shebang = "#!/usr/bin/env lua5.1\n"
if not args.source and not args.bytecode and not args.individual then
	args.source = true
end

require 'metalua.loader'
local mlc = require 'metalua.compiler'

local code = { source = 'metalua.lua' }
do
	print("Packaging")
	for _, package, path in listPackages(lfs.currentdir() .. '/src') do
		print("", package)

		local compiler = mlc.new()
		local ast = compiler:srcfile_to_ast(path)

		if args.individual then
			local result = "build/" .. package:gsub("%.", "/") .. ".lua"
			local dir = result:gsub("(.+)/[^.]+.lua", "%1")
			lfs.mkdir(dir)
			local file, err_msg = io.open(result, 'w')
			if file then
				file:write(compiler:ast_to_src(ast))
				file:close()
			else
				print("can't save source file: ".. result .. ' ' .. err_msg)
			end
		end

		ast = { tag = "Function", { {tag = 'Dots'} }, ast }

		if package == "metalua" then
			table.insert(code, { tag = "Return", { tag = "Call", ast, { tag = "Dots" } } } )
		else
			table.insert(code, { tag = "Set",
				{
					{ tag = "Index",
						{ tag = "Index",
							{tag = "Id", "package"},
							{tag = "String", "preload"}
						},
						{ tag = "String", package }
					}
				},
				{
					ast
				}
			})
		end
	end

	table.insert(code, {tag = "Call",
		{ tag = "Index",
			{tag = "Index",
				{ tag = "Id", "package" },
				{ tag = "String", "preload" }
			},
			{ tag = "String", "metalua.bin.metalua" }
		}
	})
end

lfs.mkdir("build")
local bytecode, source = 'build/meta.luac', 'build/meta.lua'
local compiler = mlc.new()

if args.bytecode then
	print("Compiling Bytecode")
	local file, err_msg = io.open(bytecode, 'wb')
	if not file then error("can't open bytecode file: "..err_msg) end
	if shebang then file:write(shebang) end
	file:write((compiler:ast_to_bytecode(code)))
	file:close()

	if shebang and os.getenv "OS" ~= "Windows_NT" then
		pcall(os.execute, 'chmod a+x "'..bytecode..'"')
	end
end

if args.source then
	print("Creating Source")
	local file, err_msg = io.open(source, 'w')
	if not file then error("can't open source file: "..err_msg) end
	if shebang then file:write(shebang) end
	file:write(compiler:ast_to_src(code))
	file:close()
	if shebang and os.getenv "OS" ~= "Windows_NT" then
		pcall(os.execute, 'chmod a+x "'..source..'"')
	end
end
