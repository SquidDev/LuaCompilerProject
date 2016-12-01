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
local function formatPositions(item)
	if item.start and item.contents then
		return ("%s %s-%s (%q)"):format(item.start.name, formatPosition(item.start), formatPosition(item.finish), item.contents)
	elseif item.start then
		return ("%s %s-%s"):format(item.start.name, formatPosition(item.start), formatPosition(item.finish))
	elseif item.macro then
		local macVar = item.macro.var
		return ("macro expansion of %s (%s)"):format(macVar.name, formatPositions(item.macro.node))
	else
		return "?"
	end
end

local function getSource(item)
	repeat
		if item.lines and item.start then
			return item
		end
		item = item.parent
	until not item
end

local function errorPositions(item, msg)
	local out = { msg }

	local source = getSource(item)
	if source then
		out[#out + 1] = source.lines[source.start.line]

		if source.start.line == source.finish.line then
			out[#out + 1] = (" "):rep(source.start.column - 1) .. ("^"):rep(source.finish.column - source.start.column + 1)
		else
			out[#out + 1] = (" "):rep(source.start.column - 1) .. "^..."
		end
	end

	local previous = nil
	repeat
		local formatted = formatPositions(item)
		if formatted ~= previous then
			out[#out + 1] = "  in " .. formatted
			previous = formatted
		end
		item = item.parent
	until not item

	error(table.concat(out, "\n"), 0)
end

local function parse(toks, src)
	local n = 1
	local lines = nil
	if src then
		lines = {}
		for line in src:gmatch("[^\n]*") do
			lines[#lines + 1] = line
		end
	end

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
		if tag == "string" or tag == "number" or tag == "symbol" then
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

			if previous and head.start and previous.start.line ~= head.start.line then
				local prevPos, itemPos = previous.start, item.start
				if prevPos.column ~= itemPos.column and prevPos.line ~= itemPos.line then
					print("\27[33m[WARN] Different indent compared with previous expressions.\27[0m")

					print(("\27[96m  => %s %s:%s.\27[0m"):format(itemPos.name, itemPos.line, itemPos.column))

					print("  You should try to maintain consistent indentation across a program,")
					print("  try to ensure all expressions are lined up.")
					print("  If this looks OK to you, check you're not missing a closing ')'.")

					if lines then
						local code = "\27[92m %" .. #tostring(itemPos.line) .. "s |\27[0m %s"

						print(code:format(tostring(prevPos.line), lines[prevPos.line]))
						print(code:format("", (" "):rep(prevPos.column - 1).. "^"))

						-- Don't display the ... on adjacent lines
						if (itemPos.line - prevPos.line) > 2 then
							print(" \27[92m...\27[0m")
						end

						print(code:format(tostring(itemPos.line), lines[itemPos.line]))
						print(code:format("", (" "):rep(itemPos.column - 1) .. "^"))
					end
				end
			end

			push()
			head.start = item.start
		elseif tag == "close" then
			if #stack == 0 then
				errorPositions(item, "')' without matching '('")
			end

			head.finish = item.finish
			pop()
		elseif tag == "quote" or tag == "unquote" or tag == "quasiquote" or tag == "unquote-splice" then
			push()
			head.start = item.start

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
			head.finish = item.finish
			pop()
		end
	end

	return head
end

return {
	lex = lex,
	parse = parse,
	formatPosition = formatPosition,
	formatPositions = formatPositions,
	errorPositions = errorPositions,
}
