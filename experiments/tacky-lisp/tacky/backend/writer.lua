return function()
	local out, n = {}, 0

	local indent, tabsPending = 0, false

	function out.line(txt)
		-- Don't write empty lines
		if tabsPending then return end

		tabsPending = true
		n = n + 1
		out[n] = "\n"

		if txt then out.add(txt) end
	end

	function out.indent() indent = indent + 1 end
	function out.unindent() indent = indent - 1 end

	function out.add(txt)
		if type(txt) ~= "string" then error(tostring(txt) .. " isn't a string", 2) end
		if tabsPending then
			tabsPending = false
			n = n + 1
			out[n] = ("\t"):rep(indent)
		end

		n = n + 1
		out[n] = txt
	end
	function out.toString() return table.concat(out) end

	return out
end
