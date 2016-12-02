local logger = require "tacky.logger"

local function lex(str, name)
	local line, column = 1, 1
	local offset, length = 1, #str
	local out, n = {}, 0

	name = name or "<in>"

	local lines = {}
	for line in str:gmatch("[^\n]*") do
		lines[#lines + 1] = line
	end

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
		return { line = line, column = column, offset = offset }
	end

	local function append(tok, start, finish)
		if not start then start = position() end
		if not finish then finish = position() end
		tok.range = { start = start, finish = finish, lines = lines, name = name }

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

			local tag = "symbol"
			if char == ":" then tag = "key" end
			append({ tag = tag }, start)
		end
		consume()
	end

	append({ tag = "eof" })

	return out
end

local function parse(toks)
	local n = 1

	local function peek() return toks[n] end

	local head = { tag = "list", n = 0 }
	local stack = {}

	local function append(item)
		head[#head + 1] = item
		head.n = head.n + 1

		item.parent = head
		item.lines = lines
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
		if tag == "string" or tag == "number" or tag == "symbol" or tag == "key" then
			append(item)
		elseif tag == "open" then
			local previous = head[#head]

			--[[
				We want to detect places where the indent is different.

				Initially we check that they aren't on the same line as the parent:
				this catches cases like:
					(define x (lambda (x)
						(foo))) ; Has a different line and indent then parent.

				We obviously shouldn't report entries which are on the same line:
					(foo) (bar) ; Has a different indent
			]]

			if previous and head.range and previous.range.start.line ~= head.range.start.line then
				local prevPos, itemPos = previous.range, item.range
				if prevPos.start.column ~= itemPos.start.column and prevPos.start.line ~= itemPos.start.line then
					logger.printWarning("Different indent compared with previous expressions.")
					logger.putTrace(item)

					logger.putInfo(
						"You should try to maintain consistent indentation across a program,",
						"try to ensure all expressions are lined up.",
						"If this looks OK to you, check you're not missing a closing ')'."
					)

					logger.putLines(false,
						prevPos, "",
						itemPos, ""
					)
				end
			end

			push()
			head.range = {
				start = item.range.start,
				name  = item.range.name,
				lines = item.range.lines,
			}
		elseif tag == "close" then
			if #stack == 0 then
				logger.errorPositions(item, "')' without matching '('")
			end

			head.range.finish = item.range.finish
			pop()
		elseif tag == "quote" or tag == "unquote" or tag == "quasiquote" or tag == "unquote-splice" then
			push()
			head.range = {
				start = item.range.start,
				name  = item.range.name,
				lines = item.range.lines,
			}

			append({
				tag = "symbol",
				contents = tag,
				range = item.range,
			})

			autoClose = true
			head.autoClose = true
		elseif tag == "eof" then
			if #stack ~= 0 then
				logger.printError("Expected ')', got eof")
				logger.putTrace(item)

				logger.putLines(false,
					head.range, "block opened here",
					item.range, "end of file here"
				)
				error("An error occured", 0)
			else
				break
			end
		else
			logger.errorPositions(item, "Unsuported type " .. item.tag)
		end

		if not autoClose and head.autoClose then
			if #stack == 0 then
				logger.errorPositions(item, "')' without matching '('")
			end
			head.autoClose = nil
			head.range.finish = item.range.finish
			pop()
		end
	end

	return head
end

return {
	lex = lex,
	parse = parse,
}
