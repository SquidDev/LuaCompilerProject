local writer = require "tacky.backend.writer"

local function expression(node, writer)
	local tag = node.tag
	if tag == "string" or tag == "number" or tag == "symbol" then
		writer.add(tostring(node.contents))
	elseif tag == "list" then
		writer.add("(")

		local newLine = false
		for i = 1, #node - 1 do
			if node[i].tag == "list" then
				newLine = true
				break
			end
		end

		writer.indent()

		for i = 1, #node do
			if i > 1 then
				writer.add(" ")
				if newLine then
					writer.line()
				end
			end
			expression(node[i], writer)
		end

		writer.unindent()
		writer.add(")")
	else
		error("Unsuported type " .. tag)
	end
end

local function block(list, writer)
	for i = 1, #list do
		expression(list[i], writer)
		writer.line()
	end
end

return {
	expression = expression,
	block = block,
}
