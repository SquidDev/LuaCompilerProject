--[[

]]
local insert, setmetatable = table.insert, setmetatable
local lexer = require 'metalua.compiler.grammar.lexer'

local M = { }

local meta = {}
function meta:__call(lx, ...)
	return self:parse(lx, ...)
end

local sequence, tag, wrap, choice

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
		while src:sub(i,i) ~= '\n' and i>=0 do i=i-1 end
		while src:sub(j,j) ~= '\n' and j<=#src do j=j+1 end
		local srcline = src:sub(i+1, j-1)
		local idx  = string.rep(" ", column).."^"
		msg = string.format("%s\n>>> %s\n>>> %s", msg, srcline, idx)
	end
	error(msg, 2)
end

local function isParser(x)
	return type(x) == "table" and getmetatable(x) == meta
end

local function toParser(x)
	local t = type(x)
	if t == "string" then
		return tag({tag="Keyword", x})
	elseif t == "function" then
		local setmetatable({parse = function(self, ...) return x(...) end}, meta)
	elseif t == "table" then
		if x.kind then return x end
		if x.tag then return tag(x) end
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

	if parser.wrap ~= false then parser.parse = wrap(parser.parse) end
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
wrap = function(x)
	return function(parser, lx)
		local fli = lx:lineinfo_right()
		local seq = x(parser, lx)
		local lli = lx:lineinfo_left()

		return transform(seq, parser, fli, lli)
	end
end

--[[-
	The core tag parser.

	@tparam                         tagParser self The parser containing the tag
	@tparam {"tag":string,0:value?} lexer     lx   The lexer to read from
	@treturn node The parsed node, or nil if we don't capture it
]]

local tagParser = function(self, lx)
	local tok = lx:next()
	if tok.tag ~= self.tag then
		parse_error(lx, self.message or "Expected " .. self.name .. ", got " .. tok.tag)
	elseif self[1] ~= nil and tok[1] ~= self[1] then
		parse_error(lx, self.message or "Expected " .. self.name .. ", got " .. tok.tag .. string.format("%q", tok[1]))
	end

	if x.capture then
		return next
	end
	return nil
end

--[[-
	Generate a tag parser
	@type tagParser
	@tfield string? name    The name of the parser (defaults to tag + contents)
	@tfield bool?   capture Return the result of the tag
	@tfield string  tag     The tag to capture
	@tfield value?  1       The value to capture

	@treturn parser
]]
tag = function(x)
	if not x.name then
		local name = x.tag
		if x[1] ~= nil then
			name = name .. " " .. string.format("%q", x[1])
		end
		x.name = name
	end

	if x.capture == nil then
		x.capture == x[1] == nil
	end

	return makeParser("tag", { parse = tagParser, prefix = x })
end

--[[-
	The core sequence parser

	@tparam seqParser self The sequence parser
	@tparam lexer     lx   The lexer
	@treturn node The parsed node
]]
local sequenceParser = function(self, lx)
	local result = {}
	for i = 1, #self do
		local node = self[i](lx)
		if node ~= nil then
			insert(result, node)
		end
	end
	if self.builder then
		return self.builder(result)
	end
	return result
end

--[[-
	Generate a sequence parser
]]
sequence = function(x)
	if #x < 1 then
		error("Cannot create empty sequence", 2)
	end

	-- Convert all nodes to a parseri
	for i = 1, #x do
		x[i] = toParser(x[i])
	end
	if not x.name then
		x.name = x[1].name or "unnamed_sequence"
	end
	if x.builder then
		local t = type(x.builder)
		if t == "string" then 
			local tag = x.builder
			x.builder = function(node) node.tag = tag return node end
		elseif t ~= "function" then
			error("Invalid builder of type " .. t .. " in sequence")
		end
	end

	x.parse = sequenceParser

	x.prefix = x[1].prefix
	return makeParser("sequence", x)
end

--[[-
	The core choice parser
]]
local choiceParser = function(self, lx)
	local tok = lx:peek()

	local block = self[tok.tag]
	if not block then 
		local def = self.default
		if def then return def(lx) end
		return false
	end

	local func = block[tok[1] or false]
	if not block then 
		local def = block.default or self.default
		if def then return def(lx) end
		return false
	end

	return func(lx)
end

--[[-
	The choice generator
]]
local choice = function(x)
	function x:add(parser, prefix)
		parser = toParser(parser)
		if parser.prefix then
			local seq = self.sequences
			if not prefix then prefix = parser.prefix end
			local tag, content = prefix.tag, prefix[1]

			local block = seq[tag]
			if not block then
				block = {}
				seq[tag] = block
			end

			if content ~= nil then
				block.default = parser
			else
				if content == "default" then error("Content cannot be default", 2)
				block[content] = parser
			end
		else
			if self.default then
				error("There must be a defined prefix for a choice parser", 2)
			else
				self.default = parser
			end
		end
	end

	function x:del(parser)
		local block = self.seq[parser.tag]
		if not block then return end
		block[parser[1] or "default"] = nil
	end

	if x.default then
		x.default = toParser(x.default)
	end

	for i = 1, #x do
		x:add(x[1])
		x[i] = nil
	end
	x.parse = choiceParser

	return makeParser("choice", x)
end

--[[
	Generate an optional parser.
	This is simply created by a choice parser with the default consuming no tokens.
	
	This actually allows us to emulate most possible usages.

	onkeyword { "else", _M.block } 
	optional { "else", _M.block, wrap = false } 
	
	optkeyword { "else" } 
	optional { "else"  }
]]
local optional = function(x)
	return choice({
		x, default = function(lx) return false end,
		wrap = x.wrap or false, name = x.name, kind = "optional",
	})
end

local function requiredParser(self, lx)
	local result = self.primary(lx)
	if not result or #result == 0 then
		parseError(lx, "`%s` must not be empty.", self.name or "required")
	end
	return result
end

local required = function(primary)
	primary = toParser(primary)
	return makeParser("required", {
		prefix = primary.prefix, name = primary.name
		wrap = false, parse = requiredParser,
	})
end

local future
local futureMeta = {index = function __index(self, name) return future(self.module, name) end}
local function future(module, name)
	if not name then
		return setmetatable({module=module}, futureMeta)
	end
	return toParser(function(lx)
		return module[name](lx)
	end)
end
