local function lex(str, name)
	local line, column = 1, 1
	local offset, length = 1, #str
	local out, n = {}, 0

	name = name or "<in>"

	local function consume()
		if str:sub(offset, offset) == "\n" then
			line = line + 1
			column = 1
		else
			column = column + 1
		end
		offset = offset + 1
	end

	local function position()
		return { line = line, column = column, offset = offset, name = name }
	end

	local function append(tok, start, finish)
		if not start then start = position() end
		if not finish then finish = position() end
		tok.start, tok.finish = start, finish

		tok.contents = str:sub(start.offset, finish.offset)

		n = n + 1
		out[n] = tok
	end

	while offset <= length do
		local char = str:sub(offset, offset)
		if char == "\n" or char == "\t" or char == " " then
		elseif char == "'" then
			append { tag = "quote" }
		elseif char == "(" then
			append { tag = "open" }
		elseif char == ")" then
			append { tag = "close" }
		elseif char == "`" then
			append { tag = "quasiquote" }
		elseif char == "," then
			if str:sub(offset + 1, offset + 1) == "@" then
				local start = position()
				consume()
				append({ tag = "unquote-splice" }, start)
			else
				append { tag = "unquote" }
			end
		elseif (char >= "0" and char <= "9") or (char == "-" and str:sub(offset + 1, offset + 1):find("%d")) then
			local start = position()
			while str:sub(offset + 1, offset + 1):find("[0-9.e+-]") do
				consume()
			end

			append({ tag = "number" }, start)
		elseif char == "\"" then
			local start = position()
			consume()
			while true do
				local char = str:sub(offset, offset)
				if char == nil then error("Unexpected EOF")
				elseif char == "\\" then consume()
				elseif char == "\"" then
					break
				end

				consume()
			end

			append({ tag = "string" }, start)
		elseif char == ";" then
			while offset <= length and str:sub(offset + 1, offset + 1) ~= "\n" do
				consume()
			end
		else
			local start = position()
			while true do
				local char = str:sub(offset + 1, offset + 1)
				if char == "\n" or char == " " or char == "\t" or char == "(" or char == ")" then
					break
				end

				consume()
			end
			append({ tag = "symbol" }, start)
		end
		consume()
	end

	append({ tag = "eof" })

	return out
end

local function formatPosition(pos) return pos.line .. ":" .. pos.column end
local function errorPositions(item, msg)
	if item.start then
		error(msg .. " at " .. item.start.name .. ":" .. formatPosition(item.start) .. "-" .. formatPosition(item.finish) .. ": " .. item.contents)
	else
		error(msg .. " at ?")
	end
end

local function parse(toks)
	local n = 1

	local function peek() return toks[n] end
	local function consume(tag)
		local item = toks[n]
		if not tag or item.tag == tag then
			n = n + 1
			return item
		else
			return nil
		end
	end

	local function expect(tag)
		local item = toks[n]
		if item.tag == tag then
			n = n + 1
			return item
		else
			errorPositions("Expected " .. tag .. ", got " .. item.tag)
		end
	end

	local head = { tag = "list", n = 0 }
	local stack = {}

	local function append(item)
		head[#head + 1] = item
		head.n = head.n + 1
	end

	local function push()
		local next = { tag = "list", n = 0 }
		-- Push old head to the stack
		stack[#stack + 1] = head
		-- Push new head to old head
		append(next)

		head = next
	end

	local function pop()
		head = stack[#stack]
		stack[#stack] = nil
	end

	while true do
		local item = toks[n]
		local autoClose = false
		n = n + 1

		local tag = item.tag
		if tag == "string" or tag == "number" or tag == "symbol" then
			append(item)
		elseif tag == "open" then
			push()
		elseif tag == "close" then
			if #stack == 0 then
				errorPositions(item, "')' without matching '('")
			end
			pop()
		elseif tag == "quote" or tag == "unquote" or tag == "quasiquote" or tag == "unquote-splice" then
			push()
			append({
				tag = "symbol",
				contents = tag,
				start = item.start,
				finish = item.finish,
			})

			autoClose = true
			head.autoClose = true
		elseif tag == "eof" then
			if #stack ~= 0 then
				errorPositions(item, "Expected ')', got eof")
			else
				break
			end
		else
			errorPositions(item, "Unsuported type " .. item.tag)
		end

		if not autoClose and head.autoClose then
			if #stack == 0 then
				errorPositions(item, "')' without matching '('")
			end
			head.autoClose = nil
			pop()
		end
	end

	return head
end

return {
	lex = lex,
	parse = parse,
	formatPosition = formatPosition,
	errorPositions = errorPositions,
}
