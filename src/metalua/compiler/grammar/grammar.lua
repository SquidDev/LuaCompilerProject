--[[

]]
local insert, setmetatable = table.insert, setmetatable
local lexer = require 'metalua.compiler.grammar.lexer'

local M = { }

local meta = {}
function meta:__call(lx, ...)
	return self:parse(lx, ...)
end

local sequence, tag

local function parseError(lx, fmt, ...)
	local li = lx:lineinfo_left()
	local file, line, column, offset, positions
	if li then
		file, line, column, offset = li.source, li.line, li.column, li.offset
		positions = { first = li, last = li }
	else
		line, column, offset = -1, -1, -1
	end

	local msg  = string.format("line %i, char %i: "..fmt, line, column, ...)
	if file and file~='?' then msg = "file "..file..", "..msg end

	local src = lx.src
	if offset>0 and src then
		local i, j = offset, offset
		while src:sub(i,i) ~= '\n' and i>=0	do i=i-1 end
		while src:sub(j,j) ~= '\n' and j<=#src do j=j+1 end
		local srcline = src:sub(i+1, j-1)
		local idx  = string.rep(" ", column).."^"
		msg = string.format("%s\n>>> %s\n>>> %s", msg, srcline, idx)
	end
	--lx:kill()
	error(msg, 2)
end

local function toParser(x)
	local t = type(x)
	if t == "string" then
		return tag({tag="Keyword", x})
	elseif t == "function" then
		local setmetatable({parse = function(self, ...) return x(...) end}, meta)
	elseif t == "table" then
		if x.kind then return x end
		if x.tag then return tag(x)
		if #x > 0 then return sequence(x) end
	end

	error("Cannot convert " .. t, 2)
end

--[[-
	Construct a parser from a type and a parser table

	This sets the metatable and adds a tranformer array

	@tparam string  kind   The parser type
	@tparam parser? parser The parser to use
	@treturn parser The generated parser
]]
local function makeParser(kind, parser)
	parser = parser or {}
	parser.kind = kind
	if not parser.transformers then parser.transformers = {} end
	function parser.transformers:add(x) insert(self, x) end
	return setmetatable(parser, meta)
end

--[[
	Transforms an AST node

	@tparam node     ast    The node to transform
	@tparam parser   parser The parser to get transformers from
	@tparam lineinfo fli    Starting line info
	@tparam lineinfo lli    End line info
	@treturn node The transformed AST node
]]
local function transform(ast, parser, fli, lli)
	if parser.transformers then
		for _, t in ipairs(parser.transformers) do
			ast = t(ast)
		end
	end
	if type(ast) == 'table' then
		local ali = ast.lineinfo
		if not ali or ali.first ~= fli or ali.last ~= lli then
			ast.lineinfo = lexer.new_lineinfo(fli, lli)
		end
	end

	return ast
end

--[[-
	Wrap a parser with transformers, etc...

	@tparam (parser, lexer)->node? x The function that parses the content.
	@treturn function The parser function
]]
local function wrap(x)
	return function(parser, lx)
		local fli = lx:lineinfo_right()
		local seq = x(lx)
		local lli = lx:lineinfo_left()

		return transform(seq, parser, fli, lli)
	end
end

--[[-
	Create a parser for a tag

	@tparam {"tag":string,0:value?} x    The node to consume
	@tparam string?                 name The name of the node
]]
tag = function(x, name)
	if not name then
		name = x.tag
		if x[1] ~= nil then
			name = name .. " " .. string.format("%q", x[1])
		end
	end

	return makeParser("tag", {
		parse = wrap(function(lx)
			local next = lx:next()
			if next.tag ~= x.tag or (x[1] ~= nil and next[1] ~= x[1] then
				message = "Expected " .. name .. ", got " .. next.tag
				if x[1] ~= nil then
					message = message .. " " .. string.format("%q", next[1])
				end
				parse_error(message, 2)
			end

			return next
		end),
		prefix = x
	})
end

sequence = function(x)

end
